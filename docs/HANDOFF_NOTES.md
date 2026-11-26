# Drowned Bell handoff notes

## Harder bosses, 27 November 2026 (v0.5)

The three bosses are hardened; read `docs/BOSS_DESIGN.md` first. Tuning: `BossTuning` (and `Main.unity`, which `SceneTuning` keeps equal to the code) for the Undertoll; constants and properties in `PumpMachinery.cs` for the Warden and `ArchiveFoes.cs` for the Archivist. Damage: `Enemy.ContactDamage` and `AttackDamage`, `BossTuning.ContactDamage`; healing: `World.BossHitLanded` and `BossTuning.HealEveryHits`. The scripted divers got smarter: `Autopilot.BossFight` (crosses a recovering bell, never ends a recovery inside it), `SluiceWorks.WardenTactic`, and the Archivist branch of `SoundingArchive.RouteInput`. `BossDesignChecks.cs` holds the hard/fair rules and `BossDifficulty.cs` the reader/greedy/idle fights. Art is unaffected: lanes draw from `AttackLane`, so the wide and tracked jets already show.

## Sounding Archive mechanics, 27 November 2026 (v0.4)

Chapter III is playable end to end with placeholder shapes. Map: `tools/archive_rooms.py` generates `Core/ArchiveData.cs`; `Core/SoundingArchive.cs` holds the builder, stations (records, keys, order, chute rides) and the input-only `RouteInput`. Foes: `Core/ArchiveFoes.cs` (InkMoth, TuningWidow, Archivist); echo drums are `RippleBell` with `Echo`/`Harmless` ('O' live, 'o' practice). `Level.Chapter` now identifies the chapter; `Tile.KeyDoor` ('K') opens with the drum key. The Pump Warden's victory continues into the Archive (`World.EnterArchive`) and saves its start; finishing the Archive clears the save. Visuals: `Game/ArchiveFoeView.cs` and the `PLACEHOLDER` sprites in `LevelView.cs`; the art pass is specified in `docs/ARCHIVE_ART_BRIEF.md` and is being handed to another agent. Debug: `ARCHIVE_TRACE=1 dotnet run` prints the route. Next mechanics per the plan: Tidal Engine (chapter IV).

## Save and continue, 27 November 2026 (v0.3)

Step 3 of the campaign plan is done. `Core/SaveGame.cs` holds the versioned key=value save format; `World.Snapshot()` and `World.Restore()` turn a run into a save and back (no positions: Continue wakes at the last lamp or lift with full health; after the Undertoll the save already points at the start of the Sluice Works). `Game/SaveStore.cs` writes `drowned-bell-save.txt` in `Application.persistentDataPath` through a temp file, falls back to it on read, and is switched off by `-autoplay` and `-autoshot`. The title shows Continue (Enter) and New game (N); finishing the Works clears the save. The browser template sets `autoSyncPersistentDataPath`. Headless "Save and continue" checks cover the format, damaged files and both chapters. Next per the plan: Sounding Archive (step 4).

## Full Sluice Works, 27 November 2026

The current Windows package is Builds/Drowned-Bell-Full-Works-Windows-x64.zip. Start in the Belfry and continue after the Undertoll. The Works now includes Impeller Well, the optional Sediment Trap and keeper record, Counterweight Walk, and Duty Pump. PumpMachinery.cs defines the Limpet, Porter and Warden. PumpMachines.png and PumpMachineView.cs provide four poses per actor and geometry-driven attack tells. The Warden drops a key for the eastern archive hatch. Save/Continue, Sounding Archive and Tidal Engine are still planned. See docs/QA_REPORT.md for verification and outstanding hands-on checks.

## Campaign continuation

Start with docs/CAMPAIGN_PLAN.md. The Windows Sluice preview contains ten rooms. The next production rooms are Impeller Well, Sediment Trap, Counterweight Walk and Duty Pump. Build the Piston Limpet, Chain Porter and Pump Warden in that order. The inspection hatch is the temporary endpoint. Local save/continue is planned. Keep earlier work local, and use built-in autoplay for standalone QA. See docs/QA_REPORT.md for current verification.

## Current Windows art build, 27 November 2026

The latest shareable Windows build is Builds/Drowned-Bell-Windows-x64.zip. Extract it and launch Drowned Bell/DrownedBell.exe. The local player is Builds/Release/DrownedBell.exe. It contains the illustrated title and ending, tide lamp, shortcut wheel and twin-chamber Brass Lung. The previous QA and browser builds predate this art pass. Repackage subsequent editor Windows builds with python tools/package_windows.py. Keep all work local unless publication is authorized.

## Latest acceptance QA

