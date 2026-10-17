using System;
using System.Collections.Generic;
using System.Linq;
using ConfigurableQuota.Compat;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ConfigurableQuota.Patches;

internal static class PenaltyHelpers
{
    public static bool IsServer => NetworkManager.Singleton.IsServer;

    public static int GetPlayerCount()
    {
        return StartOfRound.Instance.connectedPlayersAmount + 1;
    }

    private static readonly HashSet<int> _recoveredAtTakeoff = new HashSet<int>();

    public static (int dead, int total, int recovered) CountDeathsAndRecovered()
    {
        int dead = 0;
        int total = 0;
        int recovered = 0;
        DeadBodyInfo[]? bodies = null;
        var players = StartOfRound.Instance.allPlayerScripts;

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player == null) continue;

            if (player.isPlayerControlled || player.isPlayerDead)
                total++;

            if (!player.isPlayerDead) continue;

            dead++;

            if (_recoveredAtTakeoff.Contains(i) || IsBodyInShip(FindBody(player, ref bodies)))
                recovered++;
        }

        return (dead, Math.Max(total, 1), recovered);
    }

    internal static void TakeBodySnapshot()
    {
        _recoveredAtTakeoff.Clear();
        DeadBodyInfo[]? bodies = null;
        var players = StartOfRound.Instance.allPlayerScripts;

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player != null && player.isPlayerDead && IsBodyInShip(FindBody(player, ref bodies)))
                _recoveredAtTakeoff.Add(i);
        }

        Plugin.Log.LogDebug($"Bodies in the ship at takeoff: {_recoveredAtTakeoff.Count}.");
    }

    internal static void ClearBodySnapshot()
    {
        _recoveredAtTakeoff.Clear();
    }

    private static DeadBodyInfo? FindBody(GameNetcodeStuff.PlayerControllerB player, ref DeadBodyInfo[]? bodies)
    {
        if (player.deadBody != null) return player.deadBody;

        bodies ??= UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
        foreach (var body in bodies)
        {
            if (body != null && body.playerScript == player)
                return body;
        }

        return null;
    }

    private static bool IsBodyInShip(DeadBodyInfo? body)
    {
        if (body == null || body.deactivated) return false;
        if (body.isInShip) return true;

        var grab = body.grabBodyObject;
        if (grab != null && grab.isInShipRoom) return true;

        var position = grab != null ? grab.transform.position : body.transform.position;
        return StartOfRound.Instance.shipBounds.bounds.Contains(position);
    }

    private static SelectableLevel? _classifiedLevel;
    private static string _classifiedLists = string.Empty;
    private static bool _classifiedIsCompany;

    public static bool IsAtCompany()
    {
        try
        {
            var level = StartOfRound.Instance.currentLevel;
            if (level == null) return false;

            string lists = ConfigManager.ForcedCompanyMoons.Value + "|" + ConfigManager.ExcludedCompanyMoons.Value;
            if (!ReferenceEquals(level, _classifiedLevel) || lists != _classifiedLists)
            {
                _classifiedLevel = level;
                _classifiedLists = lists;
                _classifiedIsCompany = ClassifyCompanyMoon(level);
            }

            return _classifiedIsCompany;
        }
        catch { return false; }
    }

    public static bool CreditPenaltiesApply(bool atCompany)
        => ConfigManager.CreditPenaltiesEnabled.Value
            && (!atCompany || ConfigManager.CreditPenaltiesOnCompanyMoons.Value);

    public static bool QuotaPenaltiesApply(bool atCompany)
        => ConfigManager.QuotaPenaltiesEnabled.Value
            && (!atCompany || ConfigManager.QuotaPenaltiesOnCompanyMoons.Value);

    private static bool ClassifyCompanyMoon(SelectableLevel level)
    {
        if (MatchesMoonList(level, ConfigManager.ExcludedCompanyMoons.Value)) return false;
        if (MatchesMoonList(level, ConfigManager.ForcedCompanyMoons.Value)) return true;
        if (level.sceneName == "CompanyBuilding") return true;

        bool company = !level.planetHasTime
            && !level.spawnEnemiesAndScrap
            && level.maxEnemyPowerCount <= 0
            && level.maxOutsideEnemyPowerCount <= 0;

        return company;
    }

    private static bool MatchesMoonList(SelectableLevel level, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string planetName = level.PlanetName ?? string.Empty;
        int space = planetName.IndexOf(' ');
        string shortName = space >= 0 ? planetName.Substring(space + 1) : planetName;

        foreach (var part in raw.Split(','))
        {
            string name = part.Trim();
            if (name.Length == 0) continue;

            if (string.Equals(name, level.sceneName, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(name, planetName, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(name, shortName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    public static float GetPenaltyPercent(bool dynamicMode, float percentPerPlayer, float cap, float threshold, float recoveryBonus,
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

    internal static SyncPenaltySummary Summary;
    internal static bool HasPenaltyCache;
    internal static int CachedQuotaPenaltyDelta;

    internal static int CachedShipScrapBeforeLoss;
    internal static int CachedShipScrapAfterLoss;
    internal static bool HasScrapLossSummary;
    internal static bool HasAllDeadSnapshot;

    internal static void ResetCreditSchedule()
    {
        _creditScheduled = false;
    }

    private static void ApplyPenalties(int dead, int total, int recovered, bool atCompany)
    {
        bool creditApplied = PenaltyHelpers.CreditPenaltiesApply(atCompany);
        Summary = new SyncPenaltySummary(dead, total, recovered, creditApplied, 0f, 0, 0f, 0);
        HasPenaltyCache = true;

        if (creditApplied)
        {
            ScheduleCreditPenalty(dead, total, recovered);
        }

        if (PenaltyHelpers.QuotaPenaltiesApply(atCompany))
        {
            ApplyQuotaPenalty(dead, total, recovered);
        }

        NetworkSync.SyncPenaltySummaryToClients(Summary);
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
        bool wipeHandled = false;

        try
        {
            if (!PenaltyHelpers.IsServer) return true;

            HasPenaltyCache = false;
            HasAllDeadSnapshot = false;
            CachedQuotaPenaltyDelta = 0;
            ClearScrapLossSummary();
            SelfSortingStorageCompat.Processed = false;
            HQoLCompat.Processed = false;

            bool atCompany = PenaltyHelpers.IsAtCompany();
            var (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();

            bool lossesAllowedHere = !atCompany || ConfigManager.ApplyLossesOnCompanyMoons.Value;

            if (!despawnAllItems && lossesAllowedHere && dead > 0 && dead >= total && !_lossesAppliedThisRound)
            {
                wipeHandled = true;
                _appliedThisRound = true;
                _lossesAppliedThisRound = true;

                MarkItemsInShipRoom();
                CollectVehicleItems();
                MarkBeltBagContentsAsShipItems();

                DespawnFacilityItems();

                ApplyLossesWhenAllDead();
                MarkSurvivingItemsAsPersisted();
                HasAllDeadSnapshot = true;

                HudQuotaAnimationPatch.TryApplyAdvancedFeaturesEndscreen();

                ApplyPenalties(dead, total, recovered, atCompany);

                if (ConfigManager.RolloverWipePenalty.Value > 0f)
                {
                    ApplyRolloverWipePenalty();
                }

                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning(wipeHandled
                ? $"Error in despawn prefix: {e.Message}. The crew wipe was already handled, so the game is not allowed to despawn on top of it."
                : $"Error in despawn prefix: {e.Message}");

            return !wipeHandled;
        }
    }

    [HarmonyPatch("DespawnPropsAtEndOfRound")]
    [HarmonyPostfix]
    private static void DespawnPostfix(bool despawnAllItems)
    {
        try
        {
            if (despawnAllItems || !PenaltyHelpers.IsServer) return;

            if (_appliedThisRound) return;

            var (dead, total, recovered) = PenaltyHelpers.CountDeathsAndRecovered();
            if (dead <= 0) return;

            ApplyPenalties(dead, total, recovered, PenaltyHelpers.IsAtCompany());

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

            int currentCredits = GetCurrentCredits();
            float pct = PenaltyHelpers.GetPenaltyPercent(
                ConfigManager.CreditPenaltiesDynamic.Value,
                ConfigManager.CreditPenaltyPercentPerPlayer.Value,
                ConfigManager.CreditPenaltyPercentCap.Value,
                ConfigManager.CreditPenaltyPercentThreshold.Value,
                ConfigManager.CreditPenaltyRecoveryBonus.Value,
                dead, total, recovered);

            pct = MoreShipUpgradesCompat.ApplyLifeInsurance(pct);

            if (pct <= 0f) return;

            int desiredFinal = Mathf.Max(0, currentCredits - Mathf.RoundToInt(currentCredits * pct));
            Summary.CreditPercent = pct;
            Summary.CreditLoss = currentCredits - desiredFinal;
            _creditScheduled = true;
            StartOfRound.Instance.StartCoroutine(FinalizeCreditPenaltyAfterDelay(desiredFinal));
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
            int before = GetCurrentCredits();
            SetCredits(desiredFinal);
            Plugin.Log.LogInfo($"Took {before - desiredFinal} credits for deaths, {desiredFinal} left.");
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
        float pct = PenaltyHelpers.GetPenaltyPercent(
            ConfigManager.QuotaPenaltiesDynamic.Value,
            ConfigManager.QuotaPenaltyPercentPerPlayer.Value,
            ConfigManager.QuotaPenaltyPercentCap.Value,
            ConfigManager.QuotaPenaltyPercentThreshold.Value,
            ConfigManager.QuotaPenaltyRecoveryBonus.Value,
            dead, total, recovered);

        if (pct <= 0f) return;

        var tod = TimeOfDay.Instance;
        int oldQuota = tod.profitQuota;
        int delta = Mathf.RoundToInt(Math.Max(1, oldQuota) * pct);
        int newQuota = Mathf.Max(1, oldQuota + delta);
        CachedQuotaPenaltyDelta = delta;
        Summary.QuotaPercent = pct;
        Summary.QuotaDelta = delta;
        tod.profitQuota = newQuota;

        NetworkSync.SyncQuotaToClients(newQuota, delta);

        Plugin.Log.LogInfo($"Raised the quota by {pct:P0} from {oldQuota} to {newQuota}, {dead} of {total} dead and {recovered} recovered.");
    }

    private static void ApplyRolloverWipePenalty()
    {
        float pct = Mathf.Clamp01(ConfigManager.RolloverWipePenalty.Value);
        if (pct <= 0f) return;

        var tod = TimeOfDay.Instance;
        int banked = tod.quotaFulfilled;
        if (banked <= 0) return;

        int cut = Mathf.RoundToInt(banked * pct);
        if (cut <= 0) return;

        int newFulfilled = Mathf.Max(0, banked - cut);
        tod.quotaFulfilled = newFulfilled;
        TimeOfDayQuotaPatch.LastAppliedRollover = Math.Min(TimeOfDayQuotaPatch.LastAppliedRollover, newFulfilled);

        NetworkSync.SyncRolloverToClients(newFulfilled);

        Plugin.Log.LogInfo($"Cut the rollover by {pct:P0} for the crew wipe, {banked} down to {newFulfilled}.");
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

            var storageSlots = SelfSortingStorageCompat.GetSlots();
            var hqolItems = HQoLCompat.GetItems();
            var storedLive = new HashSet<GrabbableObject>(storageSlots.Select(s => s.Live));

            var shipItems = allGrab.Where(g => IsShipItem(g) && !storedLive.Contains(g)).ToArray();
            var shipScrap = shipItems.Where(g => g.itemProperties.isScrap).ToArray();
            var shipEquip = shipItems.Where(g => !g.itemProperties.isScrap && !IsBodyOrBlacklisted(g)).ToArray();

            var storedScrap = storageSlots.Where(s => s.IsScrap).ToList();
            var storedEquip = storageSlots
                .Where(s => s.Consistent && !s.IsScrap && !IsBodyOrBlacklisted(s.Live))
                .Select(s => (Slot: s, Indices: GetLosableIndices(s, false)))
                .ToList();

            int shipScrapBeforeLoss = SumScrapValue(shipScrap) + SumStoredValues(storedScrap) + hqolItems.Sum(i => Mathf.Max(0, i.Value));

            bool scrapInsured = ScrapInsuranceCompat.IsScrapInsured();
            if (scrapInsured)
                Plugin.Log.LogInfo("Scrap Insurance is active, collected scrap is protected from this crew wipe.");

            bool dayProtection = ConfigManager.OnlyLoseCurrentDayLoot.Value;
            int players = PenaltyHelpers.GetPlayerCount();
            int maxPlayers = ConfigManager.OnlyCurrentDayMaxPlayers.Value;
            bool onlyCurrentDay = dayProtection && (maxPlayers <= 0 || players <= maxPlayers);

            bool scrapCanBeLost = (ConfigManager.ScrapLossEnabled.Value || ConfigManager.ValueLossEnabled.Value) && !scrapInsured;

            if (scrapCanBeLost && dayProtection && !onlyCurrentDay)
                Plugin.Log.LogInfo($"Lobby of {players} is over the limit of {maxPlayers}, older loot can be lost this wipe.");

            var losableScrap = onlyCurrentDay
                ? shipScrap.Where(g => g != null && !g.scrapPersistedThroughRounds).ToArray()
                : shipScrap;
            var losableStored = storedScrap
                .Where(s => s.Consistent)
                .Select(s => (Slot: s, Indices: GetLosableIndices(s, onlyCurrentDay)))
                .Where(p => p.Indices.Count > 0)
                .ToList();
            var losableHQoL = onlyCurrentDay ? new List<HQoLItem>() : hqolItems;

            if (scrapCanBeLost && onlyCurrentDay)
            {
                int storedTotal = storedScrap.Sum(s => s.Count) + hqolItems.Count;
                int storedAtRisk = losableStored.Sum(p => p.Indices.Count);
                Plugin.Log.LogInfo($"Only today's loot can be lost, {losableScrap.Length} of {shipScrap.Length} loose scrap and {storedAtRisk} of {storedTotal} stored items are at risk.");
            }

            if (ConfigManager.ValueLossEnabled.Value && !scrapInsured)
            {
                float keptFraction = 1f - Mathf.Clamp01(ConfigManager.ValueLossPercent.Value);

                var valueTargets = losableScrap
                    .Concat(losableStored.Where(p => p.Indices.Contains(0)).Select(p => p.Slot.Live))
                    .ToArray();
                if (valueTargets.Length > 0)
                    ApplyValueLoss(valueTargets);

                foreach (var (slot, indices) in losableStored)
                    SelfSortingStorageCompat.ScaleValues(slot, keptFraction, indices);

                HQoLCompat.ScaleValues(losableHQoL, keptFraction);
            }

            if (ConfigManager.ScrapLossEnabled.Value && !scrapInsured)
            {
                float safeChance = Mathf.Clamp01(ConfigManager.ItemsSafeChance.Value);
                float loseChance = Mathf.Clamp01(ConfigManager.LoseEachScrapChance.Value);

                int budget = GetLossBudget(ConfigManager.MaxLostScrapItems.Value);
                budget = SelectAndRemoveScrap(losableScrap, budget);
                budget = SelectAndRemoveStoredItems(losableStored, budget, safeChance, loseChance, "scrap");
                SelectAndRemoveHQoLItems(losableHQoL, budget, safeChance, loseChance);
            }

            if (ConfigManager.EquipmentLossEnabled.Value)
            {
                int budget = GetLossBudget(ConfigManager.MaxLostEquipmentItems.Value);
                budget = SelectAndRemoveEquipment(shipEquip, budget);
                SelectAndRemoveStoredItems(
                    storedEquip,
                    budget,
                    0f,
                    Mathf.Clamp01(ConfigManager.LoseEachEquipmentChance.Value),
                    "equipment");
            }

            int shipScrapAfterLoss = SumCurrentShipScrapValue(shipScrap) + SumStoredValues(storedScrap) + (HQoLCompat.Processed ? HQoLCompat.SumValues() : 0);
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

    private static void MarkItemsInShipRoom()
    {
        var bounds = StartOfRound.Instance.shipInnerRoomBounds.bounds;
        foreach (var item in UnityEngine.Object.FindObjectsOfType<GrabbableObject>())
        {
            if (bounds.Contains(item.transform.position))
                item.isInShipRoom = true;
        }
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

    private static int GetLossBudget(int configured)
    {
        int value = Mathf.Max(0, configured);
        return value == 0 ? int.MaxValue : value;
    }

    private static List<int> GetLosableIndices(StorageSlot slot, bool onlyCurrentDay)
    {
        var indices = new List<int>();

        for (int i = 0; i < slot.Count; i++)
        {
            if (!onlyCurrentDay || SelfSortingStorageCompat.IsNew(slot, i))
                indices.Add(i);
        }

        return indices;
    }

    private static int SumStoredValues(IEnumerable<StorageSlot> slots)
    {
        int total = 0;

        foreach (var slot in slots)
            total += SelfSortingStorageCompat.SumValues(slot);

        return total;
    }

    private static int SelectAndRemoveStoredItems(
        List<(StorageSlot Slot, List<int> Indices)> entries,
        int budget,
        float safeChance,
        float loseChance,
        string label)
    {
        if (entries.Count == 0) return budget;

        int eligible = 0;
        int removedCount = 0;
        List<string> removedNames = new();

        foreach (var (slot, indices) in entries)
        {
            try
            {
                if (slot?.Live == null || indices.Count == 0) continue;

                var chosen = new List<int>();

                foreach (int index in indices)
                {
                    eligible++;

                    if (removedCount + chosen.Count >= budget) continue;

                    if (safeChance > 0f && UnityEngine.Random.value < safeChance) continue;

                    if (UnityEngine.Random.value < loseChance)
                        chosen.Add(index);
                }

                if (chosen.Count == 0) continue;

                string itemName = slot.Live.itemProperties != null
                    ? slot.Live.itemProperties.itemName
                    : slot.Live.name;

                int removed = SelfSortingStorageCompat.RemoveItems(slot, chosen);

                removedCount += removed;
                for (int i = 0; i < removed; i++)
                    removedNames.Add(itemName);

                if (removed > 0 && slot.Count == 0)
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

    private static void SelectAndRemoveHQoLItems(List<HQoLItem> items, int budget, float safeChance, float loseChance)
    {
        if (items.Count == 0) return;

        var chosen = new List<HQoLItem>();

        foreach (var item in items)
        {
            if (chosen.Count >= budget) break;

            if (safeChance > 0f && UnityEngine.Random.value < safeChance) continue;

            if (UnityEngine.Random.value < loseChance)
                chosen.Add(item);
        }

        int removed = HQoLCompat.RemoveItems(chosen);
        string names = string.Join(", ", chosen.OrderByDescending(i => i.Index).Take(removed).Select(i => i.Name));
        Plugin.Log.LogInfo($"Stored HQoL scrap removed: {removed}/{items.Count} [{names}].");
    }

    private static int SelectAndRemoveScrap(GrabbableObject[] scrapItems, int budget)
    {
        float safeChance = Mathf.Clamp01(ConfigManager.ItemsSafeChance.Value);
        float loseChance = Mathf.Clamp01(ConfigManager.LoseEachScrapChance.Value);

        int eligible = 0;
        int removedCount = 0;
        int keptByUpgrade = 0;
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
                    if (MoreShipUpgradesCompat.CanKeepScrap(g))
                    {
                        keptByUpgrade++;
                        continue;
                    }

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

        if (eligible > 0)
            Plugin.Log.LogInfo($"Scrap items removed: {removedCount}/{eligible} [{string.Join(", ", removedNames)}].");

        if (keptByUpgrade > 0)
            Plugin.Log.LogInfo($"Scrap Keeper saved {keptByUpgrade} scrap from this crew wipe.");

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

        if (eligible > 0)
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

        Plugin.Log.LogInfo($"Reduced the value of {affected} items by {pct:P0}, ${totalOldValue} to ${totalNewValue}.");
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
internal static class EndOfGamePatch
{
    [HarmonyPatch(nameof(StartOfRound.EndOfGameClientRpc))]
    [HarmonyPrefix]
    private static void EndOfGameClientRpc_Prefix()
    {
        PenaltiesOnLandingPatch.HasPenaltyCache = false;

        try
        {
            if (PenaltyHelpers.IsServer)
                PenaltyHelpers.TakeBodySnapshot();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Could not count the bodies in the ship: {e.Message}");
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
        PenaltiesOnLandingPatch.ResetCreditSchedule();
        PenaltiesOnLandingPatch.HasPenaltyCache = false;
        PenaltiesOnLandingPatch.HasAllDeadSnapshot = false;
        PenaltyHelpers.ClearBodySnapshot();
        PenaltiesOnLandingPatch.CachedQuotaPenaltyDelta = 0;
        PenaltiesOnLandingPatch.ClearScrapLossSummary();
        SelfSortingStorageCompat.Processed = false;
        HQoLCompat.Processed = false;
    }
}
