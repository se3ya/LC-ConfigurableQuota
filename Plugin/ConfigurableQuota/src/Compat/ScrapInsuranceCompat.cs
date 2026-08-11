using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace ConfigurableQuota.Compat
{
    internal static class ScrapInsuranceCompat
    {
        private const string BehaviourTypeName = "ScrapInsurance.ScrapInsuranceBehaviour";
        private const string StatusMethodName = "GetScrapInsuranceStatus";

        private static MethodInfo? _statusMethod;
        private static bool _reflectionReady;
        private static bool _reflectionAttempted;

        internal static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(ModGUIDs.SCRAP_INSURANCE_GUID);

        internal static bool IsScrapInsured()
        {
            if (!IsInstalled || !EnsureReflectionReady())
                return false;

            try
            {
                return _statusMethod!.Invoke(null, null) is true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not read Scrap Insurance status: {e.Message}");
                return false;
            }
        }

        private static bool EnsureReflectionReady()
        {
            if (_reflectionAttempted)
                return _reflectionReady;

            _reflectionAttempted = true;

            try
            {
                Type? behaviour = AccessTools.TypeByName(BehaviourTypeName);
                _statusMethod = AccessTools.Method(behaviour, StatusMethodName, Type.EmptyTypes);

                _reflectionReady = _statusMethod != null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to initialize Scrap Insurance reflection: {e.Message}");
            }

            return _reflectionReady;
        }
    }
}
