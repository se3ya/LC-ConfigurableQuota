using BepInEx.Configuration;

namespace ConfigurableQuota
{
    public static class ConfigManager
    {
        public static ConfigEntry<int> StartingCredits = null!;
        public static ConfigEntry<int> StartingQuota = null!;
        public static ConfigEntry<int> DaysToDeadline = null!;
        public static ConfigEntry<int> BaseIncrease = null!;
        public static ConfigEntry<float> CurveSharpness = null!;
        public static ConfigEntry<float> RandomizerMultiplier = null!;

        public static ConfigEntry<int> FinalLevel = null!;
        public static ConfigEntry<int> FinalIncrease = null!;
        public static ConfigEntry<int> QuotaCap = null!;

        public static ConfigEntry<bool> EnablePlayerMultiplier = null!;
        public static ConfigEntry<int> PlayerThreshold = null!;
        public static ConfigEntry<int> PlayerCap = null!;
        public static ConfigEntry<float> MultPerPlayer = null!;

        public static ConfigEntry<bool> DisableQuota = null!;
        public static ConfigEntry<float> RolloverAmount = null!;

        public static ConfigEntry<bool> CreditPenaltiesEnabled = null!;
        public static ConfigEntry<bool> CreditPenaltiesOnGordion = null!;
        public static ConfigEntry<float> CreditPenaltyPercentPerPlayer = null!;
        public static ConfigEntry<bool> CreditPenaltiesDynamic = null!;
        public static ConfigEntry<float> CreditPenaltyPercentCap = null!;
        public static ConfigEntry<float> CreditPenaltyPercentThreshold = null!;
        public static ConfigEntry<float> CreditPenaltyRecoveryBonus = null!;

        public static ConfigEntry<bool> QuotaPenaltiesEnabled = null!;
        public static ConfigEntry<bool> QuotaPenaltiesOnGordion = null!;
        public static ConfigEntry<float> QuotaPenaltyPercentPerPlayer = null!;
        public static ConfigEntry<bool> QuotaPenaltiesDynamic = null!;
        public static ConfigEntry<float> QuotaPenaltyPercentCap = null!;
        public static ConfigEntry<float> QuotaPenaltyPercentThreshold = null!;
        public static ConfigEntry<float> QuotaPenaltyRecoveryBonus = null!;

        public static ConfigEntry<bool> ScrapLossEnabled = null!;
        public static ConfigEntry<float> ItemsSafeChance = null!;
        public static ConfigEntry<float> LoseEachScrapChance = null!;
        public static ConfigEntry<int> MaxLostScrapItems = null!;

        public static ConfigEntry<bool> ValueLossEnabled = null!;
        public static ConfigEntry<float> ValueLossPercent = null!;

        public static ConfigEntry<bool> RandomizeDeadline = null!;
        public static ConfigEntry<int> DeadlineMin = null!;
        public static ConfigEntry<int> DeadlineMax = null!;
        public static ConfigEntry<bool> DeadlineMustChange = null!;

        public static ConfigEntry<bool> EnableGrowthDampening = null!;
        public static ConfigEntry<int> DampeningStartAt = null!;
        public static ConfigEntry<float> DampeningSharpness = null!;

        public static ConfigEntry<bool> EquipmentLossEnabled = null!;
        public static ConfigEntry<float> LoseEachEquipmentChance = null!;
        public static ConfigEntry<int> MaxLostEquipmentItems = null!;

        public static ConfigEntry<float> QuotaAnimationSpeed = null!;

        public static ConfigEntry<bool> MinMaxRateEnabled = null!;
        public static ConfigEntry<float> MinRate = null!;
        public static ConfigEntry<float> MaxRate = null!;
        public static ConfigEntry<bool> RandomRateEnabled = null!;
        public static ConfigEntry<bool> LastDayRateEnabled = null!;
        public static ConfigEntry<float> LastDayRangeChance = null!;
        public static ConfigEntry<float> LastDayMinRate = null!;
        public static ConfigEntry<float> LastDayMaxRate = null!;
        public static ConfigEntry<bool> JackpotEnabled = null!;
        public static ConfigEntry<bool> JackpotLastDayOnly = null!;
        public static ConfigEntry<float> JackpotChance = null!;
        public static ConfigEntry<float> JackpotMinRate = null!;
        public static ConfigEntry<float> JackpotMaxRate = null!;
        public static ConfigEntry<bool> BuyRateAlertEnabled = null!;
        public static ConfigEntry<bool> JackpotAlertEnabled = null!;
        public static ConfigEntry<float> AlertDelaySeconds = null!;

