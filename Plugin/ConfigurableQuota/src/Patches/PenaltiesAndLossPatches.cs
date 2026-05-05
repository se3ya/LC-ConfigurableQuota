using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Patches
{
    internal static class PenaltyHelpers
    {
        public static bool IsServerSafe => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        public static (int dead, int total, int recovered) CountDeathsAndRecovered()
        {
            var sor = StartOfRound.Instance;
            if (sor == null) return (0, 0, 0);

            int dead = 0;
            int total = 0;
            int recovered = 0;

            foreach (var player in sor.allPlayerScripts)
            {
                if (player == null) continue;

                bool isControlled = player.isPlayerControlled;
                bool isDead = player.isPlayerDead;

                if (isControlled || isDead)
                {
                    total++;
                }

                if (!isDead) continue;

                dead++;
                Transform? bodyTransform = player.deadBody?.transform;
                if (bodyTransform != null && IsPositionInsideShip(bodyTransform.position))
                {
                    recovered++;
                }
            }

            return (dead, Math.Max(total, 1), Mathf.Clamp(recovered, 0, dead));
        }

        public static bool IsPositionInsideShip(Vector3 pos)
        {
            try
            {
                var shipBounds = StartOfRound.Instance?.shipBounds;
                return shipBounds != null && shipBounds.bounds.Contains(pos);
            }
            catch { return false; }
        }

        public static bool IsOnGordion()
        {
            try
            {
                var level = StartOfRound.Instance?.currentLevel;
                if (level == null) return false;

                return level.sceneName == "CompanyBuilding";
            }
            catch { return false; }
        }

        public static float ComputePenaltyPercent(bool dynamicMode, float percentPerPlayer, float cap, float threshold, float recoveryBonus,
            int dead, int total, int recovered)
        {
            if (dead <= 0 || total <= 0) return 0f;

            float pct = dynamicMode ? (float)dead / total : dead * Mathf.Max(0f, percentPerPlayer);

            if (recovered > 0 && dead > 0)
            {
                float recoveredRatio = Mathf.Clamp01((float)recovered / dead);
                pct *= Mathf.Clamp01(1f - (Mathf.Clamp01(recoveryBonus) * recoveredRatio));
            }

            if (cap >= 0f) pct = Mathf.Min(pct, Mathf.Clamp01(cap));
            return pct < threshold ? 0f : Mathf.Clamp01(pct);
        }
    }

    [HarmonyPatch(typeof(RoundManager))]
    internal static class PenaltiesOnLandingPatch
    {
        internal static bool _appliedThisRound;
        private static bool _creditScheduled;
        internal static bool _lossesAppliedThisRound;

        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        [HarmonyPrefix]
        private static bool DespawnPrefix(bool despawnAllItems)
        {
            try
            {
                if (!PenaltyHelpers.IsServerSafe) return true;

                bool atCompany = PenaltyHelpers.IsOnGordion();
                var (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();

                if (!despawnAllItems && !atCompany && dead >= total && !_lossesAppliedThisRound)
                {
                    DespawnFacilityItems();

                    ApplyLossesWhenAllDead();
                    _lossesAppliedThisRound = true;

                    if (ConfigManager.CreditPenaltiesEnabled.Value)
                    {
                        ScheduleCreditPenalty(dead, total, recovered);
                    }
                    if (ConfigManager.QuotaPenaltiesEnabled.Value)
                    {
                        ApplyQuotaPenalty(dead, total, recovered);
                    }

                    _appliedThisRound = true;
                    Plugin.Log.LogDebug("Bypassed vanilla despawn to preserve kept items");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Error in despawn prefix: {e.Message}");
                return true;
            }
        }

        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        [HarmonyPostfix]
        private static void DespawnPostfix(bool despawnAllItems)
        {
            try
            {
                if (despawnAllItems || !PenaltyHelpers.IsServerSafe) return;

                if (_appliedThisRound) return;

                var (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();
                if (dead <= 0) return;

                bool atCompany = PenaltyHelpers.IsOnGordion();

                if (ConfigManager.CreditPenaltiesEnabled.Value && (!atCompany || ConfigManager.CreditPenaltiesOnGordion.Value))
                {
                    ScheduleCreditPenalty(dead, total, recovered);
                }

                if (ConfigManager.QuotaPenaltiesEnabled.Value && (!atCompany || ConfigManager.QuotaPenaltiesOnGordion.Value))
                {
                    ApplyQuotaPenalty(dead, total, recovered);
                }

                _appliedThisRound = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Error in despawn postfix: {e.Message}");
            }
        }

        private static void ScheduleCreditPenalty(int dead, int total, int recovered)
        {
            try
            {
                if (_creditScheduled) return;

                var sor = StartOfRound.Instance;
                if (sor == null) return;

                int currentCredits = GetCurrentCredits();
                float pct = PenaltyHelpers.ComputePenaltyPercent(
                    ConfigManager.CreditPenaltiesDynamic.Value,
                    ConfigManager.CreditPenaltyPercentPerPlayer.Value,
                    ConfigManager.CreditPenaltyPercentCap.Value,
                    ConfigManager.CreditPenaltyPercentThreshold.Value,
                    ConfigManager.CreditPenaltyRecoveryBonus.Value,
                    dead, total, recovered);

                if (pct <= 0f) return;

                int desiredFinal = Mathf.Max(0, currentCredits - Mathf.RoundToInt(currentCredits * pct));
                _creditScheduled = true;
                sor.StartCoroutine(FinalizeCreditPenaltyAfterDelay(desiredFinal));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to schedule credit penalty: {ex.Message}");
            }
        }

        private static int GetCurrentCredits()
        {
            try
            {
                var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                if (term != null) return term.groupCredits;
            }
            catch { }

            return 0;
        }

        private static System.Collections.IEnumerator FinalizeCreditPenaltyAfterDelay(int desiredFinal)
        {
            yield return new WaitForSeconds(1.5f);

            try
            {
                var sor = StartOfRound.Instance;
                if (sor == null) yield break;

                int before = GetCurrentCredits();
                SetCredits(desiredFinal);
                Plugin.Log.LogInfo($"[Penalty] Credits: {before} → {desiredFinal} (-{before - desiredFinal})");
            }
            finally
            {
                _creditScheduled = false;
            }
        }

        private static void SetCredits(int value)
        {
            try
            {
                var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                if (term != null)
                    term.SyncGroupCreditsServerRpc(value, term.numberOfItemsInDropship);
            }
            catch { }
        }

        private static void ApplyQuotaPenalty(int dead, int total, int recovered)
        {
            float pct = PenaltyHelpers.ComputePenaltyPercent(
                ConfigManager.QuotaPenaltiesDynamic.Value,
                ConfigManager.QuotaPenaltyPercentPerPlayer.Value,
                ConfigManager.QuotaPenaltyPercentCap.Value,
                ConfigManager.QuotaPenaltyPercentThreshold.Value,
                ConfigManager.QuotaPenaltyRecoveryBonus.Value,
                dead, total, recovered);

            if (pct <= 0f) return;

            var tod = TimeOfDay.Instance;
            if (tod != null)
            {
                int delta = Mathf.RoundToInt(Math.Max(1, tod.profitQuota) * pct);
                int newQuota = Mathf.Max(1, tod.profitQuota + delta);
                tod.profitQuota = newQuota;

                NetworkSync.SyncQuotaToClients(newQuota);

                int oldQuota = tod.profitQuota - delta;
                Plugin.Log.LogInfo($"[Penalty] Quota: {oldQuota} → {newQuota} (+{delta}, {pct:P0} penalty, {dead}/{total} dead)");
            }
        }

        private static void DespawnFacilityItems()
        {
            try
            {
                var allGrab = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                if (allGrab == null || allGrab.Length == 0) return;

                Transform? shipRoot = null;
                try { shipRoot = StartOfRound.Instance?.shipBounds?.transform; } catch { }

                int despawnedCount = 0;
                foreach (var g in allGrab)
                {
                    try
                    {
                        if (!IsShipItem(g, shipRoot))
                        {
                            DespawnObject(g);
                            despawnedCount++;
                        }
                    }
                    catch { }
                }

                Plugin.Log.LogDebug($"Despawned {despawnedCount} facility items");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Error despawning facility items: {ex.Message}");
            }
        }

        private static void ApplyLossesWhenAllDead()
        {
            try
            {
                var allGrab = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                if (allGrab == null || allGrab.Length == 0) return;

                Transform? shipRoot = null;
                try { shipRoot = StartOfRound.Instance?.shipBounds?.transform; } catch { }

                var shipItems = allGrab.Where(g => IsShipItem(g, shipRoot)).ToArray();
                var shipScrap = shipItems.Where(g => g.itemProperties.isScrap).ToArray();
                var shipEquip = shipItems.Where(g => !g.itemProperties.isScrap && !IsBodyOrBlacklisted(g)).ToArray();

                Plugin.Log.LogDebug($"Ship items: {shipScrap.Length} scrap, {shipEquip.Length} equipment");

                if (ConfigManager.ValueLossEnabled.Value && shipScrap.Length > 0)
                {
                    ApplyValueLoss(shipScrap);
                }

                if (ConfigManager.ScrapLossEnabled.Value && shipScrap.Length > 0)
                {
                    SelectAndRemoveScrap(shipScrap);
                }

                if (ConfigManager.EquipmentLossEnabled.Value && shipEquip.Length > 0)
                {
                    SelectAndRemoveEquipment(shipEquip);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Error applying losses: {ex.Message}");
            }
        }

        private static bool IsShipItem(GrabbableObject g, Transform? shipRoot)
        {
            if (g == null || g.itemProperties == null || !g.isInShipRoom) return false;

            try
            {
                var no = g.GetComponent<NetworkObject>();
                if (no == null || !no.IsSpawned) return false;
            }
            catch { return false; }

            if (shipRoot == null) return true;

            var t = g.transform;
            for (int depth = 0; depth < 8 && t != null; depth++)
            {
                if (t == shipRoot) return true;
                t = t.parent;
            }
            return true;
        }

        private static bool IsBodyOrBlacklisted(GrabbableObject g)
        {
            if (g == null) return true;
            if (g is RagdollGrabbableObject) return true;
            if (g is ClipboardItem) return true;

            try
            {
                string name = (g.itemProperties?.itemName ?? g.name);
                return name.IndexOf("sticky note", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private static void SelectAndRemoveScrap(GrabbableObject[] scrapItems)
        {
            int maxRemove = Mathf.Max(0, ConfigManager.MaxLostScrapItems.Value);
            float safeChance = Mathf.Clamp01(ConfigManager.ItemsSafeChance.Value);
            float loseChance = Mathf.Clamp01(ConfigManager.LoseEachScrapChance.Value);

            int eligible = 0;
            int removedCount = 0;
            List<string> removedNames = new();

            foreach (var g in scrapItems)
            {
                try
                {
                    if (g == null || g.itemProperties?.isScrap != true) continue;

                    eligible++;

                    if (maxRemove > 0 && removedCount >= maxRemove) continue;

                    if (UnityEngine.Random.value < safeChance) continue;

                    if (UnityEngine.Random.value < loseChance)
                    {
                        DespawnObject(g);
                        removedCount++;
                        removedNames.Add(g.itemProperties.itemName);
                    }
                }
                catch { }
            }

            Plugin.Log.LogInfo($"Scrap: {removedCount}/{eligible} removed [{string.Join(", ", removedNames)}]");
        }

        private static void SelectAndRemoveEquipment(GrabbableObject[] equipItems)
        {
            int maxRemove = Mathf.Max(0, ConfigManager.MaxLostEquipmentItems.Value);
            float loseChance = Mathf.Clamp01(ConfigManager.LoseEachEquipmentChance.Value);

            int eligible = 0;
            int removedCount = 0;
            List<string> removedNames = new();

            foreach (var g in equipItems)
            {
                try
                {
                    if (g == null || g.itemProperties?.isScrap != false) continue;

                    eligible++;

                    if (maxRemove > 0 && removedCount >= maxRemove) continue;

                    if (UnityEngine.Random.value < loseChance)
                    {
                        DespawnObject(g);
                        removedCount++;
                        removedNames.Add(g.itemProperties.itemName);
                    }
                }
                catch { }
            }

            Plugin.Log.LogInfo($"[Losses] Equipment: {removedCount}/{eligible} removed [{string.Join(", ", removedNames)}]");
        }

        private static void ApplyValueLoss(GrabbableObject[] scrapItems)
        {
            float pct = Mathf.Clamp01(ConfigManager.ValueLossPercent.Value);
            if (pct <= 0f) return;

            float multiplier = 1f - pct;
            int affected = 0;
            int totalOldValue = 0;
            int totalNewValue = 0;
            List<SyncValueLossData> syncData = new();

            foreach (var g in scrapItems)
            {
                try
                {
                    if (g?.itemProperties?.isScrap == true && g.scrapValue > 0)
                    {
                        int oldValue = g.scrapValue;
                        int newValue = Mathf.Max(0, Mathf.RoundToInt(g.scrapValue * multiplier));

                        g.scrapValue = newValue;

                        try
                        {
                            g.SetScrapValue(newValue);
                        }
                        catch { }

                        syncData.Add(new SyncValueLossData(g.GetInstanceID(), newValue));

                        totalOldValue += oldValue;
                        totalNewValue += newValue;
                        affected++;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Error reducing value for item: {ex.Message}");
                }
            }

            if (syncData.Count > 0)
                NetworkSync.SyncValueLossToClients(syncData.ToArray());

            Plugin.Log.LogInfo($"Value: {affected} items reduced by {pct:P0} (${totalOldValue} → ${totalNewValue})");
        }

        private static void DespawnObject(GrabbableObject g)
        {
            try
            {
                var no = g.GetComponent<NetworkObject>();
                if (no != null && no.IsSpawned)
                {
                    no.Despawn(true);
                }
                else
                {
                    UnityEngine.Object.Destroy(g.gameObject);
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(StartOfRound))]
    internal static class ResetPenaltyFlags
    {
        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        private static void ResetFlagsOnNewGame()
        {
            PenaltiesOnLandingPatch._appliedThisRound = false;
            PenaltiesOnLandingPatch._lossesAppliedThisRound = false;
        }
    }
}
