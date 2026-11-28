# Drowned Bell

A short 2D side-scrolling action demo in Unity. A small salvage diver climbs down a flooded bell tower to silence **the Undertoll**, the great bell whose tolling keeps drowning the lower halls. Responsive movement, close-range hook combat, readable enemy tells, four chapters (the Belfry, the Sluice Works, the Sounding Archive and the Tidal Engine) and four bosses that attack in a random order. The full four-area design is in [the campaign plan](docs/CAMPAIGN_PLAN.md).

The diver, enemies, landmarks and room tiles use original code-authored pixel art. The title and ending share a 24-color belfry illustration. Sound is synthesized in code. The game renders at 512×288 and scales to the window with nearest-neighbor filtering.

**Taking over the project?** Read [docs/START_HERE.md](docs/START_HERE.md) first, then the job list in [docs/NEXT_TASKS.md](docs/NEXT_TASKS.md).

**Versions:** `main` is the current version; each finished version is a GitHub Release with a tag (`v0.1` to `v0.8.4`), listed under [Releases](https://github.com/RubahAnomali/drowned-bell/releases). There are no version branches. See [CHANGELOG.md](CHANGELOG.md).

## Play

- **On Windows:** build the player with **Drowned Bell > Build Windows** in Unity (or `Unity.exe -batchmode -executeMethod BuildTools.BuildWindows`); it writes `Builds/Windows/DrownedBell.exe`. The build starts full screen (borderless) with the mouse cursor hidden; the QA modes stay windowed. `python tools/package_windows.py --output <zip>` packs it with the Unity runtime and a short controls guide.
- **Not played in the browser any more.** The WebGL build target still exists (**Drowned Bell > Build Web**), but the game is no longer hosted on the portfolio site.
- **In the editor:** Unity 6.6 (6000.6.10f1). Open `Assets/Scenes/Main.unity` and press Play. Any other scene also works, because `Bootstrap` creates the game if the scene has none.

| Action | Keys |
|---|---|
| Move | A / D or arrows |
| Jump (hold for higher) | Space or Z |
| Swing the hook | X or J. Hold W to swing up, hold S in the air to swing down. Left-click to aim with the mouse |
| Dash | C, Left Shift or K. Right-click to dash toward the cursor |
| Continue or new game | Title screen: Enter or click continues your saved run, N starts a new game |
| Pause | Esc |
| Mute | M |
| Inspector (hitboxes, states) | F1 or I |
| Test room | T on the title screen, or launch with `-testroom`. Pause and press T to return to the title |
| Shake strength and reduced flashing | 1 and 2 while paused (F5 and F6 also work in the Windows build) |
| Quit (title, pause, ending) | Q (Windows build only) |

The bosses are built to be hard but fair (see [docs/BOSS_DESIGN.md](docs/BOSS_DESIGN.md)): a direct hit costs two masks, and every 7 hook hits on a boss mend one. A down-swing in the air bounces off enemies and spikes (pogo) and gives back the air dash. When the bell rises for a slam, you can run underneath it to the open side.

Progress saves itself at lamps and the lift, pickups, opened shortcuts, boss clears and every respawn. Continue wakes you at the last lamp or lift with full health. One save slot: `drowned-bell-save.txt` in Unity's persistent data folder (on Windows, `%USERPROFILE%\AppData\LocalLow\RubahAnomali\Drowned Bell`; in a WebGL build, the browser's local storage). Finishing the Tidal Engine (the end of the campaign) clears it, so the next dive starts fresh. QA runs (`-autoplay`, `-autoshot`) never read or write it.

## The campaign

| Chapter | Where | Boss | New ideas |
|---|---|---|---|
| I | Drowned Belfry (7 rooms, below) | The Undertoll | movement, the hook, floor waves, the Brass Lung |
| II | Sluice Works (10 rooms) | Pump Warden | Limpet and Porter machines, vents, the counterweight lift, the keeper gauge |
| III | Sounding Archive (6 rooms) | Archivist of Depths | Ink Moths, Tuning Widows, echo drums, the drum key, the chute |
| IV | Tidal Engine (7 rooms) | Tidekeeper | Ballast Knight, two governor latches, spring pads, the Night Watch record, the bypass wheel and the dawn ending |

