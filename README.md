# AutoRemoveFinishedBurns [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Auto-remove finished burns from the burn plan in [Kitten Space Agency](https://ahwoo.com/app/100000/kitten-space-agency).

In stock KSA, when an auto-burn completes the flight computer flips the
burn mode to Manual but leaves the burn entry in the plan. You then have
to click "Delete" manually before the next maneuver can take focus. This
mod cleans up completed auto-burns automatically.

This mod is written against the [StarMap loader](https://github.com/StarMapLoader/StarMap).

Validated against KSA build version 2026.4.18.4206.

## Features

- **Auto-removes finished auto-burns** from the burn plan as soon as the
  flight computer flips out of Auto mode on completion.
- **Out-of-fuel safe**: completion is confirmed via the same delta-V
  vector reversal the stock flight computer uses internally, so a burn
  that flamed out before reaching its target stays in the plan and can
  be resumed after staging.
- **Auto-only**: manual burns are never touched. Players keep full
  control to fine-tune by hand.
- **In-game toggle** in the Mods settings tab. Setting is persisted to
  a TOML file in the mod's user directory.

## Installation

1. Install [StarMap](https://github.com/StarMapLoader/StarMap).
2. Download the latest release from the [Releases](https://github.com/Maximilian-Nesslauer/KSA-AutoRemoveFinishedBurns/releases) tab.
3. Extract into `Documents\My Games\Kitten Space Agency\mods\AutoRemoveFinishedBurns\`.
4. The game auto-discovers new mods and prompts you to enable them. Alternatively, add to `Documents\My Games\Kitten Space Agency\manifest.toml`:

```toml
[[mods]]
id = "AutoRemoveFinishedBurns"
enabled = true
```

## Dependencies

| Package | Purpose | Tested version |
| --- | --- | --- |
| [StarMap](https://github.com/StarMapLoader/StarMap) | Mod loader, required at runtime (see [Installation](#installation)) | 0.4.5 |

## Build dependencies

Required only to build the mod from source. Targets **.NET 10**.

| Package | Source | Tested Version |
| --- | --- | --- |
| [StarMap.API](https://github.com/StarMapLoader/StarMap) | NuGet | 0.3.6 |
| [Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony) | NuGet | 2.4.2 |

## Mod compatibility

- Known conflicts: none

## Notes

- Detection uses the delta-V vector reversal signal the stock flight
  computer already evaluates each tick to flip Auto -> Manual on burn
  completion. If the vector hasn't reversed (you ran out of fuel before
  reaching the target) the burn entry is preserved.
- The toggle is persisted to
  `Documents\My Games\Kitten Space Agency\mods\AutoRemoveFinishedBurns\autoremovefinishedburns.toml`.

## Check out my other mods

- [AutoStage](https://github.com/Maximilian-Nesslauer/KSA-AutoStage) - automatic staging during auto-burns and manual flight, with configurable ignition delays
- [StageInfo](https://github.com/Maximilian-Nesslauer/KSA-StageInfo) - extra info in the stock Stage/Sequence window: per-stage delta V, TWR, burn time, fuel pool, RCS, and more
- [AdvancedFlightComputer](https://github.com/Maximilian-Nesslauer/KSA-AdvancedFlightComputer) - set periapsis / set apoapsis / match or set inclination quick-tools in the Transfer Planner, plus hyperbolic-target support (Oumuamua, 2I/Borisov, 3I/ATLAS)
