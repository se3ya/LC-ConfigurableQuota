using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using ConfigurableQuota.Compat;

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
            RagdollGrabbableObject[]? _ragdollCache = null;

            foreach (var player in sor.allPlayerScripts)
            {
                if (player == null) continue;

                bool isControlled = player.isPlayerControlled;
                bool isDead = player.isPlayerDead;

                if (isControlled || isDead)
                    total++;

                if (!isDead) continue;

                dead++;

                var bodyInfo = player.deadBody;
                RagdollGrabbableObject? ragdoll = bodyInfo?.grabBodyObject as RagdollGrabbableObject;

                if (ragdoll == null)
                {
                    _ragdollCache ??= UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>();

                    foreach (var r in _ragdollCache)
                    {
                        if (r?.GetComponent<DeadBodyInfo>()?.playerScript == player)
                        {
                            ragdoll = r;
                            break;
                        }
                    }
                }

                bool inShip = false;
                if (ragdoll != null)
                {
                    bool flagCheck = ragdoll.isInShipRoom;
                    bool posCheck = IsPositionInsideShip(ragdoll.transform.position);
                    inShip = flagCheck || posCheck;
                }

                if (inShip) recovered++;
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

            float clampedCap = cap >= 0f ? Mathf.Clamp01(cap) : 1f;

            float pct = dynamicMode
                ? ((float)dead / total) * clampedCap
                : dead * Mathf.Max(0f, percentPerPlayer);

            if (recovered > 0 && dead > 0)
            {
                float recoveredRatio = Mathf.Clamp01((float)recovered / dead);
                pct *= Mathf.Clamp01(1f - (Mathf.Clamp01(recoveryBonus) * recoveredRatio));
            }

            if (!dynamicMode && cap >= 0f) pct = Mathf.Min(pct, clampedCap);
            return pct < threshold ? 0f : Mathf.Clamp01(pct);
        }
    }

    [HarmonyPatch(typeof(RoundManager))]
    internal static class PenaltiesOnLandingPatch
    {
        internal static bool _appliedThisRound;
        private static bool _creditScheduled;
        internal static bool _lossesAppliedThisRound;

        internal static int CachedDead;
        internal static int CachedTotal;
        internal static int CachedRecovered;
        internal static bool HasPenaltyCache;
        internal static int CachedQuotaPenaltyDelta;

        internal static int CachedShipScrapBeforeLoss;
        internal static int CachedShipScrapAfterLoss;
        internal static bool HasScrapLossSummary;
        internal static bool HasAllDeadSnapshot;

        internal static void CachePenaltyCounts(int dead, int total, int recovered)
        {
            CachedDead = dead;
            CachedTotal = total;
            CachedRecovered = recovered;
            HasPenaltyCache = true;
        }

        internal static void CacheScrapLossSummary(int beforeValue, int afterValue)
        {
            int before = Mathf.Max(0, beforeValue);
            int after = Mathf.Clamp(afterValue, 0, before);

            CachedShipScrapBeforeLoss = before;
            CachedShipScrapAfterLoss = after;
            HasScrapLossSummary = before > 0;
        }

        internal static void ClearScrapLossSummary()
        {
            CachedShipScrapBeforeLoss = 0;
            CachedShipScrapAfterLoss = 0;
            HasScrapLossSummary = false;
        }

        internal static bool TryGetScrapLossSummary(out int beforeValue, out int afterValue, out float lostPercent)
        {
            beforeValue = CachedShipScrapBeforeLoss;
            afterValue = CachedShipScrapAfterLoss;
            lostPercent = 0f;

            if (!HasScrapLossSummary || beforeValue <= 0)
                return false;

            lostPercent = Mathf.Clamp01((beforeValue - afterValue) / (float)beforeValue);
            return true;
        }

        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        [HarmonyPrefix]
        private static bool DespawnPrefix(bool despawnAllItems)
        {
            try
            {
                if (!PenaltyHelpers.IsServerSafe) return true;

                HasPenaltyCache = false;
                HasAllDeadSnapshot = false;
                CachedQuotaPenaltyDelta = 0;
                ClearScrapLossSummary();

                bool atCompany = PenaltyHelpers.IsOnGordion();
                var (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();

                if (!despawnAllItems && !atCompany && dead >= total && !_lossesAppliedThisRound)
                {
                    CollectVehicleItems();
                    MarkBeltBagContentsAsShipItems();

                    DespawnFacilityItems();

                    ApplyLossesWhenAllDead();
                    MarkSurvivingItemsAsPersisted();
                    _lossesAppliedThisRound = true;
                    HasAllDeadSnapshot = true;

                    CachePenaltyCounts(dead, total, recovered);

                    HudQuotaAnimationPatch.TryApplyAdvancedFeaturesEndscreen();

                    if (ConfigManager.CreditPenaltiesEnabled.Value)
                    {
                        ScheduleCreditPenalty(dead, total, recovered);
                    }
                    if (ConfigManager.QuotaPenaltiesEnabled.Value)
                    {
                        ApplyQuotaPenalty(dead, total, recovered);
                    }
                    if (ConfigManager.RolloverWipePenalty.Value > 0f)
                    {
                        ApplyRolloverWipePenalty();
                    }

                    _appliedThisRound = true;
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

                CachePenaltyCounts(dead, total, recovered);

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

                pct = MoreShipUpgradesCompat.ApplyLifeInsurance(pct);

                if (pct <= 0f) return;

                int desiredFinal = Mathf.Max(0, currentCredits - Mathf.RoundToInt(currentCredits * pct));
                _creditScheduled = true;
                sor.StartCoroutine(FinalizeCreditPenaltyAfterDelay(desiredFinal));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not schedule credit penalty: {ex.Message}");
            }
        }

        private static int GetCurrentCredits()
        {
            try
            {
                var term = UnityEngine.Object.FindObjectOfType<Terminal>();
                if (term != null) return term.groupCredits;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"Could not read current credits: {ex.Message}");
            }

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
                Plugin.Log.LogInfo($"Credits penalty applied: {before} -> {desiredFinal} (-{before - desiredFinal}).");
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
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"Could not sync credits to terminal: {ex.Message}");
            }
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
                int oldQuota = tod.profitQuota;
                int delta = Mathf.RoundToInt(Math.Max(1, oldQuota) * pct);
                int newQuota = Mathf.Max(1, oldQuota + delta);
                CachedQuotaPenaltyDelta = delta;
                tod.profitQuota = newQuota;

                NetworkSync.SyncQuotaToClients(newQuota, delta);

                Plugin.Log.LogInfo($"Quota penalty applied: {oldQuota} -> {newQuota} (+{delta}, {pct:P0}, {dead}/{total} dead).");
            }
        }

        private static void ApplyRolloverWipePenalty()
        {
            float pct = Mathf.Clamp01(ConfigManager.RolloverWipePenalty.Value);
            if (pct <= 0f) return;

            var tod = TimeOfDay.Instance;
            if (tod == null) return;

            int banked = tod.quotaFulfilled;
            if (banked <= 0) return;

            int cut = Mathf.RoundToInt(banked * pct);
            if (cut <= 0) return;

            int newFulfilled = Mathf.Max(0, banked - cut);
            tod.quotaFulfilled = newFulfilled;

            NetworkSync.SyncRolloverToClients(newFulfilled);

            Plugin.Log.LogInfo($"Rollover wipe penalty applied: {banked} -> {newFulfilled} (-{cut}, {pct:P0}).");
        }

        private static void CollectVehicleItems()
        {
            try
            {
                foreach (var vehicle in UnityEngine.Object.FindObjectsOfType<VehicleController>())
                {
                    try
                    {
                        if (vehicle == null) continue;

                        if (vehicle.magnetedToShip)
                        {
                            vehicle.CollectItemsInTruck();
                            continue;
                        }

                        var netObj = vehicle.NetworkObject;
                        if (netObj != null && netObj.IsSpawned)
                            netObj.Despawn(false);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogDebug($"Skipped vehicle cleanup: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not collect vehicle items: {ex.Message}");
            }
        }

        private static void MarkBeltBagContentsAsShipItems()
        {
            try
            {
                foreach (var bag in UnityEngine.Object.FindObjectsOfType<BeltBagItem>())
                {
                    try
                    {
                        if (bag == null) continue;

                        if (bag.insideAnotherBeltBag != null
                            && (bag.insideAnotherBeltBag.isInShipRoom || bag.insideAnotherBeltBag.isHeld))
                        {
                            bag.isInElevator = true;
                            bag.isInShipRoom = true;
                        }

                        if (!bag.isInShipRoom && !bag.isHeld) continue;

                        foreach (var stored in bag.objectsInBag)
                        {
                            if (stored == null) continue;

                            stored.isInElevator = true;
                            stored.isInShipRoom = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogDebug($"Skipped belt bag contents: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not protect belt bag contents: {ex.Message}");
            }
        }

        private static void MarkSurvivingItemsAsPersisted()
        {
            try
            {
                foreach (var g in UnityEngine.Object.FindObjectsOfType<GrabbableObject>())
                {
                    try
                    {
                        if (g != null && IsShipItem(g))
                            g.scrapPersistedThroughRounds = true;
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogDebug($"Skipped persist flag for item: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not flag surviving items: {ex.Message}");
            }
        }

        private static void DespawnFacilityItems()
        {
            try
            {
                var allGrab = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                if (allGrab == null || allGrab.Length == 0) return;

                foreach (var g in allGrab)
                {
                    try
                    {
                        if (!IsShipItem(g))
                        {
                            DespawnObject(g);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogDebug($"Skipped despawn check for facility item: {ex.Message}");
                    }
                }
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

                var storageSlots = SelfSortingStorageCompat.GetSlots();
                var storedLive = new HashSet<GrabbableObject>(storageSlots.Select(s => s.Live));

                var shipItems = allGrab.Where(g => IsShipItem(g) && !storedLive.Contains(g)).ToArray();
                var shipScrap = shipItems.Where(g => g.itemProperties.isScrap).ToArray();
                var shipEquip = shipItems.Where(g => !g.itemProperties.isScrap && !IsBodyOrBlacklisted(g)).ToArray();

                var storedScrap = storageSlots.Where(s => s.IsScrap).ToList();
                var storedEquip = storageSlots.Where(s => !s.IsScrap && !IsBodyOrBlacklisted(s.Live)).ToList();

                int shipScrapBeforeLoss = SumScrapValue(shipScrap) + SumStoredValues(storedScrap);

                bool scrapInsured = ScrapInsuranceCompat.IsScrapInsured();
                if (scrapInsured)
                    Plugin.Log.LogInfo("Scrap Insurance is active, collected scrap is protected from this crew wipe.");

                if (ConfigManager.ValueLossEnabled.Value && !scrapInsured)
                {
                    var valueTargets = shipScrap.Concat(storedScrap.Select(s => s.Live)).ToArray();
                    if (valueTargets.Length > 0)
                    {
                        ApplyValueLoss(valueTargets);

                        float keptFraction = 1f - Mathf.Clamp01(ConfigManager.ValueLossPercent.Value);
                        foreach (var slot in storedScrap)
                            SelfSortingStorageCompat.ScaleValues(slot, keptFraction);
                    }
                }

                if (ConfigManager.ScrapLossEnabled.Value && !scrapInsured)
                {
                    int budget = ResolveLossBudget(ConfigManager.MaxLostScrapItems.Value);
                    budget = SelectAndRemoveScrap(shipScrap, budget);
                    SelectAndRemoveStoredItems(
                        storedScrap,
                        budget,
                        Mathf.Clamp01(ConfigManager.ItemsSafeChance.Value),
                        Mathf.Clamp01(ConfigManager.LoseEachScrapChance.Value),
                        "scrap");
                }

                if (ConfigManager.EquipmentLossEnabled.Value)
                {
                    int budget = ResolveLossBudget(ConfigManager.MaxLostEquipmentItems.Value);
                    budget = SelectAndRemoveEquipment(shipEquip, budget);
                    SelectAndRemoveStoredItems(
                        storedEquip,
                        budget,
                        0f,
                        Mathf.Clamp01(ConfigManager.LoseEachEquipmentChance.Value),
                        "equipment");
                }

                int shipScrapAfterLoss = SumCurrentShipScrapValue(shipScrap) + SumStoredValues(storedScrap);
                CacheScrapLossSummary(shipScrapBeforeLoss, shipScrapAfterLoss);
                NetworkSync.SyncScrapLossSummaryToClients(shipScrapBeforeLoss, shipScrapAfterLoss);

                if (shipScrapBeforeLoss > 0)
                {
                    int lostValue = Mathf.Max(0, shipScrapBeforeLoss - shipScrapAfterLoss);
                    float lossPct = Mathf.Clamp01(lostValue / (float)shipScrapBeforeLoss);
                    Plugin.Log.LogInfo($"Scrap lost: {Mathf.RoundToInt(lossPct * 100f)}% (${lostValue}/${shipScrapBeforeLoss}).");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not apply ship loss rules: {ex.Message}");
            }
        }

        private static int SumScrapValue(IEnumerable<GrabbableObject> items)
        {
            int total = 0;

            foreach (var item in items)
            {
                try
                {
                    if (item?.itemProperties?.isScrap == true)
                        total += Mathf.Max(0, item.scrapValue);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug($"Skipped scrap value sum for item: {ex.Message}");
                }
            }

            return total;
        }

        private static int SumCurrentShipScrapValue(GrabbableObject[] shipScrap)
        {
            int total = 0;

            foreach (var item in shipScrap)
            {
                try
                {
                    if (item?.itemProperties?.isScrap != true) continue;
                    if (!IsShipItem(item)) continue;

                    total += Mathf.Max(0, item.scrapValue);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug($"Skipped scrap value sum for item: {ex.Message}");
                }
            }

            return total;
        }

        private static bool IsShipItem(GrabbableObject g)
        {
            if (g == null || g.itemProperties == null || !g.isInShipRoom) return false;

            try
            {
                var no = g.GetComponent<NetworkObject>();
                return no != null && no.IsSpawned;
            }
            catch { return false; }
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

        private static int ResolveLossBudget(int configured)
        {
            int value = Mathf.Max(0, configured);
            return value == 0 ? int.MaxValue : value;
        }

        private static int SumStoredValues(IEnumerable<StorageSlot> slots)
        {
            int total = 0;

            foreach (var slot in slots)
                total += SelfSortingStorageCompat.SumValues(slot);

            return total;
        }

        private static int SelectAndRemoveStoredItems(
            List<StorageSlot> slots,
            int budget,
            float safeChance,
            float loseChance,
            string label)
        {
            if (slots.Count == 0) return budget;

            int eligible = 0;
            int removedCount = 0;
            List<string> removedNames = new();

            foreach (var slot in slots)
            {
                try
                {
                    if (slot?.Live == null || slot.Count == 0) continue;

                    int lostFromSlot = 0;

                    for (int i = 0; i < slot.Count; i++)
                    {
                        eligible++;

                        if (removedCount + lostFromSlot >= budget) continue;

                        if (safeChance > 0f && UnityEngine.Random.value < safeChance) continue;

                        if (UnityEngine.Random.value < loseChance)
                            lostFromSlot++;
                    }

                    if (lostFromSlot == 0) continue;

                    string itemName = slot.Live.itemProperties != null
                        ? slot.Live.itemProperties.itemName
                        : slot.Live.name;

                    bool emptied = SelfSortingStorageCompat.RemoveItems(slot, lostFromSlot);

                    removedCount += lostFromSlot;
                    for (int i = 0; i < lostFromSlot; i++)
                        removedNames.Add(itemName);

                    if (emptied)
                        DespawnObject(slot.Live);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug($"Skipped stored {label} removal: {ex.Message}");
                }
            }

            if (eligible > 0)
                Plugin.Log.LogInfo($"Stored {label} removed: {removedCount}/{eligible} [{string.Join(", ", removedNames)}].");

            return Mathf.Max(0, budget - removedCount);
        }

        private static int SelectAndRemoveScrap(GrabbableObject[] scrapItems, int budget)
        {
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

                    if (removedCount >= budget) continue;

                    if (UnityEngine.Random.value < safeChance) continue;

                    if (UnityEngine.Random.value < loseChance)
                    {
                        DespawnObject(g);
                        removedCount++;
                        removedNames.Add(g.itemProperties.itemName);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug($"Skipped scrap removal for item: {ex.Message}");
                }
            }

            Plugin.Log.LogInfo($"Scrap items removed: {removedCount}/{eligible} [{string.Join(", ", removedNames)}].");
            return Mathf.Max(0, budget - removedCount);
        }

        private static int SelectAndRemoveEquipment(GrabbableObject[] equipItems, int budget)
        {
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

                    if (removedCount >= budget) continue;

                    if (UnityEngine.Random.value < loseChance)
                    {
                        DespawnObject(g);
                        removedCount++;
                        removedNames.Add(g.itemProperties.itemName);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug($"Skipped equipment removal for item: {ex.Message}");
                }
            }

            Plugin.Log.LogInfo($"Equipment items removed: {removedCount}/{eligible} [{string.Join(", ", removedNames)}].");
            return Mathf.Max(0, budget - removedCount);
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
                        catch (Exception ex)
                        {
                            Plugin.Log.LogDebug($"SetScrapValue failed: {ex.Message}");
                        }

                        var netObj = g.GetComponent<Unity.Netcode.NetworkObject>();
                        if (netObj != null)
                            syncData.Add(new SyncValueLossData(netObj.NetworkObjectId, newValue));

                        totalOldValue += oldValue;
                        totalNewValue += newValue;
                        affected++;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Could not reduce a scrap item's value: {ex.Message}");
                }
            }

            if (syncData.Count > 0)
                NetworkSync.SyncValueLossToClients(syncData.ToArray());

            Plugin.Log.LogInfo($"Scrap value reduced on {affected} items by {pct:P0} (${totalOldValue} -> ${totalNewValue}).");
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
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"Could not despawn object: {ex.Message}");
            }
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
            PenaltiesOnLandingPatch.HasPenaltyCache = false;
            PenaltiesOnLandingPatch.HasAllDeadSnapshot = false;
            PenaltiesOnLandingPatch.CachedQuotaPenaltyDelta = 0;
            PenaltiesOnLandingPatch.ClearScrapLossSummary();
        }
    }
}
