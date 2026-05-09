<div style="display: flex; gap: 10px; flex-wrap: wrap; margin-bottom: 16px;">
  <img src="https://img.shields.io/codefactor/grade/github/se3ya/LC-ConfigurableQuota?style=flat&logo=codefactor&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="CodeFactor Grade">
  <img src="https://img.shields.io/thunderstore/dt/seechela/Configurable_Quota?style=flat&logo=thunderstore&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="Thunderstore Downloads">
  <img src="https://img.shields.io/github/v/release/se3ya/LC-ConfigurableQuota?style=flat&logo=github&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="GitHub Release Version">
  </div>
<br><br>

---
# Configurable Quota
### **Allows users to configure every aspect of the quota the way they want.**
---

## Features

- Configure starting credits, starting quota and deadline
- Configure curved quota growth with sharpness and randomizer
- Configure growth dampening which slows down quota increases the longer you play
- Configure player count scaling with threshold, cap and per player multiplier
- Configure rollover which transfers any extra fulfilment to the next quota
- Configure randomized deadline
- Configure credit penalties when crew members die
- Configure quota penalties when crew members die
- Configure randomly lost scrap items on a full crew wipe
- Configure reduced scrap value on full crew wipe
- Configure randomly lost purchased equipment on a full crew wipe
- Disable quota entirely
- Configure new quota animation speed
- Show actual penalty values on fine UI
- Configure the Company's buy rate with min/max clamp, random rate, last-day override and jackpot rolls
- __LethalConstellations__ compatibility with per-constellation deadline modes _[ fixed, random or use global ]_

---

## Configuration Options

### **0. Basic**

- **Starting Credits** - Credits you start with on a new game
- **Starting Quota** - First quota value on a new game
- **Days To Deadline** - Days per quota. Ignored when `RandomizeDeadline` is on
- **Randomize Deadline** - Pick a random deadline length each quota using the min/max below. Also used by constellations in `Use Global` mode
- **Deadline Min** - Minimum days when random deadline is enabled
- **Deadline Max** - Maximum days when random deadline is enabled
- **Deadline Must Change** - Forces the next random deadline to differ from the previous one
- **Base Increase** - Base amount the quota goes up each quota
- **Curve Sharpness** - Controls how fast the quota scales. Higher = slower growth
- **Randomizer Multiplier** - Adds variation to quota increases. 0 = no randomness, 1 = vanilla variance

**Quota growth formula**

```
increase ≈ BaseIncrease * (1 + quota² / CurveSharpness) * randomFactor
randomFactor ∈ [1 - 0.5*RandomizerMultiplier, 1 + 0.5*RandomizerMultiplier]
```

**Example** with defaults (`BaseIncrease=100`, `CurveSharpness=16`, `RandomizerMultiplier=1`):

| Quota | Curve term            | Avg increase | Cumulative quota (start 130) |
|------:|----------------------:|-------------:|-----------------------------:|
| 1     | 100 × (1 + 1/16) ≈ 106  | ~106 ± 53    | ~236                         |
| 2     | 100 × (1 + 4/16) ≈ 125  | ~125 ± 63    | ~361                         |
| 3     | 100 × (1 + 9/16) ≈ 156  | ~156 ± 78    | ~517                         |
| 4     | 100 × (1 + 16/16) ≈ 200 | ~200 ± 100   | ~717                         |
| 5     | 100 × (1 + 25/16) ≈ 256 | ~256 ± 128   | ~973                         |

### **1. Leveling**

- **Final Level** - Quota value where curved growth switches to flat. Set -1 to disable
- **Final Increase** - Flat increase used after hitting `FinalLevel`
- **Quota Cap** - Maximum quota value. Set -1 for no limit
- **Enable Growth Dampening** - Gradually reduces quota growth the longer you play
- **Dampening Start At** - How many fulfilled quotas before dampening kicks in
- **Dampening Sharpness** - How aggressively growth is reduced. Lower = stronger dampening

**Dampening formula** (only active after `DampeningStartAt` quota):

```
excess = currentQuota - DampeningStartAt
divisor = 1 + (excess / DampeningSharpness)²
finalIncrease = curveIncrease / divisor
```

