using HarmonyLib;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    public enum PlayerScalingDirection
    {
        PerMissingPlayer,
        PerExtraPlayer
    }

    [HarmonyPatch(typeof(RoundManager))]
    internal static class DynamicLevelPatches
    {
        private static int _savedMinScrapValue;
        private static int _savedMaxScrapValue;
        private static int _savedMinScrap;
        private static int _savedMaxScrap;

        private static int _savedMaxEnemyPower;
        private static int _savedMaxOutsidePower;
        private static int _savedMaxDaytimePower;
        private static bool _enemyPowerSaved;

        [HarmonyPatch("GenerateNewFloor")]
        [HarmonyPrefix]
        [HarmonyAfter(new[] { Metadata.LLL_GUID, Metadata.LUNAR_CONFIG_GUID })]
        private static void GenerateNewFloor_Prefix(RoundManager __instance)
        {
            if (!ShouldRunServer(__instance, out var level)) return;
            if (!ConfigManager.DynamicInteriorSizeEnabled.Value) return;

            float factor = ComputePlayerFactor(
                ConfigManager.DynamicInteriorSizePlayerThreshold.Value,
                ConfigManager.DynamicInteriorSizeMultPerPlayer.Value,
                ConfigManager.DynamicInteriorSizeDirection.Value);
            float size = ConfigManager.DynamicInteriorSizeBase.Value * factor;
            level.factorySizeMultiplier = size;
            Plugin.Log.LogInfo($"Dynamic interior size - factorySizeMultiplier = {size:F2} (players={GetPlayerCount()}, factor={factor:F2}).");
        }

        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPrefix]
        [HarmonyAfter(new[] { Metadata.LLL_GUID, Metadata.LUNAR_CONFIG_GUID })]
        private static void SpawnScrapInLevel_Prefix(RoundManager __instance)
        {
            if (!ShouldRunServer(__instance, out var level)) return;
            _savedMinScrapValue = level.minTotalScrapValue;
            _savedMaxScrapValue = level.maxTotalScrapValue;
            _savedMinScrap = level.minScrap;
            _savedMaxScrap = level.maxScrap;
            if (ConfigManager.DynamicScrapValueEnabled.Value) ApplyScrapValue(level);
            if (ConfigManager.DynamicScrapAmountEnabled.Value) ApplyScrapAmount(level);
        }

        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPostfix]
        private static void SpawnScrapInLevel_Postfix(RoundManager __instance)
        {
            if (!ShouldRunServer(__instance, out var level)) return;
            level.minTotalScrapValue = _savedMinScrapValue;
            level.maxTotalScrapValue = _savedMaxScrapValue;
            level.minScrap = _savedMinScrap;
            level.maxScrap = _savedMaxScrap;
        }

        [HarmonyPatch("GenerateNewFloor")]
        [HarmonyPrefix]
        [HarmonyAfter(new[] { Metadata.LLL_GUID, Metadata.LUNAR_CONFIG_GUID })]
        private static void EnemyPower_GenerateNewFloor_Prefix(RoundManager __instance)
        {
            if (!ShouldRunServer(__instance, out var level)) return;
            if (!ConfigManager.DynamicEnemyPowerEnabled.Value) return;

            _savedMaxEnemyPower = level.maxEnemyPowerCount;
            _savedMaxOutsidePower = level.maxOutsideEnemyPowerCount;
            _savedMaxDaytimePower = level.maxDaytimeEnemyPowerCount;
            _enemyPowerSaved = true;

            float factor = ComputePlayerFactor(
                ConfigManager.DynamicEnemyPowerPlayerThreshold.Value,
                ConfigManager.DynamicEnemyPowerMultPerPlayer.Value,
                PlayerScalingDirection.PerExtraPlayer);
            factor = Mathf.Min(factor, Mathf.Max(1f, ConfigManager.DynamicEnemyPowerMaxFactor.Value));

            if (ConfigManager.DynamicEnemyPowerScaleInside.Value)
                level.maxEnemyPowerCount = Mathf.RoundToInt(_savedMaxEnemyPower * factor);
            if (ConfigManager.DynamicEnemyPowerScaleOutside.Value)
                level.maxOutsideEnemyPowerCount = Mathf.RoundToInt(_savedMaxOutsidePower * factor);
            if (ConfigManager.DynamicEnemyPowerScaleDaytime.Value)
                level.maxDaytimeEnemyPowerCount = Mathf.RoundToInt(_savedMaxDaytimePower * factor);

            Plugin.Log.LogInfo(
                $"Dynamic enemy power - factor={factor:F2} (players={GetPlayerCount()}), " +
                $"inside={level.maxEnemyPowerCount}, outside={level.maxOutsideEnemyPowerCount}, daytime={level.maxDaytimeEnemyPowerCount}.");
        }

        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        [HarmonyPostfix]
        private static void EnemyPower_RestoreOnRoundEnd(RoundManager __instance)
        {
            if (!_enemyPowerSaved) return;
            var level = __instance?.currentLevel;
            if (level != null)
            {
                level.maxEnemyPowerCount = _savedMaxEnemyPower;
                level.maxOutsideEnemyPowerCount = _savedMaxOutsidePower;
                level.maxDaytimeEnemyPowerCount = _savedMaxDaytimePower;
            }
            _enemyPowerSaved = false;
        }

        private static bool ShouldRun(RoundManager rm, out SelectableLevel level)
        {
            level = rm?.currentLevel!;
            if (level == null) return false;
            if (ConfigManager.DisableQuota.Value) return false;
            if (PenaltyHelpers.IsOnGordion()) return false;
            return true;
        }

        private static bool ShouldRunServer(RoundManager rm, out SelectableLevel level)
        {
            if (!PenaltyHelpers.IsServerSafe) { level = null!; return false; }
            return ShouldRun(rm, out level);
        }

        private static int GetPlayerCount()
        {
            return Mathf.Max(1, (StartOfRound.Instance?.connectedPlayersAmount ?? 0) + 1);
        }

        private static float ComputePlayerFactor(int threshold, float mult, PlayerScalingDirection direction)
        {
            int count = direction == PlayerScalingDirection.PerMissingPlayer
                ? Mathf.Max(0, threshold - GetPlayerCount())
                : Mathf.Max(0, GetPlayerCount() - threshold);
            return 1f + count * Mathf.Max(0f, mult);
        }

        private static void ApplyScrapValue(SelectableLevel level)
        {
            int baseMin = Mathf.Max(1, _savedMinScrapValue);
            int baseMax = Mathf.Max(baseMin, _savedMaxScrapValue);
            float factor = ComputePlayerFactor(
                ConfigManager.DynamicScrapValuePlayerThreshold.Value,
                ConfigManager.DynamicScrapValueMultPerPlayer.Value,
                ConfigManager.DynamicScrapValueDirection.Value);

            int offset = ConfigManager.DynamicScrapValueOffset.Value;
            int min = Mathf.RoundToInt(baseMin * ConfigManager.DynamicScrapValueMinMult.Value * factor) + offset;
            int max = Mathf.RoundToInt(baseMax * ConfigManager.DynamicScrapValueMaxMult.Value * factor) + offset;

            if (min < 1) min = 1;
            if (max < 1) max = 1;
            if (max < min) max = min;
            level.minTotalScrapValue = min;
            level.maxTotalScrapValue = max;
            Plugin.Log.LogInfo($"Dynamic scrap value: factor={factor:F2}, base=${baseMin}/${baseMax}, min/max=${min}/${max}.");
        }

        private static void ApplyScrapAmount(SelectableLevel level)
        {
            float factor = ComputePlayerFactor(
                ConfigManager.DynamicScrapAmountPlayerThreshold.Value,
                ConfigManager.DynamicScrapAmountMultPerPlayer.Value,
                ConfigManager.DynamicScrapAmountDirection.Value);

            int baseMinTotalScrapValue = Mathf.Max(0, _savedMinScrapValue);
            if (baseMinTotalScrapValue <= 0)
            {
                int baseMinScrap = Mathf.Max(0, _savedMinScrap);
                int baseMaxScrap = Mathf.Max(baseMinScrap, _savedMaxScrap);
                level.minScrap = baseMinScrap;
                level.maxScrap = baseMaxScrap;
                Plugin.Log.LogInfo(
                    $"Dynamic scrap amount - factor={factor:F2}, baseline scrap value <= 0 so keeping base min/max items={baseMinScrap}/{baseMaxScrap}.");
                return;
            }

            int divisor = Mathf.Max(1, ConfigManager.DynamicScrapAmountValuePerItem.Value);
            int scaled = Mathf.RoundToInt(baseMinTotalScrapValue * factor);
            int maxScrap = Mathf.Max(1, scaled / divisor);
            int cap = ConfigManager.DynamicScrapAmountCap.Value;
            if (cap >= 0) maxScrap = Mathf.Min(maxScrap, cap);
            int minScrap = Mathf.Max(1, Mathf.RoundToInt(maxScrap * ConfigManager.DynamicScrapAmountMinFraction.Value));
            if (minScrap > maxScrap) minScrap = maxScrap;
            level.minScrap = minScrap;
            level.maxScrap = maxScrap;
            Plugin.Log.LogInfo($"Dynamic scrap amount - factor={factor:F2}, min/max items={minScrap}/{maxScrap}.");
        }
    }
}
