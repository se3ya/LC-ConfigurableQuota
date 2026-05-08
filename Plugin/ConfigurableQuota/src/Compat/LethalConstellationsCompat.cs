using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;

namespace ConfigurableQuota.Compat
{
    internal static class LethalConstellationsCompat
    {
        private const string AssemblyName = "LethalConstellations";
        private const string CollectionsTypeName = "LethalConstellations.PluginCore.Collections";
        private const string CurrentConstellationFieldName = "CurrentConstellation";
        private const string ConstellationListFieldName = "ConstellationStuff";
        private const string ConstellationNameFieldName = "consName";

        private static Type? _collectionsType;
        private static FieldInfo? _currentConstellationField;
        private static FieldInfo? _constellationStuffField;
        private static FieldInfo? _constellationNameField;

        private static bool _loggedReflectionFailure;

        internal static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(Metadata.LETHAL_CONSTELLATIONS_GUID);

        internal static bool TryGetCurrentConstellationName(out string constellationName)
        {
            constellationName = string.Empty;

            if (!IsInstalled || !EnsureReflectionReady())
                return false;

            try
            {
                if (_currentConstellationField?.GetValue(null) is string rawName)
                {
                    constellationName = rawName.Trim();
                    return constellationName.Length > 0;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not read current constellation name: {e.Message}");
            }

            return false;
        }

        internal static List<string> GetKnownConstellationNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!IsInstalled || !EnsureReflectionReady())
                return new List<string>();

            try
            {
                if (_constellationStuffField?.GetValue(null) is not IEnumerable list)
                    return new List<string>();

                foreach (object? entry in list)
                {
                    if (entry == null)
                        continue;

                    string name = GetConstellationName(entry);
                    if (name.Length > 0)
                        names.Add(name);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not collect constellation names: {e.Message}");
            }

            return names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string GetConstellationName(object entry)
        {
            try
            {
                if (_constellationNameField == null || _constellationNameField.DeclaringType != entry.GetType())
                {
                    _constellationNameField = entry.GetType().GetField(
                        ConstellationNameFieldName,
                        BindingFlags.Public | BindingFlags.Instance
                    );
                }

                if (_constellationNameField?.GetValue(entry) is string rawName)
                    return rawName.Trim();
            }
            catch
            {
                // ignore malformed entries and keep enmerating
            }

            return string.Empty;
        }

        private static bool EnsureReflectionReady()
        {
            if (_collectionsType != null && _currentConstellationField != null && _constellationStuffField != null)
                return true;

            try
            {
                Assembly? lcAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => assembly.GetName().Name == AssemblyName);

                if (lcAssembly == null)
                {
                    return false;
                }

                _collectionsType = lcAssembly.GetType(CollectionsTypeName);
                _currentConstellationField = _collectionsType?.GetField(
                    CurrentConstellationFieldName,
                    BindingFlags.Public | BindingFlags.Static
                );
                _constellationStuffField = _collectionsType?.GetField(
                    ConstellationListFieldName,
                    BindingFlags.Public | BindingFlags.Static
                );

                bool ready = _collectionsType != null
                    && _currentConstellationField != null
                    && _constellationStuffField != null;

                if (!ready)
                {
                    LogReflectionWarningOnce("Required LethalConstellations symbols were not found. Falling back to global deadlines.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                LogReflectionWarningOnce($"Failed to initialize LethalConstellations reflection: {e.Message}");
                return false;
            }
        }

        private static void LogReflectionWarningOnce(string message)
        {
            if (_loggedReflectionFailure)
                return;

            _loggedReflectionFailure = true;
            Plugin.Log.LogWarning(message);
        }
    }
}