**Example**: `DampeningStartAt=6`, `DampeningSharpness=11`, at quota 10:

```
excess = 10 - 6 = 4
divisor = 1 + (4/11)² ≈ 1.13
=> growth shrinks by ~12%
```

### **2. Player.Scaling**

- **Enable Player Multiplier** - Scale quota increases based on how many players are connected
- **Player Threshold** - Player count where scaling starts
- **Player Cap** - Maximum players counted for scaling
- **Multiplier Per Player** - Extra multiplier per player above the threshold

**Multiplier formula**:

```
extra = clamp(playerCount - PlayerThreshold, 0, PlayerCap - PlayerThreshold)
multiplier = 1 + extra * MultPerPlayer
```

**Example**: 4 players, `PlayerThreshold=2`, `PlayerCap=4`, `MultPerPlayer=0.25`:

```
extra = clamp(4 - 2, 0, 4 - 2) = 2
multiplier = 1 + 2 * 0.25 = 1.5x
```

5th player would not raise this further because of the cap.

### **3. Optional**

- **Disable Quota** - Disables the quota system entirely
- **Rollover Amount** - Percentage of excess fulfillment that carries over to the next quota. 0 = none

**Rollover example**: quota was 100, you sold $150 of scrap (overage = $50), `RolloverAmount = 0.5`:

```
carried = $50 * 0.5 = $25 → applied toward next quota
```

### **4. Penalties.Credits / 5. Penalties.Quota**

- **Enabled** - Apply the penalty when crew members die
- **On Gordion** - Apply the penalty even at The Company
- **Percent Per Player** - Per-death amount when `Dynamic = false`
- **Dynamic** - Switch to ratio-based mode using `PercentCap` as the scale
- **Percent Cap** - Hard ceiling (fixed mode) / scale (Dynamic mode)
- **Percent Threshold** - Penalties below this percent are ignored
- **Recovery Bonus** - Recovered bodies reduce the penalty

**Formulas**:

```
fixedMode: pct = dead * PercentPerPlayer        (clamped to PercentCap)
dynamicMode: pct = (dead / total) * PercentCap    (naturally ≤ PercentCap)

if recovered > 0:
    pct *= 1 - RecoveryBonus * (recovered / dead)

if pct < PercentThreshold: pct = 0
```

**Example A** — fixed mode, 8-player lobby, 2 dead, `PercentPerPlayer=0.15`, `PercentCap=0.5`:

```
pct = 2 * 0.15 = 30%   (under 50% cap → applied as-is)
```

**Example B** — dynamic mode, 8-player lobby, 2 dead, `PercentCap=0.05`:

```
pct = (2 / 8) * 0.05 = 1.25%
```

**Example C** — recovery bonus, 4 dead, 2 recovered, `RecoveryBonus=0.5`, base 30%:

```
pct = 30% * (1 - 0.5 * 2/4) = 30% * 0.75 = 22.5%
```

### **6. Loss.Scrap**

- **Enabled** - Randomly lose collected scrap items when the entire crew is wiped
- **Items Safe Chance** - Chance for each item to be protected from loss
- **Lose Each Scrap Chance** - Chance to lose an unprotected item
- **Max Lost Scrap Items** - Maximum scrap items that can be lost per round

**Per-item loss chance**:

```
lossChance = (1 - ItemsSafeChance) * LoseEachScrapChance
```

**Example**: 10 items on the ship, `ItemsSafeChance=0.5`, `LoseEachScrapChance=0.1`:

```
lossChance = 0.5 * 0.1 = 5% per item
expected losses ≈ 10 * 0.05 = 0.5 items per wipe (capped at MaxLostScrapItems)
```

### **7. Loss.Value**

- **Enabled** - Reduce scrap value of all ship items on full crew wipe
- **Percent** - How much of scrap value to remove. Stacks on repeated wipes

**Stacking example**: `Percent = 0.25` (each wipe removes 25%), 3 wipes in a row on the same scrap:

```
remaining = (1 - 0.25)³ = 0.75³ ≈ 0.42
=> scrap retains ~42% of its original value
```

