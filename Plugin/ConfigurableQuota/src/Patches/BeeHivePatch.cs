using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Patches;

[HarmonyPatch(typeof(RedLocustBees))]
internal static class BeeHivePatch
{
    private static readonly HashSet<RedLocustBees> _rolled = new HashSet<RedLocustBees>();

    [HarmonyPatch(nameof(RedLocustBees.SpawnHiveClientRpc))]
    [HarmonyPrefix]
    private static void SpawnHiveClientRpc_Prefix(RedLocustBees __instance, NetworkObjectReference hiveObject, ref int hiveScrapValue, Vector3 hivePosition)
    {
        if (!__instance.IsServer || _rolled.Remove(__instance)) return;
        if (!ConfigManager.BeeHiveEnabled.Value) return;

        _rolled.Add(__instance);

        float distance = Vector3.Distance(hivePosition, StartOfRound.Instance.shipLandingPosition.position);
        bool near = distance < ConfigManager.BeeHiveDistance.Value;
        int first = near ? ConfigManager.BeeHiveNearMin.Value : ConfigManager.BeeHiveFarMin.Value;
        int second = near ? ConfigManager.BeeHiveNearMax.Value : ConfigManager.BeeHiveFarMax.Value;

        var random = new System.Random(StartOfRound.Instance.randomMapSeed + 1314 + (int)hiveObject.NetworkObjectId);
        hiveScrapValue = random.Next(Mathf.Min(first, second), Mathf.Max(first, second) + 1);
    }
}
