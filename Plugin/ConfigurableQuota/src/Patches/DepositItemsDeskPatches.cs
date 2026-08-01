using HarmonyLib;
using Unity.Netcode;

namespace ConfigurableQuota.Patches
{
    [HarmonyPatch(typeof(DepositItemsDesk))]
    internal static class DepositItemsDeskPatches
    {
        internal static int SoldThisQuota;

        [HarmonyPatch("SellAndDisplayItemProfits")]
        [HarmonyPostfix]
        private static void SellAndDisplayItemProfits_Postfix(int profit)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            SoldThisQuota += profit;
        }
    }
}