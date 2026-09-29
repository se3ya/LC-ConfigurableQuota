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
        internal IList? Saves;
        internal IList? States;
        internal bool Consistent;

        internal int Count => Values.Count;
        internal bool IsScrap => Live != null && Live.itemProperties != null && Live.itemProperties.isScrap;
    }

    internal static class SelfSortingStorageCompat
    {
        private const string AssemblyName = "SelfSortingStorage";
        private const string SmartCupboardTypeName = "SelfSortingStorage.Cupboard.SmartCupboard";
        private const string SmartMemoryTypeName = "SelfSortingStorage.Cupboard.SmartMemory";
        private const string InvalidId = "INVALID";

        internal static bool Processed;

        private static Type? _smartCupboardType;
        private static FieldInfo? _placedItemsField;
        private static FieldInfo? _memoryInstanceField;
        private static FieldInfo? _memoryItemListField;
        private static FieldInfo? _memorySizeField;
        private static FieldInfo? _dataIdField;
        private static FieldInfo? _dataValuesField;
        private static FieldInfo? _dataSavesField;
        private static FieldInfo? _dataStatesField;
        private static FieldInfo? _dataQuantityField;
        private static MethodInfo? _retrieveDataMethod;
        private static MethodInfo? _updateDisplayedQuantityRpc;
        private static MethodInfo? _setSizeRpc;
        private static bool _ready;
        private static bool _initDone;

        private static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(ModGUIDs.SELF_SORTING_STORAGE_GUID);

        internal static void Init(Harmony harmony)
        {
            if (_initDone) return;
            _initDone = true;

            if (!IsInstalled) return;

            if (!LoadMembers())
            {
                Plugin.Log.LogWarning("Could not read SSS storage, it will be cleared on a crew wipe as usual.");
                return;
            }

            _ready = true;

            var handlers = StorageWipePatch.FindHandlers(m => m.DeclaringType?.Assembly.GetName().Name == AssemblyName);
            if (handlers.Count == 0)
            {
                Plugin.Log.LogWarning("Could not find the SSS crew wipe handler, stored items will be cleared on a crew wipe.");
                return;
            }

            if (handlers.Count(h => StorageWipePatch.PatchSelfSorting(harmony, h)) > 0)
                Plugin.Log.LogInfo("SSS stored items now follow crew wipe loss settings.");
        }

        internal static List<StorageSlot> GetSlots()
        {
            var slots = new List<StorageSlot>();

            if (!_ready)
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
                            var slot = new StorageSlot
                            {
                                Key = flatIndex,
                                Live = live,
                                Values = values,
                                Saves = _dataSavesField!.GetValue(data) as IList,
                                States = _dataStatesField?.GetValue(data) as IList
                            };
                            slot.Consistent = IsConsistent(slot, data);

                            if (!slot.Consistent)
                                Plugin.Log.LogDebug($"SSS stack {flatIndex} is left alone, its stored lists do not line up.");

                            slots.Add(slot);
                        }

                        flatIndex++;
                    }
                }

                Processed = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not read SSS storage: {e.Message}");
                slots.Clear();
            }

            return slots;
        }

        internal static bool IsNew(StorageSlot slot, int index)
        {
            if (slot.States != null && index < slot.States.Count && slot.States[index] is bool persisted)
                return !persisted;

            return slot.Live != null && !slot.Live.scrapPersistedThroughRounds;
        }

        internal static int RemoveItems(StorageSlot slot, IEnumerable<int> indices)
        {
            if (!_ready || slot == null || !slot.Consistent)
                return 0;

            object? memory = _memoryInstanceField!.GetValue(null);
            if (memory == null) return 0;

            int startCount = slot.Values.Count;
            int removed = 0;

            try
            {
                foreach (int index in indices.Where(i => i >= 0 && i < startCount).Distinct().OrderByDescending(i => i))
                {
                    MoveToFront(slot.Values, index);
                    MoveToFront(slot.Saves!, index);
                    if (slot.States != null)
                        MoveToFront(slot.States, index);

                    if (_retrieveDataMethod!.Invoke(memory, new object[] { slot.Key, true }) == null)
                        break;

                    removed++;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not remove SSS entries: {e.Message}");
            }

            if (removed == 0) return 0;

            try
            {
                bool emptied = removed >= startCount;

                if (emptied && TryGetPlacedItems(out IDictionary? placedItems))
                    placedItems!.Remove(slot.Key);
                else if (!emptied)
                    RealignLiveValue(slot);

                SyncCupboardState(memory, slot.Key, emptied ? 0 : startCount - removed);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update SSS stack after removal: {e.Message}");
            }

            return removed;
        }

        internal static void ScaleValues(StorageSlot slot, float multiplier, IEnumerable<int> indices)
        {
            if (!_ready || slot == null)
                return;

            try
            {
                foreach (int i in indices)
                {
                    if (i < 0 || i >= slot.Values.Count) continue;

                    int value = slot.Values[i] is int stored ? stored : 0;
                    slot.Values[i] = Mathf.Max(0, Mathf.RoundToInt(value * multiplier));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not scale SSS values: {e.Message}");
            }
        }

        private static void MoveToFront(IList list, int index)
        {
            if (index == 0) return;

            object? item = list[index];
            list.RemoveAt(index);
            list.Insert(0, item);
        }

        private static void RealignLiveValue(StorageSlot slot)
        {
            try
            {
                if (slot.Live == null || slot.Values.Count == 0) return;
                if (slot.Live.itemProperties == null || !slot.Live.itemProperties.isScrap) return;
                if (slot.Values[0] is not int value) return;
                if (slot.Live.scrapValue == value) return;

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

        private static bool IsConsistent(StorageSlot slot, object data)
        {
            if (slot.Saves == null || _dataQuantityField!.GetValue(data) is not int quantity || quantity <= 0)
                return false;

            return slot.Values.Count == quantity
                && slot.Saves.Count == quantity
                && (slot.States == null || slot.States.Count == quantity);
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

        private static bool LoadMembers()
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
            _dataSavesField = AccessTools.Field(dataType, "Saves");
            _dataStatesField = dataType?.GetField("States");
            _dataQuantityField = AccessTools.Field(dataType, "Quantity");

            _retrieveDataMethod = AccessTools.Method(memoryType, "RetrieveData", new[] { typeof(int), typeof(bool) });
            _updateDisplayedQuantityRpc = AccessTools.Method(_smartCupboardType, "UpdateDisplayedQuantityClientRpc", new[] { typeof(int), typeof(int) });
            _setSizeRpc = AccessTools.Method(_smartCupboardType, "SetSizeClientRpc", new[] { typeof(int) });

            return _smartCupboardType != null
                && _placedItemsField != null
                && _memoryInstanceField != null
                && _memoryItemListField != null
                && _memorySizeField != null
                && _dataIdField != null
                && _dataValuesField != null
                && _dataSavesField != null
                && _dataQuantityField != null
                && _retrieveDataMethod != null;
        }
    }
}
