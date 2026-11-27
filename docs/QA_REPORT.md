# Drowned Bell QA report

## The Tidal Engine and the campaign art (v0.8), 27 November 2026

All 256 headless checks pass, including the new "Tidal Engine" group: the map (seven rooms, two latches, three pads, a wheel, a lift, a last lamp, a record and an exit; the required route never climbs more than two tiles at once or crosses a gap wider than three), the hatch to the Night Watch (a shelf every two tiles), the three spring pads, the Tidekeeper (telegraphs 0.70 s or more, 0.55 s in phase two, a reset of 0.80 s, hammer and lane only on the side it faces, all four attacks in random order and never three alike, the hinge only takes hits while it resets), both latches, the lift, the dock model, the rest lamp, the Knight's hinge, saves (an Engine save restores every state and wakes at the right lamp) and an input-only route through the whole chapter (95 s from a fresh start, no deaths). The difficulty table has a Tidekeeper row: the flawless reader wins all 8 seeds with no deaths (62 to 65 s), the greedy diver dies at 11 to 12 s and never wins.

Unity built Windows with 0 errors. In the real build the bot played all four chapters in one run on seed 1234 (docs/qa-engine-standalone.txt): victory, 2 planned boss deaths plus one unplanned, the Night Watch crossed by the pad route to the record, both governor latches, the Tidekeeper beaten, the tide reversed, the completion screen with all three keepers on the memorial (8:44, 3 deaths), the credits, and a clean "dive again" run. 0 runtime errors, 144 fps, and the same smooth drawing as before. Screenshots of the plates, a Tidekeeper attack in each phase, the Night Watch record and the ending are docs/screenshots/engine_*.png. Not checked by hand: the Engine with a keyboard, how hard the Tidekeeper is for a person (the reader is a script with perfect knowledge), and the browser build before it was rebuilt. Known small gaps: the painted record note and lamps stay on the plate after being used (only a glow and a thrown latch are drawn on top), and the room banner can sit over the Night Watch clock.

## Random boss fights (v0.7), 27 November 2026