### **8. Loss.Equipment**

- **Enabled** - Randomly lose purchased equipment when the entire crew is wiped
- **Lose Each Equipment Chance** - Chance for each equipment item to be lost
- **Max Lost Equipment Items** - Maximum equipment items lost per round

**Example**: 6 equipment items on the ship, `LoseEachEquipmentChance=0.05`, `MaxLostEquipmentItems=1`:

```
expected losses ≈ 6 * 0.05 = 0.3 items per wipe (capped at 1)
```

### **9. UI**

- **Quota Animation Speed** - Speed of the new quota pop-up animation. Higher = faster

### **X. Buy.Rate**

Native port of *BuyRateSettings* features. All entries default OFF; the Company's daily buy rate is vanilla until you enable something here. The host computes the rate and broadcasts it to clients — no desync.

- **MinMaxEnabled** - Clamp daily buy rate to `[MinRate, MaxRate]`. Required for `RandomRateEnabled`
- **MinRate / MaxRate** - The clamp / random-pick bounds (e.g. `0.2` = 20%)
- **RandomRateEnabled** - Pick the daily rate uniformly in `[MinRate, MaxRate]`
- **LastDayRateEnabled** - On the deadline's last day, override using `LastDayMinRate / LastDayMaxRate`
- **LastDayRangeChance** - Chance to use the range; on miss, fall back to 100%
- **LastDayMinRate / LastDayMaxRate** - Last-day bounds (or fixed if equal)
- **JackpotEnabled** - Allow a chance to roll a jackpot rate
- **JackpotLastDayOnly** - Only roll the jackpot on the deadline's last day
- **JackpotChance** - Chance per eligible day (e.g. `0.01` = 1%)
- **JackpotMinRate / JackpotMaxRate** - Jackpot bounds (or fixed if equal)
- **BuyRateAlertEnabled** - Yellow on-screen alert when the rate changes (per-client)
- **JackpotAlertEnabled** - Red `SCRAP EMERGENCY` alert with sound on jackpot (per-client)
- **AlertDelaySeconds** - Delay before the alert displays. Increase to 8+ if it overlaps BetterEXP / DiscountAlerts

**Precedence (host evaluates in order, first hit wins)**:

```
1. Jackpot       → JackpotEnabled AND chance roll AND (last-day if LastDayOnly)
2. Last-day rate → LastDayRateEnabled AND daysUntilDeadline == 0
3. Random rate   → RandomRateEnabled AND MinMaxEnabled
4. Min/Max clamp → MinMaxEnabled (clamps the vanilla rate)
5. Vanilla       → none of the above
```

**Sample run** (`JackpotEnabled=true`, chance=1%, last-day-only, `LastDayMin=Max=1.2`, `MinMax=true`, `Min=0.3`, `Max=1.0`):

| Day                    | Result                                      |
|------------------------|---------------------------------------------|
| Mid-quota, no jackpot  | Vanilla rate clamped to `[0.3, 1.0]`        |
| Last day, no jackpot   | `1.2` (120%) from last-day override         |
| Last day, jackpot hits | Random pick in `[1.5, 3.0]` + red alert     |

### **Compatibility — LethalConstellations**

A separate `com.seeya.configurablequota_constellations.cfg` is auto-generated only when LethalConstellations is detected. Each constellation gets its own block:

- **DeadlineMode** - `UseGlobal`, `Fixed`, or `Random`
- **FixedDaysToDeadline** - Used when set to `Fixed`
- **DeadlineMin / DeadlineMax** - Used when set to `Random`

---

## FAQ

### **Q: Can I use this with other quota mods?**

Not recommended.

---

## Credits

- Developed by **[seeya](https://thunderstore.io/c/lethal-company/p/seechela/)**
- Inspired mostly from QuotaOverhaul, AfineQuota, BuyRateSettings and ChocoQuota

---

## License

Distributed under the GPL v3 License.

---

### 💖 Support

If you enjoy my work, consider [supporting](https://www.buymeacoffee.com/see_ya) me. Donations are optional but greatly appreciated.

---