Use the local player at `Builds/QA/DrownedBell.exe` or extract `Builds/DrownedBell-Windows-QA.zip`. The packaged files were verified byte-for-byte against the player. The separate editor-menu build in `Builds/Windows` is older.

The review fixes are in place. The boss rain spread uses five drips in the Main scene and in the rebuilt player. One headless check covers all four saved tuning groups. It failed against the original scene on boss.RainDrops alone. The route tests now exercise NavGraph directly, and Reachability.cs is gone. The boss trace starts through Autopilot.StartAt and reached boss death. The unused idle counter and BossView.Tick arena argument were removed.

All 97 headless checks passed. The fresh standalone pilot reached one victory in 122.0 seconds of game time with no deaths or runtime errors. A second standalone run reached one victory at 199.2 seconds after three planned deaths with zero runtime errors. Each respawn-to-arena return took 5.4 seconds plus a 1.6-second death fade. See `docs/QA_REPORT.md` for standalone timings and the remaining hands-on acceptance checks. User rules still apply: keep work local, omit AI attribution in commits, and do not drive the game window.

## Current handoff — 2026-11-26

Continue from this section. The original handoff follows for architecture, commands and historical rationale.

### Latest steering and pixel-art pass

- The user asked for a consistent pixel-art style throughout the game and offered the installed Aseprite application. The standard install and Steam library checks did not expose its executable, so the art is authored directly in the project with no external runtime dependency.
- `PixelArt.cs` holds the diver poses, separate hook, crab, jelly, Undertoll, crack overlay and health helmet. The ripple bell remains in `Art.cs`. `LevelView` draws stepped arch recesses, waterlines, masonry, and platforms. The camera, effects, UI and bitmap text share a 512×288 native frame. `GameInput` remaps mouse aim through the fitted viewport and the F1 overlay follows the same projection.
- A native test-room smoke run exposed a default-coordinate sentinel issue. `Level.HasBoss` is now set from the actual boss marker, so the test room has no arena and the dummy is visible. The built player recorded one hit from one QA swing. The headless suite includes a regression check.
- Final Unity batch build succeeded with zero errors. Its normal-window `-autoplay` run reached victory in 178.2 simulated seconds, with two planned boss deaths, return trips of 5.4 seconds and zero logged errors. The final `-testroom -autoshot` run exited cleanly and saved the readout and screenshot. Results are in `docs/autoplay_report.txt` and `docs/screenshots/pixel_test_room.png`.
- Launch the built-in QA modes normally if collecting PNGs. Starting the game with a hidden window can cause Windows screen capture failures. No game-window input or automation was used outside the game's built-in QA.

### Completed

- Unity 6.6 batch tooling opened this project, compiled the previously unbuilt test-room code and built the Windows player. The first seven-room route build also succeeded. Use the editor and project paths below.
- Refreshed only `level-design`, `create-game-assets` and `game-feel` in `.claude/skills`. Verified relative paths and SHA-256 against upstream commit `c6ca6c41c839b68319ffc08430b99ab768debc32`, plus the three frontmatter names. The folders have 2, 11 and 2 files respectively, references included. The upstream tree has no `unity-2d-controller` or `unity-combat`; continuing with the project's original controller and combat implementation.
- `python tools/rooms.py` regenerates `Assets/Scripts/Core/RoomData.cs`. The current 128×54 map has seven rooms. Ripple Walk introduces the jump that counters the Undertoll's slam. Choir Gallery combines it with combat and a three-tile pit. Bell Ropes provides a breather and the winch unlocks the return grate.
- `RippleBell.cs` runs in the active room, broadcasts the wind-up and toll, and uses the boss's `Shockwave` movement and hitbox. `LevelView` adds the animated hanging bell and floor cue, and `AudioSynth` plays a wind-up and strike. Waves stop at solid walls and reset on room exit and respawn.
- The autopilot jumps training ripples and includes delayed sideways movement for the staggered shortcut ledges. `LevelPlanChecks.cs` validates the pacing curve, winch state, return path, measured climb and gap limits, and jumping the training wave. The complete headless suite passes, including two intentional boss retries in the full playthrough and four in the regression check. The two-retry run finished in about 174 simulation seconds, with both returns from the lamp in 5.4 s and no unplanned deaths or spike hits.
- The test room is wired into the title with T and has a live last-jump and last-dash readout, benchmark panel, and hit-counting target. T from its pause screen returns to the title. `-testroom -autoshot <folder>` exercises it through the existing automated capture path. Do not drive the game window.
- `docs/ART_DIRECTION.md` and `docs/asset-manifest.json` specify the flooded-tower pixel direction and the implemented art. The ripple bell and eight actor or equipment exports passed the skill's size, alpha and palette checks. The screenshot tour was used to inspect the pixel art in context. The original section below describes the earlier art plan before this pass.
- The route's feedback pass added smooth trauma shake, eased jump/land deformation, short hurt tints, impact tiers and a 145 ms presentation cap on hit-stop. F5 cycles camera shake through 100%, 50% and off in pause. F6 toggles reduced flashing. Both settings persist.

