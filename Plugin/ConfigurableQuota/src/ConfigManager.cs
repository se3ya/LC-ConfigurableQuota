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
                "Base quota increase. Combined with 'Curve Sharpness' to calculate growth."
            );
            CurveSharpness = config.Bind(
                "0. Basic",
                "CurveSharpness",
                16f,
                "Quota growth curve. Higher = slower growth. Formula: increase ≈ BaseIncrease x (1 + quotaCount²/Sharpness)"
            );
            RandomizerMultiplier = config.Bind(
                "0. Basic",
                "RandomizerMultiplier",
                1f,
                "Adds variation to quota increases. 1 = ±50% variance (vanilla), 0 = no randomness, 2 = ±100% variance."
            );

            FinalLevel = config.Bind(
                "1. Leveling",
                "FinalLevel",
                -1,
                "When quota reaches this value, the Base Increase and Curve Sharpness are ignored. Set -1 to disable."
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
                "Gradually reduces quota growth after a number of fulfilled cycles, growth will slow down the longer you play."
            );
            DampeningStartAt = config.Bind(
                "1. Leveling",
                "DampeningStartAt",
                6,
                "Number of quota cycles before dampening begins."
            );
            DampeningSharpness = config.Bind(
                "1. Leveling",
                "DampeningSharpness",
                11f,
                "Controls dampening intensity. Lower values reduce growth more aggressively."
            );

            EnablePlayerMultiplier = config.Bind(
                "2. PlayerScaling",
                "EnablePlayerMultiplier",
                false,
                "Scale quota increases based on player count."
            );
            PlayerThreshold = config.Bind(
                "2. PlayerScaling",
                "PlayerThreshold",
                2,
                "Player count where scaling begins. Example: 2 means scaling starts at 3+ players."
            );
            PlayerCap = config.Bind(
                "2. PlayerScaling",
                "PlayerCap",
                4,
                "Maximum players counted for scaling. Example: 4 means player 5+ will not increase the quota multiplier."
            );
            MultPerPlayer = config.Bind(
                "2. PlayerScaling",
                "MultPerPlayer",
                0.25f,
                "Quota increase multiplier per extra player. Example: 0.5 = +50% increase per player above threshold."
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
                "Use team death ratio instead of per-player. Example: 2 dead out of 4 total = 50% penalty, not 30% (2x15%)."
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
                "Reduce penalty if you recover bodies. Example: 0.5 = 50% penalty forgiveness if bodies brought back."
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
                "Use team death ratio instead of per-player. Example: 2 dead out of 4 total = 50% quota increase."
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
                "Reduce penalty if you recover bodies. Example: 0.5 = 50% penalty forgiveness if bodies brought back."
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
                    "Chance for each scrap to be protected from loss. Example: 0.7 = 70% chance each scrap is safe.",
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
        }
    }
}
