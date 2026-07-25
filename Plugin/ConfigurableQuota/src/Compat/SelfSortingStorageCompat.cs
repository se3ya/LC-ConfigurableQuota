using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;

namespace ConfigurableQuota.Compat
{
    internal static class SelfSortingStorageCompat
    {
        private const string AssemblyName = "SelfSortingStorage";
        private const string SmartCupboardTypeName = "SelfSortingStorage.Cupboard.SmartCupboard";
        private const string PlacedItemsFieldName = "placedItems";

        private static Type? _smartCupboardType;
        private static FieldInfo? _placedItemsField;
        private static bool _reflectionReady;
        private static bool _reflectionAttempted;

        internal static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(ModGUIDs.SELF_SORTING_STORAGE_GUID);

        internal static HashSet<GrabbableObject> GetStoredItems()
        {
            var stored = new HashSet<GrabbableObject>();

            if (!IsInstalled || !EnsureReflectionReady())
                return stored;

            try
            {
                foreach (UnityEngine.Object cupboard in UnityEngine.Object.FindObjectsOfType(_smartCupboardType))
                {
                    if (_placedItemsField!.GetValue(cupboard) is not IDictionary placedItems)
                        continue;

                    foreach (object? value in placedItems.Values)
                    {
                        if (value is GrabbableObject item && item != null)
                            stored.Add(item);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not read SelfSortingStorage stored items: {e.Message}");
            }

            return stored;
        }

        private static bool EnsureReflectionReady()
        {
            if (_reflectionAttempted)
                return _reflectionReady;

            _reflectionAttempted = true;

            try
            {
                Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == AssemblyName);

                _smartCupboardType = assembly?.GetType(SmartCupboardTypeName);
                _placedItemsField = _smartCupboardType?.GetField(
                    PlacedItemsFieldName,
                    BindingFlags.Public | BindingFlags.Instance
                );

                _reflectionReady = _smartCupboardType != null && _placedItemsField != null;

                if (!_reflectionReady)
                    Plugin.Log.LogWarning("SelfSortingStorage is present but SmartCupboard.placedItems was not found, stored items are not protected from crew wipe loss.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to initialize SelfSortingStorage reflection: {e.Message}");
            }

            return _reflectionReady;
        }
    }
}