### Remaining verification

- See `docs/QA_REPORT.md` for the completed batch build, standalone autoplay, and test-room smoke results.
- Human play, input feel in the standalone player, audio balance, and first-time run length remain unmeasured.
- The project is local and ignored by the parent portfolio repository. Keep the project-local skills in their requested location. The user's commit attribution and game-window rules below still apply.

## Original handoff and implementation notes

Written mid-task so another assistant can continue. Everything is local: no git repo, nothing pushed.

## The project

- Unity 6.6 (6000.6.10f1) project: `D:\portofolio web\game dev porto\drowned-bell`
- Unity editor: `D:\game\unity\unity installs\6000.6.10f1\Editor\Unity.exe` (Windows Mono build support installed)
- 2D soulslike demo: a salvage diver descends a flooded bell tower to silence the boss bell "the Undertoll".
- User-facing docs: `README.md` (controls, route, architecture), `docs/QA_REPORT.md` (last QA pass).

### Architecture (keep this split)

- `Assets/Scripts/Core/`: all rules in plain C#, with **no UnityEngine**. Tested headless with `dotnet`.
  - `World.cs`: fixed 60 Hz step, combat, hazards, checkpoint, shortcut, arena lock, death and respawn, ending.
  - `Player.cs`: movement, coyote time, jump buffer, dash, 3-way swing, pogo, mouse aim.
  - `Physics.cs`: tile collision.
  - `Enemies.cs`: Crab, Jelly, and the new test-room Dummy.
  - `Boss.cs`: the boss.
  - `Level.cs`: parses the ASCII map.
  - `RoomData.cs`: the map. Generated; don't hand-edit once the generator exists.
  - `Tuning.cs`: all feel numbers, shown in the Inspector.
  - `NavGraph.cs`: records real controller moves between standing spots; used by the tests and the autopilot.
  - `Autopilot.cs`: QA bot that plays only through `PlayerInput`.
  - `TestRoom.cs`: the test room and `Metrics.Measure()`.
- `Assets/Scripts/Game/`: Unity glue.
  - `GameController.cs`: the only MonoBehaviour; loop, camera, IMGUI HUD.
  - Views: `LevelView`, `ActorViews`, `Fx`, `Art`. All art is made in code; there are no asset files.
  - `AudioSynth.cs`: all sound synthesised.
  - `GameInput.cs`: keyboard and mouse, legacy Input Manager.
  - `AutoTour.cs` and `AutoPlay.cs`: QA launch modes.
- `Assets/Editor/BuildTools.cs`: menu items plus batch-mode entry points (`CreateScene`, `BuildWindows`).
- `tests/Core.Tests/`: headless test suite: `cd tests/Core.Tests && dotnet run -c Release`.
- `tests/rooms.py`: the old layout source for the 6-room map.

### Commands

```powershell
# headless tests
cd "D:\portofolio web\game dev porto\drowned-bell\tests\Core.Tests"; dotnet run -c Release
# compile + build Windows player (fails with "Scripts have compiler errors" and lists error CS lines in the log)
& "D:\game\unity\unity installs\6000.6.10f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\portofolio web\game dev porto\drowned-bell" -executeMethod BuildTools.BuildWindows -quit -logFile build.log
# automated playthrough of the built game (screenshots + report.txt in the folder)
& "D:\portofolio web\game dev porto\drowned-bell\Builds\Windows\DrownedBell.exe" -autoplay C:\temp\play -retries 2
```

### Rules the user set

- **Commits:** no Claude / AI attribution, if commits are ever made. The user's commits use the GitHub noreply identity `RubahAnomali <206820789+RubahAnomali@users.noreply.github.com>`.
- **Window control:** the user declined giving Claude control of the game window. Use the `-autoplay` / `-autoshot` modes and the headless tests instead.
- **Feedback:** the user said the game "feels like AI slop". The current task is fixing that.

## What happened most recently