All 214 headless checks pass. They were first written and compiled without being run (at the owner's request), then run afterwards: two failed, both because they assumed the old fixed order. "It uses both the slam and the rush" now only asserts the slam (a diver who stays close never gets a rush, which needs the diver 6+ tiles away; a separate test forces the rush), and the four-retries check allows the scripted diver one to three unplanned deaths on top of its four planned ones.

Difficulty over eight different run seeds (see docs/BOSS_DESIGN.md): the Pump Warden and the Archivist fall to the flawless reader on every seed with no deaths, the Undertoll on every seed but only 5 of 8 within seven minutes (the other three needed 4, 4 and 8 deaths and 583 to 809 s), and a diver who swings without dodging dies within 22 s on every seed and never wins. The reader is a script with perfect knowledge, so this shows the bosses are beatable in any order and punish greed; it does not predict how many tries a human needs. In the real Windows build the bot played all three chapters twice on different random seeds and won both times with zero runtime errors (one run needed two unplanned Undertoll deaths on top of its planned ones, the other none). A known weakness in the scripted diver (not the boss): against the Undertoll it sometimes ends a recovery standing inside the bell or gets caught by a chained slam.

## Archive art and smooth movement (v0.6), 27 November 2026

All headless checks passed, including 12 new "Smoothness" checks: Prev and Lerp are right, teleports and enemy resets do not streak, and replaying a steady run at 144, 75 and 60 Hz (with and without +-15% frame-time jitter) draws even motion (0.0000 tiles of spread per frame at 144 and 75 Hz, against 0.137 for the old drawing). Unity built Windows with 0 errors and 0 warnings. In the real build at vsync 144 Hz, 1855 steady-run frames moved the drawn diver 0 to 2 px each (old drawing: 0 to 5 px, standing still on 58% of frames). The bot played the Belfry, Sluice Works and Sounding Archive with only its planned deaths and zero runtime errors, and its Archive screenshots show the new art (shelf rooms, moth, widow, Archivist with only the real clapper swinging). Not checked by hand: how the smoothing feels on another monitor, and the browser build (not rebuilt yet).

## Harder bosses (v0.5), 27 November 2026

All headless checks passed, including 26 new "Boss design" checks (see docs/BOSS_DESIGN.md): the fairness floor (telegraph at least 0.45 s, punish window at least 0.35 s), the hard parts (2-mask boss hits, delayed slams and sweeps, second wave pair, slam chained out of a rush, tracked and double-pipe jets, boss-only healing) and a difficulty envelope: a flawless reader beats each boss in 78 to 100 s, a diver who swings without dodging dies within 22 s and never wins in 3 tries, and a diver who stands still dies. Unity built Windows with 0 errors. The Windows bot played the Belfry, Sluice Works and Sounding Archive in one run with only its planned deaths (one per earlier boss) and zero runtime errors. The reader is a script with perfect knowledge, so these numbers show the bosses are beatable and punish greed; they do not predict how many tries a human needs. Not checked by hand: the real feel of any boss, and the browser build (not rebuilt).

## Sounding Archive mechanics (v0.4), 27 November 2026

All headless checks passed, including 42 new "Sounding Archive" checks: map shape (required route is level floor; the optional loft climb is a shelf every two tiles), practice and live echo drums, moth mark/dive/lodge timing and the punish window, widow spread, ring and echo timing, immunity outside its sag and a jump-clear, the Archivist's two marks five tiles apart with the real one alternating, immunity in the air, phase-two delayed ripples, defeat unsealing the choir and staying down after a death, chute rides and the drum-key door, an input-only route through all six rooms (49 s, no deaths), a lamp-to-Archivist retry of 4.2 s via the chute, and saves (Works victory saves the Archive start; a mid-Archive save restores and finishes; v0.3 saves still load). Unity built Windows with 0 errors and 0 warnings. The Windows bot played the Belfry, Sluice Works and Sounding Archive in one run (one planned death per earlier boss) to the end with zero runtime errors and captured every Archive beat. Visuals are placeholders. Not checked by hand: the Archive with a keyboard, its difficulty for a person, and the browser build (not rebuilt for v0.4).

## Save and continue (v0.3), 27 November 2026

All headless checks passed, including 16 new "Save and continue" checks: exact read-back; files cut off mid-write, other versions, damaged values and nonsense files are ignored; Windows line endings still read; a Belfry save keeps the lamp, grate and Brass Lung and wakes at the lamp; a save after the Undertoll continues at the Works entrance; a save after the pump key restores every station, wakes at the lift, keeps the Warden down after a death and can still reach the archive hatch. Unity built Windows and WebGL with 0 errors and 0 warnings. The Windows bot played both chapters to the archive hatch (one planned death per boss) with zero runtime errors and wrote no save file. In the browser build: lighting the lamp saved, a page reload showed Continue with "Drowned Belfry 0:01", Continue woke the diver at the lit lamp, N started a fresh run and a later reload showed no save. No console errors. Not checked by hand: Continue on Windows after a real quit, and a save made inside the Sluice Works in a real build (covered only by the headless checks).

## Sluice Works campaign preview, 27 November 2026

106 headless checks passed, including pressure timing and damage, valve gating, checkpoint and upgrade persistence, and the input-only intake route. Unity built the Windows x64 player. The standalone pilot launched from a fresh ZIP extraction, cleared the Undertoll at 122.0 seconds, entered all three Sluice rooms, opened the valve, reached the outlet at 134.8 seconds of cumulative game time and restarted cleanly with zero runtime errors. The first victory line in docs/qa-sluice-standalone.txt times the Belfry only; the Sluice Works line gives the cumulative time. Human pacing, physical controls and another PC remain unverified. The public browser build predates this preview.

## Windows art release, 27 November 2026

Unity built the Windows x64 player, and all 97 headless checks passed. The built-in pilot played a clean extraction through one ending and clean restart in 147.7 seconds of game time, with one deliberate boss death, a 5.4-second respawn-to-arena return and zero runtime errors. Pause froze the game and the 1280 by 720 resize worked. All 156 final archive entries matched the tested extraction. Raw results are in docs/qa-windows-art-release.txt. Hands-on controls, first-time human pacing, and compatibility on a second Windows PC remain unverified. The release ZIP is local and the public browser build was not updated.

## Current release candidate — 26 November 2026

**Needs verification.** Both fresh standalone processes reached the ending exactly once, with zero runtime errors. The input-only pilot collected the lamp and Brass Lung, struck the winch, climbed its shortcut, and fought the Undertoll. Restart restored a clean world. First-time human navigation and hands-on controls remain essential acceptance checks.

Extract `Builds/DrownedBell-Windows-QA.zip` and run `DrownedBell.exe`, or run `Builds/QA/DrownedBell.exe` locally. Enter or click starts a fresh run. Unity 6000.6.10f1 built the enabled Main scene. The earlier build in `Builds/Windows` predates the fixes. All packaged files matched the tested build byte-for-byte.

| Standalone test | Result |
|---|---|
| First successful automated run | 122.0 s game time, 132 s total QA wall time, no deaths. Arena reached at 63.4 s. |
| Three deliberate boss deaths | 199.2 s game time to victory. Each lamp-to-arena return took 5.4 s, plus the 1.6 s death fade. |
| UI, pause and restart | Title, route, pause, victory and restart captured. Simulation froze while paused. Tested 1600×900 and 1280×720. |
| Combat performance and sound | Approximately 144 fps average, 7 ms longest sampled boss frame on this machine. Nonzero audio output in both runs. |
| Project checks | 97 headless assertions passed. Earlier graphics-backed editor Play Mode verified startup, focus callbacks, respawn camera and movement. |

### Fixed defects

- **Boss rain spread.** Main saved four drips, leaving a gap at the targeted player position. Changed the saved count to five. The headless scene check reproduced the mismatch before the fix and now passes across all four tuning groups. The compiled scene contains the five-drip tuning block.

- **Medium — buffered mouse attack.** Click upward late in hook recovery and release. The next swing went sideways. The attack buffer now retains its click direction. The new regression failed against the old controller and passed after the fix.
- **Minor — bell audio across runs.** Beating the boss while muted skipped the silence state. Restarting after an audible defeat kept the distant bell silent. Defeat now updates state while muted and every fresh run resets the toll timer. Binary probes reproduced the old state and confirmed the new six-second timer and retained mute setting.

### Remaining checks, in impact order

1. Physical keyboard and mouse handling, first-time route discovery, attack-art alignment by feel, and human completion time remain unverified. The route-aware pilot finishes well below the intended 10–15 minutes.
2. Audio balance by ear, actual OS focus loss/regain, and Q through physical input remain unverified. The focus callback paused ordinary play and kept it paused on regain.
3. An editor-only batch diagnostic crashed in Unity sprite rendering when entering Play Mode with `-batchmode -nographics`. The same scene passed with a graphics device. The graphics-backed editor also logged a Unity Search index exception during startup. Neither standalone run logged an error.

Raw playthrough reports are in `docs/qa-standalone-retries-0.txt` and `docs/qa-standalone-retries-3.txt`.

## Earlier pixel-art QA

**Earlier status.** Unity batch access and the seven-room Windows route are verified. Human play, standalone keyboard and mouse handling, and audio balance are pending.

## How the build was played

`Builds/Windows/DrownedBell.exe -autoplay <folder> -retries 2` runs the normal game with full rendering. Each simulation step takes its input from `Core/Autopilot.cs` instead of the keyboard.

- **Inputs only:** the autopilot never moves the diver directly. It walks moves recorded from the real controller (`Core/NavGraph.cs`) and fights by reading each enemy's state.
- **Report:** it saves screenshots at every milestone and writes `report.txt`. The final run's report is in `docs/autoplay_report.txt`.

## Final pixel-art Windows playthrough

| Check | Result |
|---|---|
| Title to ending | Victory screen reached, exactly one victory event |
| Route | Lamp 0:00.5, Ripple Walk crossed, Brass Lung 0:47.1, winch 0:49.6, climbed the shortcut to the lamp 0:58.2, arena 1:03.4 |
| Boss retries (2 planned deaths) | Respawn to arena 5.4 s each, plus a 1.6 s death fade, via the shortcut |
| Total | 178.2 s of game time with 2 deaths (192 s real) |
| Pause | Simulation frozen while paused, resumes |
| Window 1280x720 | Resize confirmed at 16:9 |
| Dive again from the ending | Clean new run: lamp unlit, grate closed, Brass Lung back, all 4 enemies alive, boss dormant at full health |
| Runtime errors / exceptions | 0 |

## Pixel-art build and test room

- Unity batch build succeeds with the 512Ã—288 pixel renderer, pixel font, new sprite families and letterboxed mouse aim mapping.
- The standalone test-room automation records one hit after one swing. Its HUD reports a 3.26-tile jump rise, 5.33-tile running jump, 3.21-tile ground dash, and 7.78-tile running jump with dash.
- The final test-room run logged zero errors and saved `docs/screenshots/pixel_test_room.png`.
- Run `DrownedBell.exe -testroom -autoshot <folder>` to capture the dummy check. Launch the player normally with those arguments if collecting PNGs. Windows can suppress screen capture for a process started with a hidden window even though its simulation runs.
- The authored ripple bell and eight actor or equipment exports passed the asset skill's RGBA alpha and palette limits. Their dimensions matched the manifest.
- The screenshot tour captured 23 frames without logged errors. Representative frames are in `docs/screenshots/pixel_ripple_walk.png` and `docs/screenshots/pixel_undertoll.png`.

**Headless suite** (`tests/Core.Tests`, `dotnet run -c Release`): every check passes. It covers:

- movement, combat, enemies and the boss;
- death, respawn and persistence;
- a dash-free critical path with climbs up to two tiles and level gaps up to three;
- pacing, winch gating, return paths and the training shockwave used in Ripple Walk;
- a full autopilot playthrough;
- the edge cases below.

## Defects found and fixed in earlier passes

| # | Severity | Trigger | Expected | Observed | Fix / verified by |
|---|---|---|---|---|---|
| 1 | Blocker | Bell killed by anything other than the diver's swing | Ending plays | No ending | Any boss death starts the ending; regression test, screenshot tour |
| 2 | Major | Diver dies in the same frame as the bell | Run ends once | Shared death/victory timer could be overwritten | Separate victory timer; regression test |
| 3 | Major (design) | Drip rain | Must move to dodge | 4 drips centred on the diver left the diver's own spot safe | 5 drips, the middle one on the diver; autopilot must step into a gap |
| 4 | Major | Slam: bell homed in until landing | Time to react | Shockwave spawned ~0.5 tiles from the diver | Bell follows only for the first 70% of its wind-up, at a capped speed; regression test |
| 5 | Minor | No quit option | Exit from the game | Only Alt+F4 | Q quits on the title, pause and victory screens |
| 6 | Minor | Window loses focus | Game waits | Would resume instantly on refocus | Losing focus opens the pause menu |
| 7 | Minor | Readability | Clear HUD and tells | Boss bar over the arena floor, bell too dark, crab low contrast, faint drip warnings, door read as green blocks | All adjusted; checked in screenshots |
| 8 | Minor | Lamp marker one tile above the floor | Respawn on the floor | Diver respawned mid-air | Lamp moved onto the floor; regression test |

## Open issues and unverified scenarios (by impact)

1. **Not played by a person.**
   - The keyboard path (`GameInput`) was not driven in the standalone player: the autopilot feeds the game loop directly. It compiles and is simple, but is unverified there.
   - Feel and difficulty are unverified.
2. **Length.** Autoplay reaches the boss in about a minute. First-time human duration against the 10â€“15 minute target is unmeasured.
3. **Audio not heard.** QA runs were muted at night. Sounds are synthesised and wired to events, but levels and mix are unchecked.
4. **Focus loss and regain** in the standalone was not exercised. The code path is a simple callback.
5. Fullscreen and gamepad were not tested.
