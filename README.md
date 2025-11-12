# Quota – Advanced Quota Control Mod

Fully customizable profit quota system for Lethal Company. This mod centralizes and extends the configuration knobs found across several existing quota mods (ChocoQuota, QuotaOverhaul, FastNewQuota) while aiming for stability and clarity.

## Features

- Configure starting credits, starting quota, and days-to-deadline.
- Linear + optional exponential growth with level-out and hard cap.
- Player-count based scaling (threshold, cap, per-player multiplier).
- Randomizer multiplier for variability in quota increases.
- Optional rollover: carry a portion of excess quota fulfillment into the next cycle.
- Disable the quota system entirely (free exploration mode) without breaking UI.
- Adjustable new quota animation speed (instant to slow-mo).
- Exposed penalty and item loss configuration (implementation stubs for further expansion).
- Safe starting credits application (does not overwrite higher existing credit values).

## Installation

1. Place `Quota.dll` in your BepInEx `plugins` folder.
2. Launch the game once to generate the config file (`Quota.cfg`).
3. Edit the config values to your liking.

## Key Config Sections

Section: `0. Basic`

- `StartingCredits` – Credits granted at session start (only raises if lower).
- `DaysToDeadline` – Number of days per quota cycle.
- `StartingQuota` – Initial quota value.

Section: `1. Growth`

- `BaseIncrease` – Baseline linear increase.
- `CurveSharpness` – Higher value = gentler exponential; 0 disables extra growth.
- `RandomizerMultiplier` – ± variability (0 = none, 0.25 ≈ ±25%).

Section: `2. Leveling`

- `FinalLevel` – Switch point where exponential stops (-1 disables).
- `FinalIncrease` – Flat increase applied after `FinalLevel`.
- `QuotaCap` – Hard ceiling (-1 disables).

Section: `3. PlayerScaling`

- `EnablePlayerMultiplier` – Toggle player-based scaling.
- `PlayerThreshold` – Players above this count scale further.
- `PlayerCap` – Max players counted.
- `MultPerPlayer` – Additional multiplier per extra player (0.25 = +25%).

Section: `4. Optional`

- `DisableQuota` – Disables quota logic but keeps UI elements functional.
- `RolloverAmount` – Portion (0–1) of excess fulfillment applied to next quota.

Section: `5–6 Penalties.*` (stubs)

- Provide forward compatibility with penalty systems from other mods. Currently not fully implemented.

Sections: `7–9 Loss.*` (stubs)

- Scrap/equipment/value loss options – reserved for future behavior expansions.

Section: `10. UI`

- `QuotaAnimationSpeed` – Multiplier for the new quota animation (1 = vanilla, very high = near instant).

## Rollover Behavior

If you exceed the previous quota and `RolloverAmount > 0`, a portion of the overage is immediately applied toward the next quota’s fulfillment progress.

## Disable Quota Mode

When `DisableQuota = true` the quota no longer updates or progresses. The displayed quota remains for UI consistency. You can re-enable the system mid-session by toggling the config and reloading.

## Compatibility & Safety

- Avoid mixing this mod with other quota-altering mods to prevent patch conflicts.
- Player scaling relies on Netcode’s connected client list; dedicated server counts should be accurate.
- Patches gracefully fail over to vanilla logic if an exception occurs.

## Planned Extensions

- Implement penalties and loss mechanics.
- Server-side sync & late-join catch-up for custom quota state.
- Configurable per-moon/day multipliers.

## Troubleshooting

- If quota does not change: verify `DisableQuota` is false.
- Animation too fast/slow: adjust `QuotaAnimationSpeed` (e.g., 10 for very quick).
- Conflicts: remove other quota mods from `plugins`.

## Contributing

Open to PRs adding safely isolated features (ensure Harmony patches are minimal and guarded).

---

Enjoy tailoring your runs to any difficulty curve you want!
