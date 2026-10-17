using System;
using HarmonyLib;
using UnityEngine;

namespace ConfigurableQuota.Patches;

public enum PlayerScalingDirection
{
    PerMissingPlayer,
    PerExtraPlayer
}

[HarmonyPatch(typeof(RoundManager))]
internal static class DynamicLevelPatches
{
    private static int _savedMinScrap;
    private static int _savedMaxScrap;
    private static float _savedValueMultiplier;
    private static bool _valueScaled;
    private static SelectableLevel? _scrapLevel;

    private static int _savedMaxEnemyPower;
    private static int _savedMaxOutsidePower;
    private static int _savedMaxDaytimePower;
    private static SelectableLevel? _enemyPowerLevel;

    internal static float InteriorScale = 1f;
    private static SelectableLevel? _scaledLevel;
    private static float _savedInteriorSize;

    internal static float GetInteriorScale()
    {
        if (!ConfigManager.DynamicInteriorSizeEnabled.Value || ConfigManager.DisableQuota.Value) return 1f;
        if (PenaltyHelpers.IsAtCompany()) return 1f;

        float factor = GetPlayerFactor(
            ConfigManager.DynamicInteriorSizePlayerThreshold.Value,
            ConfigManager.DynamicInteriorSizeMultPerPlayer.Value,
            ConfigManager.DynamicInteriorSizeDirection.Value);
        return Mathf.Max(0.1f, ConfigManager.DynamicInteriorSizeBase.Value * factor);
    }

    [HarmonyPatch("GenerateNewFloor")]
    [HarmonyPrefix]
    [HarmonyAfter(new[] { ModGUIDs.LethalLevelLoader, ModGUIDs.LunarConfig })]
    private static void GenerateNewFloor_Prefix(RoundManager __instance)
    {
        RestoreInteriorSize();

        var level = __instance.currentLevel;
        if (level == null || Mathf.Approximately(InteriorScale, 1f)) return;

        _scaledLevel = level;
        _savedInteriorSize = level.factorySizeMultiplier;
        level.factorySizeMultiplier = _savedInteriorSize * InteriorScale;

        if (PenaltyHelpers.IsServer)
            Plugin.Log.LogInfo($"Interior size set to {level.factorySizeMultiplier:F2}, the moon's {_savedInteriorSize:F2} times {InteriorScale:F2} for {PenaltyHelpers.GetPlayerCount()} players.");
    }

    [HarmonyPatch("DespawnPropsAtEndOfRound")]
    [HarmonyPostfix]
    private static void RestoreInteriorOnRoundEnd()
    {
        RestoreInteriorSize();

        if (!PenaltyHelpers.IsServer)
            InteriorScale = 1f;
    }

    internal static void RestoreInteriorSize()
    {
        if (_scaledLevel == null) return;

        _scaledLevel.factorySizeMultiplier = _savedInteriorSize;
        _scaledLevel = null;
    }

    [HarmonyPatch("SpawnScrapInLevel")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyAfter(new[] { ModGUIDs.LethalLevelLoader, ModGUIDs.LunarConfig, ModGUIDs.WeatherRegistry })]
    private static void SpawnScrapInLevel_Prefix(RoundManager __instance)
    {
        RestoreScrapCounts();
        if (!ShouldRunServer(__instance, out var level)) return;

        _scrapLevel = level;
        _savedMinScrap = level.minScrap;
        _savedMaxScrap = level.maxScrap;
        _savedValueMultiplier = __instance.scrapValueMultiplier;

        if (ConfigManager.DynamicScrapValueEnabled.Value) ApplyScrapValue(__instance);
        if (ConfigManager.DynamicScrapAmountEnabled.Value) ApplyScrapAmount(level);
    }

    [HarmonyPatch("SpawnScrapInLevel")]
    [HarmonyFinalizer]
    private static void SpawnScrapInLevel_Finalizer(RoundManager __instance)
    {
        if (_valueScaled)
            __instance.scrapValueMultiplier = _savedValueMultiplier;

        _valueScaled = false;
        RestoreScrapCounts();
    }

    private static void RestoreScrapCounts()
    {
        if (_scrapLevel == null) return;

        _scrapLevel.minScrap = _savedMinScrap;
        _scrapLevel.maxScrap = _savedMaxScrap;
        _scrapLevel = null;
    }

    [HarmonyPatch("GenerateNewFloor")]
    [HarmonyPrefix]
    [HarmonyAfter(new[] { ModGUIDs.LethalLevelLoader, ModGUIDs.LunarConfig })]
    private static void EnemyPower_GenerateNewFloor_Prefix(RoundManager __instance)
    {
        RestoreEnemyPower();
        if (!ShouldRunServer(__instance, out var level)) return;
        if (!ConfigManager.DynamicEnemyPowerEnabled.Value) return;

        _enemyPowerLevel = level;
        _savedMaxEnemyPower = level.maxEnemyPowerCount;
        _savedMaxOutsidePower = level.maxOutsideEnemyPowerCount;
        _savedMaxDaytimePower = level.maxDaytimeEnemyPowerCount;

        float factor = GetPlayerFactor(
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
            $"Enemy power set for {PenaltyHelpers.GetPlayerCount()} players, factor {factor:F2}. " +
            $"Inside {level.maxEnemyPowerCount}, outside {level.maxOutsideEnemyPowerCount}, daytime {level.maxDaytimeEnemyPowerCount}.");
    }

