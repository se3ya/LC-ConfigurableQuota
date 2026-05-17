using BepInEx.Configuration;
using ConfigurableQuota.Patches;

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

        public static ConfigEntry<bool> DynamicInteriorSizeEnabled = null!;
        public static ConfigEntry<float> DynamicInteriorSizeBase = null!;
        public static ConfigEntry<int> DynamicInteriorSizePlayerThreshold = null!;
        public static ConfigEntry<PlayerScalingDirection> DynamicInteriorSizeDirection = null!;
        public static ConfigEntry<float> DynamicInteriorSizeMultPerPlayer = null!;

        public static ConfigEntry<bool> DynamicScrapValueEnabled = null!;
        public static ConfigEntry<int> DynamicScrapValueOffset = null!;
        public static ConfigEntry<float> DynamicScrapValueMinMult = null!;
        public static ConfigEntry<float> DynamicScrapValueMaxMult = null!;
        public static ConfigEntry<int> DynamicScrapValuePlayerThreshold = null!;
        public static ConfigEntry<PlayerScalingDirection> DynamicScrapValueDirection = null!;
        public static ConfigEntry<float> DynamicScrapValueMultPerPlayer = null!;

        public static ConfigEntry<bool> DynamicScrapAmountEnabled = null!;
        public static ConfigEntry<int> DynamicScrapAmountValuePerItem = null!;
        public static ConfigEntry<float> DynamicScrapAmountMinFraction = null!;
        public static ConfigEntry<int> DynamicScrapAmountCap = null!;
        public static ConfigEntry<int> DynamicScrapAmountPlayerThreshold = null!;
        public static ConfigEntry<PlayerScalingDirection> DynamicScrapAmountDirection = null!;
        public static ConfigEntry<float> DynamicScrapAmountMultPerPlayer = null!;

        public static ConfigEntry<bool> DynamicEnemyPowerEnabled = null!;
        public static ConfigEntry<bool> DynamicEnemyPowerScaleInside = null!;
        public static ConfigEntry<bool> DynamicEnemyPowerScaleOutside = null!;
        public static ConfigEntry<bool> DynamicEnemyPowerScaleDaytime = null!;
        public static ConfigEntry<int> DynamicEnemyPowerPlayerThreshold = null!;
        public static ConfigEntry<float> DynamicEnemyPowerMultPerPlayer = null!;
        public static ConfigEntry<float> DynamicEnemyPowerMaxFactor = null!;

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
                "Minimum days for the deadline. REQUIRES 'Randomize Deadline' SET TO TRUE."
            );
            DeadlineMax = config.Bind(
                "0. Basic",
                "DeadlineMax",
                5,
                "Maximum days for the deadline. REQUIRES 'Randomize Deadline' SET TO TRUE."
            );
            DeadlineMustChange = config.Bind(
                "0. Basic",
                "DeadlineMustChange",
                true,
                "After fulfilling a quota, next random deadline has to be different from the previous one. REQUIRES 'Randomize Deadline' SET TO TRUE."
            );

            BaseIncrease = config.Bind(
                "0. Basic",
                "BaseIncrease",
                100,
                "Base amount the quota increase per each quota. Combined with CurveSharpness as: increase ≈ BaseIncrease * (1 + quota² / Sharpness). With defaults (100, 16) at quota 3, that's 100 * (1 + 9/16) ≈ 156."
            );
            CurveSharpness = config.Bind(
                "0. Basic",
                "CurveSharpness",
                16f,
                "Controls how fast the quota ramps up. Higher = slower growth. Formula: increase ≈ BaseIncrease * (1 + quota² / Sharpness). With BaseIncrease=100, Sharpness=16, quota 5 -> 100 * (1 + 25/16) ≈ 256."
            );
            RandomizerMultiplier = config.Bind(
                "0. Basic",
                "RandomizerMultiplier",
                1f,
                "Random variance on each quota increase. Multiplies by a factor in [1 - 0.5*M, 1 + 0.5*M]. 0 = no randomness, 1 = ±50% (vanilla), 2 = ±100%."
            );

            FinalLevel = config.Bind(
                "1. Leveling",
                "FinalLevel",
                -1,
                "Once the previous quota hits this value, growth stops using the curve and switches to a flat Final Increase per quota. With FinalLevel=10000 and FinalIncrease=200, every quota after $10000 just adds $200. Set -1 to disable."
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
                "Slows quota growth down after a while. After Dampening Start At fulfilled quotas, the curve increase gets divided by (1 + (excess / DampeningSharpness)²), where excess = currentQuota - DampeningStartAt."
            );
            DampeningStartAt = config.Bind(
                "1. Leveling",
                "DampeningStartAt",
                6,
                "How many quotas you have to clear before dampening starts. 6 means quotas 1 - 6 are untouched and quota 7 is the first one slowed down."
            );
            DampeningSharpness = config.Bind(
                "1. Leveling",
                "DampeningSharpness",
                11f,
                "Lower values dampen harder. At Start At=6, Sharpness=11, quota 10 -> divisor = 1 + (4/11)² ≈ 1.13, so growth shrinks by ~12%."
            );

            EnablePlayerMultiplier = config.Bind(
                "2. Player.Scaling",
                "EnablePlayerMultiplier",
                false,
                "Scale each quota increase by lobby size. Formula: (1 + extra * MultPerPlayer), where extra = clamp(playerCount - PlayerThreshold, 0, PlayerCap - PlayerThreshold)."
            );
            PlayerThreshold = config.Bind(
                "2. Player.Scaling",
                "PlayerThreshold",
                2,
                "Player count where the scaling starts. 2 means scaling starts at 3+ players."
            );
            PlayerCap = config.Bind(
                "2. Player.Scaling",
                "PlayerCap",
                4,
                "Player count where scaling stops. 4 means player 5+ don't bump the multiplier further."
            );
            MultPerPlayer = config.Bind(
                "2. Player.Scaling",
                "MultPerPlayer",
                0.25f,
                "Extra multiplier each player adds past the threshold. With 4 players, Threshold=2, Cap=4, MultPerPlayer=0.25, you get 1 + 2*0.25 = 1.5x on quota increases."
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
                "Credits lost per dead player. 0.15 = lose 15% of credits per death. Ignored when Dynamic is set to true."
            );
            CreditPenaltiesDynamic = config.Bind(
                "4. Penalties.Credits",
                "Dynamic",
                false,
                "Scale credit penalty by: (dead/total) * Percent Cap. With 2 dead out of 8 and Percent Cap=0.05 -> (2/8)*0.05 = 1.25%. When false, the penalty is Percent Per Player * dead, capped at Percent Cap."
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
                "Ignore penalties below this percentage. 0.1 = penalties under 10% are not applied."
            );
            CreditPenaltyRecoveryBonus = config.Bind(
                "4. Penalties.Credits",
                "RecoveryBonus",
                0f,
                "Reduce the penalty when body is recovered. Final = base * (1 - RecoveryBonus * recovered/dead). With 4 dead, 2 recovered, and RecoveryBonus=0.5 the penalty drops by 25% (base * 0.75)."
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
                "Quota increase per dead player. 0.1 = +10% to current quota per death. Ignored when Dynamic is true."
            );
            QuotaPenaltiesDynamic = config.Bind(
                "5. Penalties.Quota",
                "Dynamic",
                false,
                "Scale quota increase by: (dead/total) * Percent Cap. With 2 dead out of 8 and Percent Cap=0.5 -> (2/8)*0.5 = 12.5%. When false, the penalty is Percent Per Player * dead, capped at Percent Cap."
            );
            QuotaPenaltyPercentCap = config.Bind(
                "5. Penalties.Quota",
                "PercentCap",
                0.5f,
                "Maximum percentage the quota can increase. 0.5 = quota can increase by at most 50%."
            );
            QuotaPenaltyPercentThreshold = config.Bind(
                "5. Penalties.Quota",
                "PercentThreshold",
                0f,
                "Ignore penalties below this percentage. 0.15 = anything under 15% are not applied."
            );
            QuotaPenaltyRecoveryBonus = config.Bind(
                "5. Penalties.Quota",
                "RecoveryBonus",
                0f,
                "Reduce the quota penalty when bodies are recovered. Final = base * (1 - RecoveryBonus * recovered/dead). With 4 dead, 2 recovered, Recovery Bonus=0.5 penalty shrinks by 25% (base * 0.75)."
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
                    "Chance for each scrap to be protected. Combined with Lose Each Scrap Chance, actual loss chance is (1 - SafeChance) * LoseChance. So Safe=0.5, Lose=0.1 -> 5% per item.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            LoseEachScrapChance = config.Bind(
                "6. Loss.Scrap",
                "LoseEachScrapChance",
                0.1f,
                new ConfigDescription(
                    "Chance for unprotected scrap to get lost. 0.2 means 20% on the items that weren't saved by Items Safe Chance.",
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
                    "How much of the scrap value to reduce per crew wipe. 0.25 = each item loses 25%.",
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
                    "Chance for each equipment item to be lost. 0.1 = 10% per item.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            MaxLostEquipmentItems = config.Bind(
                "8. Loss.Equipment",
                "MaxLostEquipmentItems",
                1,
                "Maximum equipment items that can be lost per round. 1 = at most one item is lost."
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
                "Keep the Company's daily buy rate between Min Rate and Max Rate. Random Rate Enabled needs to be true."
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
                "Roll a random buy rate between Min Rate and Max Rate every day. Needs Min Max Enabled. With MinRate=0.4 and MaxRate=1.5 the daily rate lands somewhere between 40% and 150%."
            );
            LastDayRateEnabled = config.Bind(
                "X. Buy.Rate",
                "LastDayRateEnabled",
                false,
                "On the deadline's last day, force the rate through Last Day Min Rate and Last Day Max Rate. If min and max are equal you always get that rate. If they differ, roll Last Day Range Chance: a hit picks a random value in the range, a miss falls back to 100%."
            );
            LastDayRangeChance = config.Bind(
                "X. Buy.Rate",
                "LastDayRangeChance",
                0.3f,
                new ConfigDescription(
                    "Chance the last-day rate actually uses the range instead of falling back to 100%. 0.3 = 30%.",
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
                "Let the daily roll try for a jackpot rate. Checked before Last Day Rate or RandomRate. A hit picks a random value between JackpotMinRate and JackpotMaxRate (or that exact value if they match)."
            );
            JackpotLastDayOnly = config.Bind(
                "X. Buy.Rate",
                "JackpotLastDayOnly",
                true,
                "Only roll for a jackpot on the deadline's last day."
            );
            JackpotChance = config.Bind(
                "X. Buy.Rate",
                "JackpotChance",
                0.01f,
                new ConfigDescription(
                    "Chance the daily roll lands a jackpot. 0.01 = 1% per day. If Jackpot Last Day Only is true, this only fires on the last day.",
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
                    "How long to wait before the buy-rate alert pops up. 3s feels fine solo. Bump to 8+ if BetterEXP or DiscountAlerts step on it.",
                    new AcceptableValueRange<float>(0f, 30f)
                )
            );

            DynamicInteriorSizeEnabled = config.Bind(
                "A. Dynamic.Interior.Size",
                "Enabled",
                false,
                "Resize the moons interior based on lobby size."
            );
            DynamicInteriorSizeBase = config.Bind(
                "A. Dynamic.Interior.Size",
                "BaseSize",
                1.0f,
                new ConfigDescription(
                    "factorySizeMultiplier at the PlayerThreshold. 1.0 = vanilla size.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            DynamicInteriorSizePlayerThreshold = config.Bind(
                "A. Dynamic.Interior.Size",
                "PlayerThreshold",
                2,
                new ConfigDescription(
                    "Player count where scaling starts. With PerExtraPlayer it activates above this, with PerMissingPlayer it activates below.",
                    new AcceptableValueRange<int>(1, 32)
                )
            );
            DynamicInteriorSizeDirection = config.Bind(
                "A. Dynamic.Interior.Size",
                "ScalingDirection",
                PlayerScalingDirection.PerExtraPlayer,
                "PerExtraPlayer: more players = bigger dungeon. PerMissingPlayer: fewer players = bigger dungeon."
            );
            DynamicInteriorSizeMultPerPlayer = config.Bind(
                "A. Dynamic.Interior.Size",
                "MultPerPlayer",
                0.1f,
                new ConfigDescription(
                    "How much the size multiplier changes per qualifying player. threshold=2, PerExtraPlayer, this=0.1 -> 3 players get BaseSize * 1.1.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );

            DynamicScrapValueEnabled = config.Bind(
                "B. Dynamic.Scrap.Value",
                "Enabled",
                false,
                "Scale the moons min/max scrap value against the current quota."
            );
            DynamicScrapValueOffset = config.Bind(
                "B. Dynamic.Scrap.Value",
                "ScrapValueOffset",
                100,
                "Flat credits added to both min and max after the multiplier. Acts as a floor."
            );
            DynamicScrapValueMinMult = config.Bind(
                "B. Dynamic.Scrap.Value",
                "MinValueMultiplier",
                0.5f,
                new ConfigDescription(
                    "minTotalScrapValue = round(quota * this * factor) + ScrapValueOffset. With quota=300, this=0.5, factor=1.2, offset=100 you land at $280.",
                    new AcceptableValueRange<float>(0f, 100f)
                )
            );
            DynamicScrapValueMaxMult = config.Bind(
                "B. Dynamic.Scrap.Value",
                "MaxValueMultiplier",
                1.0f,
                new ConfigDescription(
                    "maxTotalScrapValue = round(quota * this * factor) + ScrapValueOffset. Keep it at or above MinValueMultiplier.",
                    new AcceptableValueRange<float>(0f, 100f)
                )
            );
            DynamicScrapValuePlayerThreshold = config.Bind(
                "B. Dynamic.Scrap.Value",
                "PlayerThreshold",
                2,
                new ConfigDescription(
                    "Player count where scaling starts. With PerExtraPlayer it activates above this, with PerMissingPlayer it activates below.",
                    new AcceptableValueRange<int>(1, 32)
                )
            );
            DynamicScrapValueDirection = config.Bind(
                "B. Dynamic.Scrap.Value",
                "ScalingDirection",
                PlayerScalingDirection.PerExtraPlayer,
                "PerMissingPlayer: fewer players = more scrap. PerExtraPlayer: more players = more scrap."
            );
            DynamicScrapValueMultPerPlayer = config.Bind(
                "B. Dynamic.Scrap.Value",
                "MultPerPlayer",
                0.15f,
                new ConfigDescription(
                    "Boost per qualifying player. threshold=2, PerMissingPlayer, this=0.15 -> solo gets factor=1.15.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );

            DynamicScrapAmountEnabled = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "Enabled",
                false,
                "Scale the moons min/max scrap item count off the current minTotalScrapValue."
            );
            DynamicScrapAmountValuePerItem = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "ValuePerScrapItem",
                25,
                new ConfigDescription(
                    "Divisor that turns scaled scrap value into item count. maxScrap = (minTotalScrapValue * factor) / this. Lower numbers mean more items per moon. At minTotalScrapValue=200, factor=1.15, this=25 you get maxScrap = floor(230/25) = 9.",
                    new AcceptableValueRange<int>(1, 1000)
                )
            );
            DynamicScrapAmountMinFraction = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "MinScrapFraction",
                0.6f,
                new ConfigDescription(
                    "minScrap = round(maxScrap * this). At maxScrap=9 and this=0.6 you get minScrap = round(5.4) = 5.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            DynamicScrapAmountCap = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "MaxScrapItemsCap",
                -1,
                "Hard upper bound on maxScrap no matter what the formula says. Set -1 to turn cap off."
            );
            DynamicScrapAmountPlayerThreshold = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "PlayerThreshold",
                2,
                new ConfigDescription(
                    "Player count where scaling starts. With PerExtraPlayer it activates above this, with PerMissingPlayer it activates below.",
                    new AcceptableValueRange<int>(1, 32)
                )
            );
            DynamicScrapAmountDirection = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "ScalingDirection",
                PlayerScalingDirection.PerExtraPlayer,
                "PerMissingPlayer: fewer players = more items. PerExtraPlayer: more players = more items."
            );
            DynamicScrapAmountMultPerPlayer = config.Bind(
                "C. Dynamic.Scrap.Amount",
                "MultPerPlayer",
                0.15f,
                new ConfigDescription(
                    "Boost per qualifying player. threshold=2, PerMissingPlayer, this=0.15 -> solo gets factor=1.15.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );

            DynamicEnemyPowerEnabled = config.Bind(
                "D. Dynamic.Enemy.Power",
                "Enabled",
                false,
                "Scale moons enemy power budget by player count. Higher budget = more / stronger enemies can spawn."
            );
            DynamicEnemyPowerScaleInside = config.Bind(
                "D. Dynamic.Enemy.Power",
                "ScaleInside",
                true,
                "Apply scaling to maxEnemyPowerCount (inside enemies)."
            );
            DynamicEnemyPowerScaleOutside = config.Bind(
                "D. Dynamic.Enemy.Power",
                "ScaleOutside",
                true,
                "Apply scaling to maxOutsideEnemyPowerCount (night time outside enemies)."
            );
            DynamicEnemyPowerScaleDaytime = config.Bind(
                "D. Dynamic.Enemy.Power",
                "ScaleDaytime",
                false,
                "Apply scaling to maxDaytimeEnemyPowerCount (daytime outside enemies)."
            );
            DynamicEnemyPowerPlayerThreshold = config.Bind(
                "D. Dynamic.Enemy.Power",
                "PlayerThreshold",
                2,
                new ConfigDescription(
                    "Player count where scaling starts. More players above this = more enemies.",
                    new AcceptableValueRange<int>(1, 32)
                )
            );
            DynamicEnemyPowerMultPerPlayer = config.Bind(
                "D. Dynamic.Enemy.Power",
                "MultPerPlayer",
                0.15f,
                new ConfigDescription(
                    "Boost per qualifying player. threshold=2, PerExtraPlayer, this=0.15: 4 players -> factor = 1 + 2*0.15 = 1.30.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );
            DynamicEnemyPowerMaxFactor = config.Bind(
                "D. Dynamic.Enemy.Power",
                "MaxFactor",
                3.0f,
                new ConfigDescription(
                    "Safety cap on the resolved factor. Floor is always 1.",
                    new AcceptableValueRange<float>(1f, 20f)
                )
            );
        }
    }
}