# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.1] - 2026-08-12

### Fixed

- Scrap Keeper upgrade not keeping scrap on crew wipe
- Penalties and crew wipe losses not applying on modded company moons

## [1.5.0] - 2026-08-11

### Added

- LGU compatibility
- Scrap Insurance compatibility

### Fixed

- SSS items ignoring the scrap, value and equipment loss settings
- Quota clearing after reloading save when rollover already covers it
- Items inside belt bags and in a cruiser being deleted on a crew wipe
- Deadline not being rolled after getting fired or ejected without LethalConstellations
- Collected scrap being counted as new collected again on following round

## [1.4.4] - 2026-08-04

### Fixed

- Quota rollover is no longer -1 to one credit below next quota
- Quota no longer advances on days where nothing was sold and rollover already covers it

## [1.4.3] - 2026-08-01

### Changed

- Overtime option now counts only scrap sold above quota, instead of reducing the bonus by rolled over amount

## [1.4.2] - 2026-07-30

### Added

- Optional config option to lose percentage of banked rollover when the entire crew dies

## [1.4.1] - 2026-07-26

### Fixed

- Items stored in a Self Sorting Storage are no longer deleted when losing equipment or scrap on a crew wipe

## [1.4.0] - 2026-07-22

### Added

- Optional config option to stop rolled over scrap from also counting toward the overtime bonus

### Fixed

- Getting fired or ejecting in the same session now keeps the correct starting constellation deadline instead of the previous one
- Deadline and buy rate now sync to players who join mid game

## [1.3.1] - 2026-05-17

### Changed

- Dynamic Scrap Value scales from each moons base scrap values instead of current quota
- Dynamic Scrap Amount scales from moon baseline only and no longer adds to the Dynamic Scrap Value

## [1.3.0] - 2026-05-17

### Added

- Dynamic interior size, scrap value and scrap item count that scale by player count
- Dynamic enemy power that scales by player count

### Fixed

- Quota no longer resets several days in a row after a big sell

## [1.2.0] - 2026-05-09

### Added

- Buy rate settings - set a min/max, random rate, last-day rate or jackpot for the Companys daily buy rate
- Buy rate alert that shows the new rate each day, plus a red SCRAP EMERGENCY alert with sound when a jackpot rolls
- Formulas and examples to the README and to every config setting

### Changed

- Dynamic penalty mode now uses `(dead/total) x PercentCap` so the cap acts as the scale instead of a cap. Example: 2 dead out of 8 with cap 0.05 used to give 5%, now gives 1.25%

### Fixed

- Fines UI quota line now shows the real amount of the penalty

## [1.1.1] - 2026-05-08

### Fixed

- Starter constellation now shows the right deadline from the start
- _Advance Features_ - Scrap loss text on the performance report UI no longer flickers and is hidden when there was no scrap to lose

## [1.1.0] - 2026-05-07

### Added

- LethalConstellations compatibility with generated per-constellation deadline config [ `ConfigurableQuota_Constellations.cfg` ]
- Per-constellation deadline mode - `UseGlobal`, `Fixed` or `Random`

## [1.0.2] - 2026-05-07

### Fixed

- Quota rollover resetting to 0
- Deadline days now sync correctly to all clients after quota fulfillment
- _GeneralImprovements_ - Total Days and Total Quotas monitors now show correct values after rejoining
- _Advanced Features_ - Crew wipe now shows the real scrap loss instead of always _Lost 100% scrap_

## [1.0.1] - 2026-05-06

### Fixed

- Fines UI now shows correct casualty and recovery counts (was always showing 0 after player revival)
- Scrap value loss no longer desyncs between host and clients
- Body recovery detection improved
- Fines UI body line now shows _X of Y bodies recovered_

## [1.0.0] - 2026-05-05

### Added

- Initial release!