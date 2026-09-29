using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ConfigurableQuota.Patches;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Compat
{
    internal sealed class HQoLItem
    {
        internal int Index;
        internal string Name = "";
        internal int Value;
    }

    internal static class HQoLCompat
    {
        private const string HandlerTypeName = "HQoL.Patches.RoundManagerPatches";
        private const string NetworkTypeName = "HQoL.Network.HQoLNetwork";
        private const string ItemTypeName = "HQoL.Util.ItemReference";
        private const string SaleRpcName = "UpdateQuotaAndDisplayCreditsEarningClientRpc";
        private const int ExecuteStage = 1;

        internal static bool Processed;

        private static PropertyInfo? _instanceProperty;
        private static FieldInfo? _storageField;
        private static FieldInfo? _totalField;
        private static FieldInfo? _modifiedField;
        private static FieldInfo? _nameField;
        private static FieldInfo? _valueField;
        private static PropertyInfo? _countProperty;
        private static PropertyInfo? _itemProperty;
        private static MethodInfo? _removeAtMethod;
        private static FieldInfo? _stageField;
        private static bool _ready;
        private static bool _initDone;

        internal static void Init(Harmony harmony)
        {
            if (_initDone) return;
            _initDone = true;

            MethodInfo? handler = StorageWipePatch.FindHandlers(m => m.DeclaringType?.FullName == HandlerTypeName).FirstOrDefault();
            if (handler == null) return;

            Assembly assembly = handler.DeclaringType!.Assembly;
            Type? networkType = assembly.GetType(NetworkTypeName);

            if (networkType == null || !LoadMembers(networkType, assembly.GetType(ItemTypeName)))
            {
                Plugin.Log.LogWarning("Could not find HQoL storage, it will be cleared on a crew wipe as usual.");
                return;
            }

            if (StorageWipePatch.PatchHQoL(harmony, handler))
            {
                _ready = true;
                Plugin.Log.LogInfo("HQoL storage now follows crew wipe loss settings.");
            }

            PatchSales(harmony, networkType);
        }

        internal static List<HQoLItem> GetItems()
        {
            var items = new List<HQoLItem>();
            if (!_ready) return items;

            try
            {
                NetworkBehaviour? network = GetNetwork();
                if (network == null) return items;

                object list = _storageField!.GetValue(network);
                int count = (int)_countProperty!.GetValue(list);

                for (int i = 0; i < count; i++)
                {
                    object entry = _itemProperty!.GetValue(list, new object[] { i });
                    items.Add(new HQoLItem
                    {
                        Index = i,
                        Name = _nameField!.GetValue(entry)?.ToString() ?? "",
                        Value = (int)_valueField!.GetValue(entry)
                    });
                }

                Processed = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not read HQoL storage: {e.Message}");
                items.Clear();
            }

            return items;
        }

        internal static void ScaleValues(List<HQoLItem> items, float multiplier)
        {
            if (!_ready || items.Count == 0) return;

            try
            {
                NetworkBehaviour? network = GetNetwork();
                if (network == null) return;

                object list = _storageField!.GetValue(network);
                int oldTotal = 0;
                int newTotal = 0;
                bool changed = false;

                foreach (var item in items)
                {
                    int newValue = Mathf.Max(0, Mathf.RoundToInt(item.Value * multiplier));
                    oldTotal += item.Value;
                    newTotal += newValue;

                    if (newValue == item.Value) continue;

                    object entry = _itemProperty!.GetValue(list, new object[] { item.Index });
                    _valueField!.SetValue(entry, newValue);
                    _itemProperty.SetValue(list, entry, new object[] { item.Index });
                    changed = true;
                }

                if (!changed) return;

                UpdateTotal(network, list);
                Plugin.Log.LogInfo($"Reduced HQoL storage value by {1f - multiplier:P0}, ${oldTotal} to ${newTotal}.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update HQoL storage: {e.Message}");
            }
        }

        internal static int RemoveItems(List<HQoLItem> items)
        {
            if (!_ready || items.Count == 0) return 0;

            NetworkBehaviour? network = GetNetwork();
            if (network == null) return 0;

            object list = _storageField!.GetValue(network);
            int removed = 0;

            try
            {
                foreach (var item in items.OrderByDescending(i => i.Index))
                {
                    _removeAtMethod!.Invoke(list, new object[] { item.Index });
                    removed++;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not remove HQoL entries: {e.Message}");
            }

            if (removed > 0)
                UpdateTotal(network, list);

            return removed;
        }

        internal static int SumValues()
        {
            if (!_ready) return 0;

            try
            {
                NetworkBehaviour? network = GetNetwork();
                return network != null ? Sum(_storageField!.GetValue(network)) : 0;
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not sum HQoL storage: {e.Message}");
                return 0;
            }
        }

        private static NetworkBehaviour? GetNetwork()
        {
            var network = _instanceProperty!.GetValue(null) as NetworkBehaviour;
            if (network == null || !network.IsSpawned || !network.IsServer) return null;
            return network;
        }

        private static int Sum(object list)
        {
            int count = (int)_countProperty!.GetValue(list);
            int total = 0;

            for (int i = 0; i < count; i++)
                total += Mathf.Max(0, (int)_valueField!.GetValue(_itemProperty!.GetValue(list, new object[] { i })));

            return total;
        }

        private static void UpdateTotal(NetworkBehaviour network, object list)
        {
            try
            {
                ((NetworkVariable<int>)_totalField!.GetValue(network)).Value = Sum(list);
                _modifiedField!.SetValue(network, true);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not update HQoL storage: {e.Message}");
            }
        }

        private static void PatchSales(Harmony harmony, Type networkType)
        {
            try
            {
                _stageField = AccessTools.Field(typeof(NetworkBehaviour), "__rpc_exec_stage");
                MethodInfo? rpc = AccessTools.Method(networkType, SaleRpcName, new[] { typeof(int) });

                if (_stageField == null || rpc == null)
                {
                    Plugin.Log.LogWarning("Could not find HQoL sales, they will not count as sold scrap.");
                    return;
                }

                harmony.Patch(
                    rpc,
                    prefix: new HarmonyMethod(typeof(HQoLCompat), nameof(ReadSale)),
                    postfix: new HarmonyMethod(typeof(HQoLCompat), nameof(CountSale)));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not patch HQoL sales: {e.Message}");
            }
        }

        private static void ReadSale(NetworkBehaviour __instance, int creditsEarned, out int __state)
        {
            __state = 0;

            try
            {
                if (_stageField == null || __instance == null || !__instance.IsServer) return;
                if (Convert.ToInt32(_stageField.GetValue(__instance)) != ExecuteStage) return;

                __state = creditsEarned;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not count HQoL sale: {e.Message}");
            }
        }

        private static void CountSale(int __state)
        {
            DepositItemsDeskPatches.SoldThisQuota += __state;
        }

        private static bool LoadMembers(Type networkType, Type? itemType)
        {
            if (itemType == null) return false;

            _instanceProperty = networkType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _storageField = AccessTools.Field(networkType, "netStorage");
            _totalField = AccessTools.Field(networkType, "totalStorageValue");
            _modifiedField = AccessTools.Field(networkType, "storageHasBeenModified");
            _nameField = AccessTools.Field(itemType, "itemName");
            _valueField = AccessTools.Field(itemType, "value");

            if (_instanceProperty == null
                || _storageField == null
                || _totalField == null
                || _modifiedField == null
                || _nameField == null
                || _valueField == null
                || _valueField.FieldType != typeof(int)
                || _modifiedField.FieldType != typeof(bool)
                || !typeof(NetworkVariable<int>).IsAssignableFrom(_totalField.FieldType))
                return false;

            Type listType = _storageField.FieldType;
            _countProperty = listType.GetProperty("Count");
            _itemProperty = listType.GetProperty("Item", itemType, new[] { typeof(int) });
            _removeAtMethod = listType.GetMethod("RemoveAt", new[] { typeof(int) });

            return _countProperty != null && _itemProperty != null && _removeAtMethod != null;
        }
    }
}