1. **Mouse and keyboard support added.** Left click swings toward the cursor (up, down in the air, or sideways), right click dashes toward it, and clicking confirms menus. Q quits from the title, pause and victory screens. Mouse-aim tests pass. **The last Unity build (`build7`) included this and succeeded.**
2. **Three skills installed** from `https://github.com/gamedev-skills/awesome-gamedev-agent-skills` into `drowned-bell/.claude/skills/`: `level-design`, `create-game-assets` and `game-feel`. The copies are byte-identical to the source, references included, and their SKILL.md frontmatter checks out.
   - The user also asked to use `unity-2d-controller` and `unity-combat`. **Those two skills do not exist in that repo.** I searched the whole repo; its `skills/unity` has only animation, build-pipeline, build-size, csharp-scripting, input-system, navmesh, physics, scriptableobjects and tilemap-2d. I used the project's own controller instead. Tell the user.
3. **Test room plus measurements (done, headless only).**
   - `TestRoom.cs` is a 64×18 room: pillars of 4, 3, 2 and 1 tiles, shallow pits of 4, 6, 8 and 10 tiles, a hit-counting target `Dummy` (marker `T`) and one crab.
   - `World` now takes an optional `Level`, and `NavGraph` takes an optional `Level` plus `allowDash`.
   - The checks are in `tests/Core.Tests/TestRoomChecks.cs` and pass.
   - **Not yet wired into the game**: there's no way to open the test room in Unity yet. A `-testroom` flag or a T key on the title, plus a live "last jump" readout in the HUD, are still to do.
   - **The Core changes since `build7` (Dummy, TestRoom, Metrics, NavGraph and World params) have not been compiled in Unity yet.** Run the batch build first.

### Measured controller metrics (from `Metrics.Measure()`)

| Measure | Value |
|---|---|
| Full jump rise | 3.26 tiles |
| Short hop | 1.09 tiles |
| Airtime | 0.65 s |
| Running jump reach | 5.33 tiles |
| Standing jump | 4.96 tiles |
| Ground dash | 3.21 tiles |
| Running jump + dash | 7.78 tiles |

Test room confirms: 1 to 3 tile climbs work, 4 doesn't; a 4-wide pit clears without a dash, a 6-wide one only with it, and 8 and 10 never.

**Design rules** (level-design skill: required ≤ 0.7 × reach, optional ≤ 0.95 × reach):
- **Required, critical path, no dash:** climbs ≤ 2 tiles, gaps ≤ 3 tiles.
- **Optional:** climbs ≤ 3 tiles, gaps ≤ 5 tiles, or ≤ 7 with the dash.

## In progress when interrupted: rebuild the route (7 rooms)

I was writing `tools/rooms.py` (a generator that builds the map and writes `Assets/Scripts/Core/RoomData.cs`). **The file was NOT created** because the shell heredoc failed. Write it with a file-write tool instead. Map: 128×54, room cells of 32×18. Unused cells (cols 32 to 127, rows 36 to 53) are solid rock.

| # | Room | Global cell (col, row) / size | Purpose (beat, intensity) |
|---|---|---|---|
| 1 | Belfry Landing | (0,0) 32×18 | teach 0.1: start `P`, lamp `C`, landmark bell `b`, a required 2-tile step block, optional perch, locked grate `G` at cols 26-28 in the floor |
| 2 | Flooded Nave | (32,0) 32×18 | teach combat 0.4: one crab `c`, optional 3-tile perches |
| 3 | **Ripple Walk (new)** | (64,0) 32×18 | teach timing 0.3: a **ripple bell `W`** hangs from the ceiling and sends floor ripples both ways (same object as the boss shockwave); a 2-high safe block near the entrance that ripples break against |
| 4 | Gutter Shaft | (96,0) 32×36 | develop 0.6: entry ledge, then a ledge you land on by walking off, then a third ledge; a spike floor with the safe landing by the bottom-left exit; one jelly `j` |
| 5 | Choir Gallery | (32,18) 64×18 | combine 0.8: crab, second ripple bell, a **3-wide** spike pit (spikes one tile down) under a jelly; optional 3/3/3 climb to the Brass Lung `H` |
| 6 | Bell Ropes | (0,18) 32×18 | rest 0.2: winch `L`; shortcut column cols 26-28 up to the grate with **staggered** one-way ledges (col 26 at rows 14/10/6/2, col 28 at rows 12/8/4/0) so climbing is 2-tile steps and dropping is a straight fall; boss drop hole cols 4-7 with warm arch lamps `a` |
| 7 | The Undertoll | (0,36) 32×18 | climax 1.0: door `D` row 1 cols 4-7; side one-way ledges 3 up (cols 1-6 and 25-30, row 13), each with a 2-high rock pedestal underneath (cols 1-2, 29-30, rows 14-15) so no 3-tile climb is required; boss `B` |