    [HarmonyPatch("DespawnPropsAtEndOfRound")]
    [HarmonyPostfix]
    private static void EnemyPower_RestoreOnRoundEnd()
    {
        RestoreEnemyPower();
    }

    internal static void RestoreEnemyPower()
    {
        if (_enemyPowerLevel == null) return;

        _enemyPowerLevel.maxEnemyPowerCount = _savedMaxEnemyPower;
        _enemyPowerLevel.maxOutsideEnemyPowerCount = _savedMaxOutsidePower;
        _enemyPowerLevel.maxDaytimeEnemyPowerCount = _savedMaxDaytimePower;
        _enemyPowerLevel = null;
    }

    private static bool ShouldRun(RoundManager rm, out SelectableLevel level)
    {
        level = rm.currentLevel;
        if (level == null) return false;
        if (ConfigManager.DisableQuota.Value) return false;
        if (PenaltyHelpers.IsAtCompany()) return false;
        return true;
    }

    private static bool ShouldRunServer(RoundManager rm, out SelectableLevel level)
    {
        if (!PenaltyHelpers.IsServer) { level = null!; return false; }
        return ShouldRun(rm, out level);
    }

    private static float GetPlayerFactor(int threshold, float mult, PlayerScalingDirection direction)
    {
        int count = direction == PlayerScalingDirection.PerMissingPlayer
            ? Mathf.Max(0, threshold - PenaltyHelpers.GetPlayerCount())
            : Mathf.Max(0, PenaltyHelpers.GetPlayerCount() - threshold);
        return 1f + (count * Mathf.Max(0f, mult));
    }

    private static void ApplyScrapValue(RoundManager rm)
    {
        float factor = GetPlayerFactor(
            ConfigManager.DynamicScrapValuePlayerThreshold.Value,
            ConfigManager.DynamicScrapValueMultPerPlayer.Value,
            ConfigManager.DynamicScrapValueDirection.Value);

        float low = ConfigManager.DynamicScrapValueMinMult.Value;
        float high = Mathf.Max(low, ConfigManager.DynamicScrapValueMaxMult.Value);
        var random = new System.Random(StartOfRound.Instance.randomMapSeed + 2718);
        float extra = low + (float)random.NextDouble() * (high - low);

        rm.scrapValueMultiplier = _savedValueMultiplier * factor * extra;
        _valueScaled = true;
        Plugin.Log.LogInfo($"Scrap value multiplier set to {rm.scrapValueMultiplier:F2}, the moon's {_savedValueMultiplier:F2} times {factor * extra:F2}.");
    }

    private static void ApplyScrapAmount(SelectableLevel level)
    {
        float factor = GetPlayerFactor(
            ConfigManager.DynamicScrapAmountPlayerThreshold.Value,
            ConfigManager.DynamicScrapAmountMultPerPlayer.Value,
            ConfigManager.DynamicScrapAmountDirection.Value);

        int minScrap = Mathf.Max(1, Mathf.RoundToInt(_savedMinScrap * factor));
        int maxScrap = Mathf.Max(minScrap, Mathf.RoundToInt(_savedMaxScrap * factor));

        int cap = ConfigManager.DynamicScrapAmountCap.Value;
        if (cap >= 0)
        {
            maxScrap = Mathf.Min(maxScrap, cap);
            minScrap = Mathf.Min(minScrap, maxScrap);
        }

        level.minScrap = minScrap;
        level.maxScrap = maxScrap;
        Plugin.Log.LogInfo($"Scrap amount set to {minScrap} to {maxScrap} items from the moon's {_savedMinScrap} to {_savedMaxScrap}, factor {factor:F2}.");
    }
}

[HarmonyPatch(typeof(StartOfRound))]
internal static class InteriorScaleSyncPatch
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void Start_Postfix()
    {
        DynamicLevelPatches.RestoreInteriorSize();
        DynamicLevelPatches.RestoreEnemyPower();
        DynamicLevelPatches.InteriorScale = 1f;
    }

    [HarmonyPatch(nameof(StartOfRound.OnDestroy))]
    [HarmonyPostfix]
    private static void OnDestroy_Postfix()
    {
        DynamicLevelPatches.RestoreInteriorSize();
        DynamicLevelPatches.RestoreEnemyPower();
    }

    [HarmonyPatch(nameof(StartOfRound.StartGame))]
    [HarmonyPrefix]
    private static void StartGame_Prefix(StartOfRound __instance)
    {
        if (!__instance.IsServer || !__instance.inShipPhase) return;

        try
        {
            float scale = DynamicLevelPatches.GetInteriorScale();
            DynamicLevelPatches.InteriorScale = scale;
            NetworkSync.SyncInteriorScaleToClients(scale);
        }
        catch (Exception e)
        {
            DynamicLevelPatches.InteriorScale = 1f;
            Plugin.Log.LogWarning($"Could not work out the interior size, using the normal size: {e.Message}");
        }
    }
}
