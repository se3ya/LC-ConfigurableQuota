# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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