        internal static void Initialize(ConfigFile config)
        {
            StartingCredits = config.Bind(
                "0. Basic",
                "StartingCredits",
                60,
                "Starting credits for a new lobby."
            );

            StartingQuota = config.Bind(
                "0. Basic",
                "StartingQuota",
                130,
                "Starting quota for a new lobby."
            );

            DaysToDeadline = config.Bind(
                "0. Basic",
                "DaysToDeadline",
                3,
                "Number of days to meet each quota. Ignored if RandomizeDeadline is enabled."
            );
            RandomizeDeadline = config.Bind(
                "0. Basic",
                "RandomizeDeadline",
                false,
                "Randomize the deadline each quota using Deadline Min/Max instead of a fixed Days To Deadline."
            );
            DeadlineMin = config.Bind(
                "0. Basic",
                "DeadlineMin",
                3,
                "Minimum days for deadline. REQUIRES 'Randomize Deadline' SET TO TRUE"
            );
            DeadlineMax = config.Bind(
                "0. Basic",
                "DeadlineMax",
                5,
                "Maximum days for deadline. REQUIRES 'Randomize Deadline' SET TO TRUE"
            );
            DeadlineMustChange = config.Bind(
                "0. Basic",
                "DeadlineMustChange",
                true,
                "New deadline after fulfilling quota must differ from the previous one. REQUIRES 'Randomize Deadline' SET TO TRUE"
            );

            BaseIncrease = config.Bind(
                "0. Basic",
                "BaseIncrease",
                100,
                "Base quota increase per quota. Combined with CurveSharpness via: increase ≈ BaseIncrease * (1 + quota² / Sharpness). Example with defaults (100, 16) at quota 3: 100 * (1 + 9/16) ≈ 156."
            );
            CurveSharpness = config.Bind(
                "0. Basic",
                "CurveSharpness",
                16f,
                "Quota growth curve. Higher = slower growth. Formula: increase ≈ BaseIncrease * (1 + quota² / Sharpness). Example: BaseIncrease=100, Sharpness=16, quota 5 → 100 * (1 + 25/16) ≈ 256."
            );
            RandomizerMultiplier = config.Bind(
                "0. Basic",
                "RandomizerMultiplier",
                1f,
                "Adds random variance via factor in [1 - 0.5*M, 1 + 0.5*M]. 0 = no randomness, 1 = ±50% (vanilla), 2 = ±100%."
            );

            FinalLevel = config.Bind(
                "1. Leveling",
                "FinalLevel",
                -1,
                "When the previous quota meets or exceeds this value, growth switches from the curve to a flat FinalIncrease. Example: FinalLevel=10000, FinalIncrease=200 → next quota = previous + 200. Set -1 to disable."
            );
            FinalIncrease = config.Bind(
                "1. Leveling",
                "FinalIncrease",
                200,
                "Fixed increase amount used after reaching Final Level value."
            );
            QuotaCap = config.Bind(
                "1. Leveling",
                "QuotaCap",
                -1,
                "Maximum quota value, quota will never increase more than this amount. Set -1 for no limit."
            );
            EnableGrowthDampening = config.Bind(
                "1. Leveling",
                "EnableGrowthDampening",
                false,
                "After DampeningStartAt fulfilled quotas, divide the curve increase by (1 + (excess / DampeningSharpness)²) where excess = currentQuota - DampeningStartAt. Reduces growth the longer you play."
            );
            DampeningStartAt = config.Bind(
                "1. Leveling",
                "DampeningStartAt",
                6,
                "Number of fulfilled quotas before dampening starts. Example: 6 means quotas 1–6 are unaffected; dampening kicks in from quota 7 onward."
            );
            DampeningSharpness = config.Bind(
                "1. Leveling",
                "DampeningSharpness",
                11f,
                "Lower = stronger dampening. With DampeningStartAt=6, Sharpness=11, at quota 10: divisor = 1 + (4/11)² ≈ 1.13, so growth shrinks ~12%."
            );

            EnablePlayerMultiplier = config.Bind(
                "2. Player.Scaling",
                "EnablePlayerMultiplier",
                false,
                "Scale each quota increase by (1 + extra * MultPerPlayer) where extra = clamp(playerCount - PlayerThreshold, 0, PlayerCap - PlayerThreshold)."
            );
            PlayerThreshold = config.Bind(
                "2. Player.Scaling",
                "PlayerThreshold",
                2,
                "Player count where scaling begins. Example: 2 means scaling starts at 3+ players."
            );
            PlayerCap = config.Bind(
                "2. Player.Scaling",
                "PlayerCap",
                4,
                "Maximum players counted for scaling. Example: 4 means player 5+ will not increase the quota multiplier."
            );
            MultPerPlayer = config.Bind(
                "2. Player.Scaling",
                "MultPerPlayer",
                0.25f,
                "Extra multiplier per player above the threshold. Example: 4 players, Threshold=2, Cap=4, MultPerPlayer=0.25 → multiplier = 1 + 2*0.25 = 1.5x quota increase."
            );

            DisableQuota = config.Bind(
                "3. Optional",
                "DisableQuota",
                false,
                "Completely disables the quota system."
            );
            RolloverAmount = config.Bind(
                "3. Optional",
                "RolloverAmount",
                0.0f,
                new ConfigDescription(
                    "Percentage of extra scrap value that goes above the set limit and is added to the next quota. 0 = none (vanilla), 0.5 = 50%, 1.0 = 100%.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            CreditPenaltiesEnabled = config.Bind(
                "4. Penalties.Credits",
                "Enabled",
                false,
                "Reduce credits when crew members die."
            );
            CreditPenaltiesOnGordion = config.Bind(
                "4. Penalties.Credits",
                "OnGordion",
                false,
                "Apply credit penalties even when visiting The Company."
            );
            CreditPenaltyPercentPerPlayer = config.Bind(
                "4. Penalties.Credits",
                "PercentPerPlayer",
                0.15f,
                "Credits lost per dead player. Example: 0.15 = lose 15% of credits per death. Ignored if 'Dynamic' is true."
            );
            CreditPenaltiesDynamic = config.Bind(
                "4. Penalties.Credits",
                "Dynamic",
                false,
                "Scale credit penalty by (dead/total) * PercentCap. Example: 2 dead out of 8 total with PercentCap=0.05 -> (2/8)*0.05 = 1.25% penalty. When false, uses PercentPerPlayer * dead, capped at PercentCap."
            );
            CreditPenaltyPercentCap = config.Bind(
                "4. Penalties.Credits",
                "PercentCap",
                0.8f,
                "Maximum percentage of credits that can be lost."
            );
            CreditPenaltyPercentThreshold = config.Bind(
                "4. Penalties.Credits",
                "PercentThreshold",
                0f,
                "Ignore penalties below this percentage. Example: 0.1 = penalties under 10% are not applied."
            );
            CreditPenaltyRecoveryBonus = config.Bind(
                "4. Penalties.Credits",
                "RecoveryBonus",
                0f,
                "Multiply the penalty by (1 - RecoveryBonus * recovered/dead). Example: 4 dead, 2 recovered, RecoveryBonus=0.5 → final = base * (1 - 0.5*0.5) = base * 0.75 (25% reduction)."
            );

            QuotaPenaltiesEnabled = config.Bind(
                "5. Penalties.Quota",
                "Enabled",
                false,
                "Increase the current quota when crew members die."
            );
            QuotaPenaltiesOnGordion = config.Bind(
                "5. Penalties.Quota",
                "OnGordion",
                false,
                "Apply quota penalties even when visiting The Company."
            );
            QuotaPenaltyPercentPerPlayer = config.Bind(
                "5. Penalties.Quota",
                "PercentPerPlayer",
                0.1f,
                "Quota increase per dead player. Example: 0.1 = +10% to current quota per death. Ignored if 'Dynamic' is true."
            );
            QuotaPenaltiesDynamic = config.Bind(
                "5. Penalties.Quota",
                "Dynamic",
                false,
                "Scale quota increase by (dead/total) * PercentCap. Example: 2 dead out of 8 total with PercentCap=0.5 -> (2/8)*0.5 = 12.5% increase. When false, uses PercentPerPlayer * dead, capped at PercentCap."
            );
            QuotaPenaltyPercentCap = config.Bind(
                "5. Penalties.Quota",
                "PercentCap",
                0.5f,
                "Maximum percentage the quota can increase. Example: 0.5 = quota can increase by at most 50%."
            );
            QuotaPenaltyPercentThreshold = config.Bind(
                "5. Penalties.Quota",
                "PercentThreshold",
                0f,
                "Ignore penalties below this percentage. Example: 0.15 = increases under 15% are not applied."
            );
            QuotaPenaltyRecoveryBonus = config.Bind(
                "5. Penalties.Quota",
                "RecoveryBonus",
                0f,
                "Multiply the penalty by (1 - RecoveryBonus * recovered/dead). Example: 4 dead, 2 recovered, RecoveryBonus=0.5 → final = base * (1 - 0.5*0.5) = base * 0.75 (25% reduction)."
            );

            ScrapLossEnabled = config.Bind(
                "6. Loss.Scrap",
                "Enabled",
                false,
                "Randomly lose collected scrap when all crew dies."
            );
            ItemsSafeChance = config.Bind(
                "6. Loss.Scrap",
                "ItemsSafeChance",
                0.5f,
                new ConfigDescription(
                    "Chance for each scrap to be protected from loss. Combined with LoseEachScrapChance: actual loss chance per item = (1 - SafeChance) * LoseChance. Example: Safe=0.5, Lose=0.1 → 5% per item.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            LoseEachScrapChance = config.Bind(
                "6. Loss.Scrap",
                "LoseEachScrapChance",
                0.1f,
                new ConfigDescription(
                    "If scrap is not safe, this is the chance it gets lost. Example: 0.2 = 20% chance to lose unprotected scrap.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            MaxLostScrapItems = config.Bind(
                "6. Loss.Scrap",
                "MaxLostScrapItems",
                2,
                "Maximum scrap that can be lost per round."
            );

            ValueLossEnabled = config.Bind(
                "7. Loss.Value",
                "Enabled",
                false,
                "Reduce the scrap value of all ship items when the entire crew is wiped."
            );
            ValueLossPercent = config.Bind(
                "7. Loss.Value",
                "Percent",
                0.2f,
                new ConfigDescription(
                    "Percentage to reduce scrap value. Example: 0.25 = all scrap items lose 25% of their value, stacks on repeated wipes.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            EquipmentLossEnabled = config.Bind(
                "8. Loss.Equipment",
                "Enabled",
                false,
                "Randomly lose purchased equipment when all crew dies."
            );
            LoseEachEquipmentChance = config.Bind(
                "8. Loss.Equipment",
                "LoseEachEquipmentChance",
                0.05f,
                new ConfigDescription(
                    "Chance for each equipment item to be lost. Example: 0.1 = 10% chance per item.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            MaxLostEquipmentItems = config.Bind(
                "8. Loss.Equipment",
                "MaxLostEquipmentItems",
                1,
                "Maximum equipment items that can be lost per round. Example: 1 = lose at most 1 item."
            );

            QuotaAnimationSpeed = config.Bind(
                "9. UI",
                "QuotaAnimationSpeed",
                1f,
                new ConfigDescription(
                    "Speed multiplier for the new quota animation. Higher = faster.",
                    new AcceptableValueRange<float>(0.1f, 2f)
                )
            );

            MinMaxRateEnabled = config.Bind(
                "X. Buy.Rate",
                "MinMaxEnabled",
                false,
                "Clamp the Company's daily buy rate to [MinRate, MaxRate]. Required for RandomRateEnabled to take effect."
            );
            MinRate = config.Bind(
                "X. Buy.Rate",
                "MinRate",
                0.2f,
                new ConfigDescription(
                    "Minimum buy rate the Company will pay. 0.2 = 20%.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            MaxRate = config.Bind(
                "X. Buy.Rate",
                "MaxRate",
                1.2f,
                new ConfigDescription(
                    "Maximum buy rate the Company will pay. 1.2 = 120%.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            RandomRateEnabled = config.Bind(
                "X. Buy.Rate",
                "RandomRateEnabled",
                false,
                "Pick the daily buy rate uniformly in [MinRate, MaxRate] each day. Requires MinMaxEnabled. Example: MinRate=0.4, MaxRate=1.5 → daily rate is a random value between 40% and 150%."
            );
            LastDayRateEnabled = config.Bind(
                "X. Buy.Rate",
                "LastDayRateEnabled",
                false,
                "On the deadline's last day, override the rate via LastDayMinRate / LastDayMaxRate. Behavior: if min == max, always that value. If min < max, roll LastDayRangeChance — on hit pick a random value in [min, max], on miss fall back to 100%."
            );
            LastDayRangeChance = config.Bind(
                "X. Buy.Rate",
                "LastDayRangeChance",
                0.3f,
                new ConfigDescription(
                    "Chance to use the LastDayMin/Max range instead of the 100% fallback. Example: 0.3 = 30% chance for the random pick.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            LastDayMinRate = config.Bind(
                "X. Buy.Rate",
                "LastDayMinRate",
                1.0f,
                new ConfigDescription(
                    "Minimum buy rate on the last day. 1.0 = 100%.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            LastDayMaxRate = config.Bind(
                "X. Buy.Rate",
                "LastDayMaxRate",
                1.2f,
                new ConfigDescription(
                    "Maximum buy rate on the last day. 1.2 = 120%.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            JackpotEnabled = config.Bind(
                "X. Buy.Rate",
                "JackpotEnabled",
                false,
                "Allow a chance to roll a jackpot rate. Jackpot is checked first (before LastDayRate or RandomRate)."
            );
            JackpotLastDayOnly = config.Bind(
                "X. Buy.Rate",
                "JackpotLastDayOnly",
                true,
                "Jackpot rolls only happen at the deadline's last day."
            );
            JackpotChance = config.Bind(
                "X. Buy.Rate",
                "JackpotChance",
                0.01f,
                new ConfigDescription(
                    "Chance to roll a jackpot. Example: 0.01 = 1% chance per day (or only on last day if JackpotLastDayOnly).",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            JackpotMinRate = config.Bind(
                "X. Buy.Rate",
                "JackpotMinRate",
                1.5f,
                new ConfigDescription(
                    "Minimum jackpot rate. 1.5 = 150%.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            JackpotMaxRate = config.Bind(
                "X. Buy.Rate",
                "JackpotMaxRate",
                3.0f,
                new ConfigDescription(
                    "Maximum jackpot rate. 3.0 = 300%.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            BuyRateAlertEnabled = config.Bind(
                "X. Buy.Rate",
                "BuyRateAlertEnabled",
                false,
                "Show a yellow on-screen alert with the new buy rate each day. Local to this client."
            );
            JackpotAlertEnabled = config.Bind(
                "X. Buy.Rate",
                "JackpotAlertEnabled",
                false,
                "Show a red SCRAP EMERGENCY alert with sound when a jackpot is rolled. Local to this client."
            );
            AlertDelaySeconds = config.Bind(
                "X. Buy.Rate",
                "AlertDelaySeconds",
                3f,
                new ConfigDescription(
                    "Seconds to wait before showing the buy-rate alert. Recommended: 3s when alone, 8+s when running BetterEXP / DiscountAlerts to avoid overlap.",
                    new AcceptableValueRange<float>(0f, 30f)
                )
            );
        }
    }
}