All four bosses choose their attacks at random from a seeded shuffle (a new game gets a new seed, `-seed N` replays one), within fixed fairness rules: telegraphs of at least 0.45 s and punish windows of at least 0.35 s. Details are in [docs/BOSS_DESIGN.md](docs/BOSS_DESIGN.md) and the chapter plan in [docs/CAMPAIGN_PLAN.md](docs/CAMPAIGN_PLAN.md). The route below is chapter I.

## The route

```
[1 Belfry Landing] -- [2 Flooded Nave] -- [3 Ripple Walk] -- [4 Gutter Shaft]
        | grate (shortcut)                                       | tall drop
[6 Bell Ropes] ------------------------ [5 Choir Gallery] -------+
        | glowing drop
[7 The Undertoll]
```

| # | Room | Purpose |
|---|---|---|
| 1 | Belfry Landing | Safe start. Learn to move, jump and swing; the **tide lamp** (checkpoint) and the great bell landmark are here, and a warm grate sits locked in the floor. |
| 2 | Flooded Nave | One **Brine Crab** in open space: learn spacing against its telegraphed lunge. |
| 3 | Ripple Walk | Watch a small bell wind up from a safe block, then jump its floor wave. |
| 4 | Gutter Shaft | Follow the ledges down past a **Knell Jelly** and land beside the spikes. |
| 5 | Choir Gallery | Combine enemy spacing and wave timing around a three-tile pit. An optional climb leads to the **Brass Lung** (+1 mask). |
| 6 | Bell Ropes | Hit the **winch** to open the grate and climb back to the lamp. Rest before the warm-lit drop. |
| 7 | The Undertoll | Apply the floor-wave jump from Ripple Walk to the boss's slam, then punish the recovery. |

After the winch, a retry is short: from the lamp, drop through the grate, then into the arena.

## Test room and traversal limits

The test room has a hit-counting target, one Brine Crab, pillars from one to four tiles, and shallow pits of four, six, eight and ten tiles. Its HUD shows live movement results alongside the default controller benchmarks.

| Measurement | Result |
|---|---:|
| Full jump rise | 3.26 tiles |
| Short hop rise | 1.09 tiles |
| Full jump airtime | 0.65 s |
| Running jump reach | 5.33 tiles |
| Standing-start jump reach | 4.96 tiles |
| Ground dash reach | 3.21 tiles |
| Running jump with dash | 7.78 tiles |

The critical path uses climbs of at most two tiles and pits of at most three. The Brass Lung climb is optional.

## Enemies and boss

| Who | Teaches | Tell | Attack | Punish window |
|---|---|---|---|---|
| Brine Crab | Spacing | Rears back, front claw rises and glows coral (0.55 s) | Fixed-distance claw lunge | 0.8 s recovery, claw down |
| Knell Jelly | Timing | Swells, brightens, a faint ring shows its full reach (0.9 s) | An expanding ring that only hurts inside its band | 1.3 s sag; harmless to touch |
| The Undertoll | Everything above | Rise with a landing shadow / lean back / coral marks on the ceiling | **Slam** with floor shockwaves (jump them) / **Rush** across the floor (stand on a side ledge) / **Drip rain** below half health (step out of the marks) | A recovery after every attack, mouth glowing; contact is harmless then |

The Undertoll picks from slam, rush and (in phase two) rain in a shuffled order. A rush needs you to be at least 6 tiles away, a slam can be delayed, and in phase two some attacks chain. Its attacks and punish windows are otherwise as above.

## Rules

