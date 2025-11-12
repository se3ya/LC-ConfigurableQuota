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
                "Starting credits for a new game."
            );

            DaysToDeadline = config.Bind(
                "0. Basic",
                "DaysToDeadline",
                3,
                "Number of days to meet each quota."
            );

            StartingQuota = config.Bind(
                "0. Basic",
                "StartingQuota",
                130,
                "First quota value at the start of a new game."
            );

            BaseIncrease = config.Bind(
                "0. Basic",
                "BaseIncrease",
                100,
                "Base quota increase per cycle. Combined with CurveSharpness to calculate growth."
            );
            CurveSharpness = config.Bind(
                "0. Basic",
                "CurveSharpness",
                16f,
                "Controls quota growth curve. Higher = slower growth. Formula: increase ≈ BaseIncrease × (1 + quotaCount²/Sharpness).."
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
                "Quota value where growth switches from curved to flat. Set -1 to disable and use curve forever."
            );
            FinalIncrease = config.Bind(
                "1. Leveling",
                "FinalIncrease",
                200,
                "Fixed increase amount used after reaching FinalLevel."
            );
            QuotaCap = config.Bind(
                "1. Leveling",
                "QuotaCap",
                -1,
                "Maximum quota value. Quota will never exceed this amount. Set -1 for no limit."
            );

            EnablePlayerMultiplier = config.Bind(
                "2. PlayerScaling",
                "EnablePlayerMultiplier",
                false,
                "Scale quota increases based on player count. Not in vanilla. Useful for balancing larger crews."
            );
            PlayerThreshold = config.Bind(
                "2. PlayerScaling",
                "PlayerThreshold",
                4,
                "Player count where scaling begins. Example: 4 means scaling starts at 5+ players."
            );
            PlayerCap = config.Bind(
                "2. PlayerScaling",
                "PlayerCap",
                8,
                "Maximum players counted for scaling. Example: 8 means player 9+ don't increase difficulty further."
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
                "Completely disables the quota system. Enables endless exploration mode."
            );
            RolloverAmount = config.Bind(
                "3. Optional",
                "RolloverAmount",
                0.0f,
                new ConfigDescription(
                    "Percentage of excess scrap value (above quota) that carries over to the next cycle. 0 = none (vanilla), 0.5 = 50%, 1.0 = 100%.",
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
                "Apply credit penalties even when visiting the Company moon (Gordion)."
            );
            CreditPenaltyPercentPerPlayer = config.Bind(
                "4. Penalties.Credits",
                "PercentPerPlayer",
                0.15f,
                "Credits lost per dead player. Example: 0.15 = lose 15% of credits per death. Ignored if Dynamic is true."
            );
            CreditPenaltiesDynamic = config.Bind(
                "4. Penalties.Credits",
                "Dynamic",
                false,
                "Use team death ratio instead of per-player. Example: 2 dead out of 4 total = 50% penalty, not 30% (2×15%)."
            );
            CreditPenaltyPercentCap = config.Bind(
                "4. Penalties.Credits",
                "PercentCap",
                0.8f,
                "Maximum percentage of credits that can be lost. Example: 0.8 = lose at most 80% of your credits."
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
                "Apply quota penalties even when visiting the Company moon (Gordion)."
            );
            QuotaPenaltyPercentPerPlayer = config.Bind(
                "5. Penalties.Quota",
                "PercentPerPlayer",
                0.1f,
                "Quota increase per dead player. Example: 0.1 = +10% to current quota per death. Ignored if Dynamic is true."
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
                "Randomly lose collected scrap items when all players die."
            );
            ItemsSafeChance = config.Bind(
                "6. Loss.Scrap",
                "ItemsSafeChance",
                0.5f,
                new ConfigDescription(
                    "Chance for each item to be protected from loss. Example: 0.7 = 70% chance each item is safe.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            LoseEachScrapChance = config.Bind(
                "6. Loss.Scrap",
                "LoseEachScrapChance",
                0.1f,
                new ConfigDescription(
                    "If an item is not safe, this is the chance it gets lost. Example: 0.2 = 20% chance to lose unprotected items.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            MaxLostScrapItems = config.Bind(
                "6. Loss.Scrap",
                "MaxLostScrapItems",
                2,
                "Maximum scrap items that can be lost per round."
            );

            ValueLossEnabled = config.Bind(
                "7. Loss.Value",
                "Enabled",
                false,
                "Reduce the sell value of all scrap when all players die. Items remain but are worth less."
            );
            ValueLossPercent = config.Bind(
                "7. Loss.Value",
                "Percent",
                0.2f,
                new ConfigDescription(
                    "Percentage to reduce scrap value. Example: 0.25 = all scrap worth 25% less when sold.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );

            EquipmentLossEnabled = config.Bind(
                "8. Loss.Equipment",
                "Enabled",
                false,
                "Randomly lose purchased equipment (shovels, flashlights, etc.) when all players die."
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
