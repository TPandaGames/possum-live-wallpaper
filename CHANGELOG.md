# Changelog

All notable changes to this project are documented here. Full notes for each release live in
[`docs/release-notes/`](docs/release-notes/) and on the [Releases](https://github.com/TPandaGames/possum-operational-screensaver/releases) page.

## [1.1.0] - 2026-11-13

Weather sync. See the [full release notes](docs/release-notes/v1.1.0.md) or download
[PocketPossum.scr](https://github.com/TPandaGames/possum-operational-screensaver/releases/download/v1.1.0/PocketPossum.scr).

### Added

- Optional **weather sync** (Open-Meteo, off by default): cloud cover, rain, snow, fog and thunderstorms with lightning,
  a temperature/conditions line under the clock, and real sunrise/sunset plus season and hemisphere from the chosen location
- City search in Settings, and a tabbed Settings dialog
- Inno Setup installer script (`installer/PocketPossum.iss`), built by the GitHub Actions workflow

### Fixed

- The live preview in the Screen Saver dialog now uses your day/night and season settings instead of the defaults

## [1.0.0] - 2026-11-12

First release. See the [full release notes](docs/release-notes/v1.0.0.md) or download
[PocketPossum.scr](https://github.com/TPandaGames/possum-operational-screensaver/releases/download/v1.0.0/PocketPossum.scr).

- Windows screensaver (`.scr`) hosting an HTML canvas scene in WebView2, on every monitor, with live preview
- Settings dialog, idle-timer tray app and optional run-at-login
- Day and night from the system clock, with sunrise/sunset blending and configurable hours
- Four seasons from the local date, with hemisphere detection from the time zone
- Burn-in protection: wandering clock, travelling moon, drifting scene
- Barnaby, Bandit and the Maple family (Pip, Nibs and Dot), with random chases, standoffs, snack theft and more

[1.1.0]: https://github.com/TPandaGames/possum-operational-screensaver/releases/tag/v1.1.0
[1.0.0]: https://github.com/TPandaGames/possum-operational-screensaver/releases/tag/v1.0.0