- Five masks of health, six with the Brass Lung. A swing can only damage a given enemy once. After a hit you are invulnerable for 1.1 s and knocked away.
- **Spikes** cost a mask and put you back on the last safe ground.
- **Death** returns you to the lit tide lamp at full health. Ordinary enemies and an unfinished boss fight reset. **The opened grate, the lit lamp and the Brass Lung stay** for the rest of the run.
- The Tidal Engine (chapter IV) follows the Archive: throw the two governor latches (one in the Flywheel Cradle behind a Ballast Knight, one on the Condenser Bridge), take the lift back to the dock, defeat the Tidekeeper in the Sea Gate (strike its hinge while it resets) and turn the bypass wheel. The optional Night Watch under the bridge holds the third keeper record behind three spring pads. Walk out through the Morning Nave to finish; the completion screen shows the keepers you found.
- Beating the bell opens the Sluice Works interlude. Press Enter or click to continue with the same upgrade, elapsed time and death count. A new lamp marks the start of the Works. Follow the pressure main through Intake Dock, Bleeder Gallery and Valve Hall. Strike the wheel to enter Impeller Well. Jump the drain for the direct route or drop to Sediment Trap for the keeper record. W or Up rides the maintenance lift. Beyond Counterweight Walk, evade the Pump Warden’s marked relief pipe and piston sweep. Strike its open brass housing during retraction. Pick up its key and walk to the eastern hatch to finish chapter two. Progress lasts one session.

## Tuning

Select the **Drowned Bell** object in `Main.unity`. The Inspector shows four groups: movement, combat, enemies and boss. They cover run speed, acceleration, jump height and apex time, coyote time, jump buffer, dash, attack windup/active/recovery/reach, knockback, invulnerability, and every enemy and boss timing. Edits made during Play apply immediately, but Unity throws them away when Play stops. To keep values, edit in Edit mode.

## Architecture

```
Assets/Scripts/Core/   all rules, no UnityEngine: tested headless
  World.cs        fixed 60 Hz step, combat resolution, hazards, checkpoint, shortcut, arena lock, death/respawn, ending
  Player.cs       run, variable jump, coyote time, jump buffer, dash, 3-way swing, pogo, hurt/invulnerability
  Physics.cs      box vs tile collision per axis in small steps (no tunnelling), one-way ledges, drop-through
  Enemies.cs      Brine Crab and Knell Jelly state machines
  Boss.cs         The Undertoll: slam, rush, rain, shockwaves, drips, phase two
  PumpMachinery.cs  Limpet, Porter, Pump Warden and the Ballast Knight; SluiceWorks.cs the chapter II map and route
  ArchiveFoes.cs / SoundingArchive.cs   chapter III foes and map
  EngineFoes.cs / TidalEngine.cs        the Tidekeeper and the chapter IV map, stations and route
  SaveGame.cs     versioned save format; Dice.cs the seeded shuffle bags
  Level.cs        tile grid and room rectangles; RoomData.cs is the generated ASCII map
  LevelPlan.cs    pacing beats and gated connections
  RippleBell.cs   room hazards that reuse the boss shockwave
  Tuning.cs       [Serializable] tuning classes shown in the Inspector
Assets/Scripts/Game/   Unity glue
  GameController  loop, hit-stop, room-clamped camera, HUD, F1 inspector
  LevelView / ActorViews / Fx / Art   meshes and sprites generated in code; Engine rooms draw the finished plates
  CampaignArt / ArchiveArt / EngineFoeView / PumpMachineView / ArchiveFoeView   the finished art (Assets/Resources)
  AudioSynth      every sound synthesised (bells from inharmonic partials)
  AutoPlay        verification only: -autoplay <folder> plays all four chapters through the real build
  AutoTour        verification only: launch with -autoshot <folder> to screenshot every room, tell and ending, then quit
Assets/Editor/BuildTools.cs   Drowned Bell > Create Main Scene / Build Windows (also used from batch mode)
tests/Core.Tests              dotnet run: the checks below
tools/rooms.py (+ sluice, archive_rooms.py, engine_rooms.py)   the layout sources; each regenerates its Core/*Data.cs
```

