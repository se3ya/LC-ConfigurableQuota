using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace ConfigurableQuota.Compat
{
    internal static class MoreShipUpgradesCompat
    {
        private const string LifeInsuranceTypeName = "MoreShipUpgrades.UpgradeComponents.TierUpgrades.Ship.LifeInsurance";
        private const string ReduceCreditCostMethodName = "ReduceCreditCostPercentage";
        private const string ScrapKeeperTypeName = "MoreShipUpgrades.UpgradeComponents.TierUpgrades.Items.ScrapKeeper";
        private const string CanKeepScrapMethodName = "CanKeepScrapBasedOnChance";

        private static MethodInfo? _reduceCreditCost;
        private static MethodInfo? _canKeepScrap;
        private static bool _reflectionReady;
        private static bool _reflectionAttempted;

        internal static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(ModGUIDs.MORE_SHIP_UPGRADES_GUID);

        internal static bool CanKeepScrap(GrabbableObject item)
        {
            if (item == null || !IsInstalled || !EnsureReflectionReady() || _canKeepScrap == null)
                return false;

            try
            {
                return _canKeepScrap.Invoke(null, new object[] { item }) is true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not read Scrap Keeper status: {e.Message}");
                return false;
            }
        }

        internal static float ApplyLifeInsurance(float creditPenaltyPercent)
        {
            if (creditPenaltyPercent <= 0f || !IsInstalled || !EnsureReflectionReady() || _reduceCreditCost == null)
                return creditPenaltyPercent;

            try
            {
                object? reduced = _reduceCreditCost.Invoke(null, new object[] { creditPenaltyPercent });
                if (reduced is not float value)
                    return creditPenaltyPercent;

                return Mathf.Clamp(value, 0f, creditPenaltyPercent);
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"Could not apply Life Insurance reduction: {e.Message}");
                return creditPenaltyPercent;
            }
        }

        private static bool EnsureReflectionReady()
        {
            if (_reflectionAttempted)
                return _reflectionReady;

            _reflectionAttempted = true;

            try
            {
                Type? lifeInsurance = AccessTools.TypeByName(LifeInsuranceTypeName);
                _reduceCreditCost = AccessTools.Method(lifeInsurance, ReduceCreditCostMethodName, new[] { typeof(float) });

                Type? scrapKeeper = AccessTools.TypeByName(ScrapKeeperTypeName);
                _canKeepScrap = AccessTools.Method(scrapKeeper, CanKeepScrapMethodName, new[] { typeof(GrabbableObject) });

                _reflectionReady = _reduceCreditCost != null || _canKeepScrap != null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to initialize LGU reflection: {e.Message}");
            }

            return _reflectionReady;
        }
    }
}