**Connections** (global rows):
- R1|R2, R2|R3 and R3|R4: rows 13-15.
- R4 bottom-left (rows 31-33) to R5's right edge, and R5's left edge (rows 31-33) to R6.
- R6 floor hole into R7.
- R6's top (cols 26-28) up to the R1 grate.

### Still to do for the route

1. **`W` ripple bell in Core.**
   - Parse marker `W` in `Level.cs`.
   - Add a `RippleBell` class: period about 2.2 s; wind-up about 0.8 s shown by the bell swinging and glowing; it emits two `Shockwave`s at the floor below it; the ripples dissipate after about 9 tiles; it only tolls while the player is in its room.
   - `World` damages the player with these waves like boss waves, and resets the bells on respawn.
2. **Pacing beat list and gated room graph as data** (`Core/LevelPlan.cs`), plus tests implementing the skill's `pacing_problems` and `goal_reachable` checks. The winch grants the grate. The boss arena is exempt from "can return to a save": death is its exit.
3. **Metrics audit test.** Build `NavGraph(allowDash: false)` keeping only edges with a rise ≤ 2 and pit width ≤ 3. Lamp, winch and arena must still be reachable from the start. The Brass Lung may need more.
4. **Autopilot.**
   - Jump any incoming `Shockwave` on its floor, not only in the boss fight.
   - Wait while a nearby ripple bell is winding up.
   - Then run the full headless playthrough plus `-autoplay` on the build.
5. **README and QA_REPORT.** Update the room list and add Ripple Walk.

## After the route: the "not AI slop" pass (planned, not started)

### Art direction (`create-game-assets` skill)

Fill `.claude/skills/create-game-assets/assets/art-direction-brief.md` into `docs/ART_DIRECTION.md`, plus an asset manifest. The decided direction:
- **Real low-res pixel art.**
  - Render the camera to a 512×288 RenderTexture (16 px per tile, point filter, no mipmaps).
  - Draw it scaled to the window: integer scale when it fills about 85% or more, otherwise fit.
  - Snap positions to 1/16 of a unit.
- **Strict palette of about 14 colours with roles:**

  | Role | Colours |
  |---|---|
  | Darks | ink `#0b0f14`, navy `#142028`, shadow `#050709` |
  | Stone | slate `#1f3340`, teal-grey `#2f4b55` |
  | Water | sea `#4b7378`, foam `#8fb3a8` |
  | The diver | bone `#e6e1cf` |
  | Brass (landmarks, interactables) | brass `#d79a3b`, ember `#f2c14e` |
  | Danger | coral `#e0584a`, rust `#8c4a2f` |
  | Jelly | violet `#6e5a9e`, lilac `#b7a6e0` |

- **Hand-made pixel sprites stored as ASCII pixel maps in code**, ideally a plain-C# file so a dotnet tool can export PNGs:

  | Sprite | Size (px) | Frames / details |
  |---|---|---|
  | Diver | 12×16 | idle, run ×4, jump, fall, hurt; separate hook sprite |
  | Crab | 24×14 | walk ×2, wind-up, lunge, recover |
  | Jelly | 16×18 | drift ×2, wind-up, spent |
  | Boss bell | 48×48 | plus a crack overlay for phase two |
  | Others | small | lamp lit/unlit, winch, grate, Brass Lung, spikes tile, ledge plank, autotiled rock with dithered edges |

- **Parallax backgrounds** with tower arches and windows, plus a water caustic band.
- **A custom 5×7 pixel font** (caps, digits, punctuation) for all UI. This replaces Arial, which is a big part of the slop look.
- **Pixel-art HUD:** helmet icons for health, a framed boss bar.
- **QA gates:** export the sprites to PNG, then run `scripts/asset_report.py --require-alpha --max-colors` and `scripts/build_preview_sheet.py` from the skill. Pillow is needed. Record provenance as "original, authored in code".

### Game feel (`game-feel` skill)

- **Fix the screen shake:** it currently uses `Random` per frame, which the skill says buzzes. Switch to trauma² times summed sines, decaying in real time.
- **Eased squash and stretch** (EaseOutBack), and hit flashes on the player as well as enemies.
- **Event tiers:** small, medium and large (the trauma, hit-stop and particle table is in the skill).
- **Toggles** in the pause menu for shake strength and reduced flashing.
- **Keep hit-stop under 0.15 s** and buffer input through it. That already works through `GameInput` latching.

## Known gaps to report to the user

- No human has played it. Feel, difficulty, audio and real keyboard and mouse use in the build are unverified.
- The playthrough is likely shorter than the brief's 10-15 minute target. The autopilot reaches the boss in about 45 s; humans will take longer.
- The test room isn't reachable in-game yet.