## Verification

What actually ran:

- **Unity compile:** `Unity.exe -batchmode -executeMethod BuildTools.CreateScene` compiled every script with 0 errors and 0 warnings and created the scene.
- **Windows build:** `Unity.exe -batchmode -executeMethod BuildTools.BuildWindows` produced `Builds/Windows/DrownedBell.exe` (0 errors).
- **Screenshot tour of the Windows player:** `DrownedBell.exe -autoshot <folder>` saved 23 frames with no logged exceptions, including the Ripple Walk wind-up and wave, the arena attacks, and the victory screen. Earlier screenshot reviews fixed four issues:
  - the ending only started when a swing landed the last hit;
  - the boss bar covered the arena floor;
  - the bell rendered too dark;
  - the crab and the drip warnings were hard to see.
- **Headless suite:** `cd tests/Core.Tests && dotnet run -c Release` passes (256 checks as of v0.8.1: the list below is chapter I; chapters II to IV, saves, random bosses, boss fairness, smooth drawing and the Tidal Engine have their own groups, and `DIFFICULTY_SEEDS=1 dotnet run -c Release` prints the 8-seed boss table). The chapter I checks cover:
  - **Movement:** full jump height matches tuning, a short hop, coyote time accepts at 0.05 s and refuses at 0.2 s, the jump buffer works, dash distance, one air dash per airtime, no wall tunnelling, one-way ledges and drop-through.
  - **Combat:** one damage per swing, invulnerability, knockback, spikes cost a mask and return you to the safe spot, the pogo off spikes.
  - **Crab:** the lunge hits a diver standing still, backing off avoids it, and the recovery can be punished.
  - **Jelly:** the ring only hits inside its band, and a spent jelly is safe.
  - **Boss:** a standing diver is hit by the shockwave and a jumping one is not; the rush passes under a ledge; recoveries take hits; phase two adds the rain; punishing alone wins; no illegal state transition anywhere.
  - **Death and persistence:** respawn at the lamp at full health, enemies and boss reset, the grate and the Brass Lung persist.
  - **Route:** every room, the lamp, the winch and the Brass Lung are reachable. A no-dash audit checks critical climbs and pits against measured limits. Pacing and gated-route checks cover the climb back to the lamp and the one-way arena entry.
  - **Determinism:** two runs with the same inputs end identically.

## QA autopilot

`DrownedBell.exe -autoplay <folder> [-retries N]` plays the game itself through inputs only. It lights the lamp, takes the Brass Lung, opens and climbs the shortcut, dies N times on purpose in the boss fight, then wins, and goes on through the Sluice Works, the Sounding Archive and the Tidal Engine (including the Night Watch) to the credits and a clean new dive. `-seed N` fixes the boss order. It saves screenshots and a report, then quits. `-autoshot <folder>` saves a tour of fixed moments. Combine it with `-testroom` to capture the test HUD after an automated swing at the dummy. See `docs/QA_REPORT.md`.

## Known issues and not yet verified

- **Playtested by hand.** The author has played the game personally and had friends play it. The routes, every fight and the ending are also verified by the headless simulation and by the bot in the real Windows build (the last run, v0.8.1, cleared all four chapters with 0 runtime errors). No formal playtest data (completion times, death counts) was recorded, so the 55 to 80 minute campaign target and the difficulty numbers still come from scripted divers rather than measured players; use F1 and the Inspector to tune.
- The diver can't climb back up the Gutter Shaft. That is by design: the way back is the shortcut, and the route check proves you can always reach the winch.
- The boss arena locks you in until the fight ends or you die, as is usual for the genre.
- See `docs/ART_DIRECTION.md` and `docs/asset-manifest.json` for sprite dimensions, palette and display scaling.
- Gamepad support is basic: the left stick and face buttons through the legacy input manager. Only keyboard was considered in design.
