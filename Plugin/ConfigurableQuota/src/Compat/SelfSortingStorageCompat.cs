using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using ConfigurableQuota.Patches;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Compat
{
    internal sealed class StorageSlot
    {
        internal int Key;
        internal GrabbableObject Live = null!;
        internal IList Values = null!;

        internal int Count => Values.Count;
        internal bool IsScrap => Live != null && Live.itemProperties != null && Live.itemProperties.isScrap;
    }

    internal static class SelfSortingStorageCompat
    {
        private const string AssemblyName = "SelfSortingStorage";
        private const string SmartCupboardTypeName = "SelfSortingStorage.Cupboard.SmartCupboard";
        private const string SmartMemoryTypeName = "SelfSortingStorage.Cupboard.SmartMemory";
        private const string ResetOnAllDeadMethodName = "ResetSmartCupboardIfAllDeads";
        private const string InvalidId = "INVALID";

        private static Type? _smartCupboardType;
        private static FieldInfo? _placedItemsField;
        private static FieldInfo? _memoryInstanceField;
        private static FieldInfo? _memoryItemListField;
        private static FieldInfo? _memorySizeField;
        private static FieldInfo? _dataIdField;
        private static FieldInfo? _dataValuesField;
        private static MethodInfo? _retrieveDataMethod;
        private static MethodInfo? _updateDisplayedQuantityRpc;
        private static MethodInfo? _setSizeRpc;
        private static bool _reflectionReady;
        private static bool _reflectionAttempted;

        internal static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(ModGUIDs.SELF_SORTING_STORAGE_GUID);

        internal static List<StorageSlot> GetSlots()
        {
            var slots = new List<StorageSlot>();

            if (!IsInstalled || !EnsureReflectionReady())
                return slots;

            try
            {
                if (!TryGetPlacedItems(out IDictionary? placedItems))
                    return slots;

                object? memory = _memoryInstanceField!.GetValue(null);
                if (memory == null || _memoryItemListField!.GetValue(memory) is not IEnumerable itemList)
                    return slots;

                int flatIndex = 0;
                foreach (object? row in itemList)
                {
                    if (row is not IEnumerable entries)
                        continue;

                    foreach (object? data in entries)
                    {
                        if (data != null
                            && IsValid(data)
                            && placedItems!.Contains(flatIndex)
                            && placedItems[flatIndex] is GrabbableObject live
                            && live != null
                            && _dataValuesField!.GetValue(data) is IList values)
                        {
                            slots.Add(new StorageSlot
                            {
                                Key = flatIndex,
                                Live = live,
                                Values = values
                            });
                        }

                        flatIndex++;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not read SSS slots: {e.Message}");
            }

            return slots;
        }

        internal static bool RemoveItems(StorageSlot slot, int count)
        {
            if (!EnsureReflectionReady() || slot == null || count <= 0)
                return false;

            try
            {
                object? memory = _memoryInstanceField!.GetValue(null);
                if (memory == null) return false;

                int startCount = slot.Values.Count;
                int toRemove = Math.Min(count, startCount);
                if (toRemove <= 0) return false;

                for (int i = 0; i < toRemove; i++)
                    _retrieveDataMethod!.Invoke(memory, new object[] { slot.Key, true });

                bool emptied = toRemove >= startCount;

                if (emptied && TryGetPlacedItems(out IDictionary? placedItems))
                    placedItems!.Remove(slot.Key);
                else if (!emptied)
                    RealignLiveValue(slot);

                SyncCupboardState(memory, slot.Key, emptied ? 0 : startCount - toRemove);
                return emptied;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not remove SSS entries: {e.Message}");
                return false;
            }
        }

        internal static void ScaleValues(StorageSlot slot, float multiplier)
        {
            if (!EnsureReflectionReady() || slot == null)
                return;

            try
            {
                for (int i = 0; i < slot.Values.Count; i++)
                {
                    int value = slot.Values[i] is int stored ? stored : 0;
                    slot.Values[i] = Mathf.Max(0, Mathf.RoundToInt(value * multiplier));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not scale SSS values: {e.Message}");
            }
        }

        private static void RealignLiveValue(StorageSlot slot)
        {
            try
            {
                if (slot.Live == null || slot.Values.Count == 0) return;
                if (slot.Live.itemProperties == null || !slot.Live.itemProperties.isScrap) return;
                if (slot.Values[0] is not int value) return;

                slot.Live.scrapValue = value;
                slot.Live.SetScrapValue(value);

                var netObj = slot.Live.GetComponent<NetworkObject>();
                if (netObj != null)
                    NetworkSync.SyncValueLossToClients(new[] { new SyncValueLossData(netObj.NetworkObjectId, value) });
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not realign SelfSortingStorage stack value: {e.Message}");
            }
        }

        internal static int SumValues(StorageSlot slot)
        {
            int total = 0;
            if (slot == null) return total;

            for (int i = 0; i < slot.Values.Count; i++)
            {
                if (slot.Values[i] is int value)
                    total += Mathf.Max(0, value);
            }

            return total;
        }

        internal static MethodInfo? FindResetOnAllDeadMethod()
        {
            try
            {
                Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == AssemblyName);
                if (assembly == null) return null;

                foreach (Type type in assembly.GetTypes())
                {
                    MethodInfo? method = type.GetMethod(
                        ResetOnAllDeadMethodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (method != null) return method;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not locate SSS crew wipe handler: {e.Message}");
            }

            return null;
        }

        private static bool TryGetPlacedItems(out IDictionary? placedItems)
        {
            placedItems = null;

            var cupboard = UnityEngine.Object.FindObjectOfType(_smartCupboardType!);
            if (cupboard == null) return false;

            placedItems = _placedItemsField!.GetValue(cupboard) as IDictionary;
            return placedItems != null;
        }

        private static bool IsValid(object data)
        {
            return _dataIdField!.GetValue(data) as string != InvalidId;
        }

        private static void SyncCupboardState(object memory, int key, int quantity)
        {
            try
            {
                var cupboard = UnityEngine.Object.FindObjectOfType(_smartCupboardType!);
                if (cupboard == null) return;

                _updateDisplayedQuantityRpc?.Invoke(cupboard, new object[] { key, quantity });

                if (_setSizeRpc != null && _memorySizeField!.GetValue(memory) is int size)
                    _setSizeRpc.Invoke(cupboard, new object[] { size });
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not sync SSS cupboard state: {e.Message}");
            }
        }

        private static bool EnsureReflectionReady()
        {
            if (_reflectionAttempted)
                return _reflectionReady;

            _reflectionAttempted = true;

            try
            {
                _smartCupboardType = AccessTools.TypeByName(SmartCupboardTypeName);
                _placedItemsField = AccessTools.Field(_smartCupboardType, "placedItems");

                Type? memoryType = AccessTools.TypeByName(SmartMemoryTypeName);
                _memoryInstanceField = AccessTools.Field(memoryType, "Instance");
                _memoryItemListField = AccessTools.Field(memoryType, "ItemList");
                _memorySizeField = AccessTools.Field(memoryType, "Size");

                Type? dataType = AccessTools.Inner(memoryType, "Data");
                _dataIdField = AccessTools.Field(dataType, "Id");
                _dataValuesField = AccessTools.Field(dataType, "Values");

                _retrieveDataMethod = AccessTools.Method(memoryType, "RetrieveData", new[] { typeof(int), typeof(bool) });
                _updateDisplayedQuantityRpc = AccessTools.Method(_smartCupboardType, "UpdateDisplayedQuantityClientRpc", new[] { typeof(int), typeof(int) });
                _setSizeRpc = AccessTools.Method(_smartCupboardType, "SetSizeClientRpc", new[] { typeof(int) });

                _reflectionReady = _smartCupboardType != null
                    && _placedItemsField != null
                    && _memoryInstanceField != null
                    && _memoryItemListField != null
                    && _memorySizeField != null
                    && _dataIdField != null
                    && _dataValuesField != null
                    && _retrieveDataMethod != null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to initialize SSS reflection: {e.Message}");
            }

            return _reflectionReady;
        }
    }
}
