# Start here: taking over Drowned Bell

Written 28 November 2026 for the next AI (or person) who continues this project. Read this first, then `docs/NEXT_TASKS.md` (the job list for the next AI), `README.md`
(how the game plays), `CHANGELOG.md` (what each version added) and `docs/HANDOFF_NOTES.md` (long history, newest last).

## 1. What this is

Drowned Bell is a 2D soulslike made in Unity 6.6 (6000.6.10f1, Built-In Render Pipeline, legacy Input Manager). A diver
climbs down a flooded bell tower. Four chapters are playable from title to credits:

| Chapter | Boss | Code |
|---|---|---|
| I Drowned Belfry (7 rooms) | The Undertoll | `Core/Boss.cs`, `RoomData.cs` |
| II Sluice Works (10 rooms) | Pump Warden | `Core/PumpMachinery.cs`, `SluiceWorks.cs` |
| III Sounding Archive (6 rooms) | Archivist of Depths | `Core/ArchiveFoes.cs`, `SoundingArchive.cs` |
| IV Tidal Engine (7 rooms) | Tidekeeper | `Core/EngineFoes.cs`, `TidalEngine.cs` |

Current version: **v0.8.2** (plus untested editor tooling, see section 7). The campaign is feature complete: it saves, has the
dawn ending, the memorial and the credits. Nothing is promised beyond polish and playtesting.

- Project folder: `D:\portofolio web\game dev porto\drowned-bell`
- GitHub: https://github.com/RubahAnomali/drowned-bell (public, owner RubahAnomali)
- The author's portfolio is a separate repo, `D:\portofolio web` (https://github.com/RubahAnomali/portfolio, site
  https://rubahanomali.vercel.app). The game is featured there but is **no longer playable in the browser**.

## 2. Rules from the owner (follow these)

1. **No AI attribution anywhere.** Commits, tags and releases must look like the owner's own work: no
   `Co-Authored-By`, no "Generated with" lines, no mention of Claude or any AI in commit messages, release notes or docs
   that get pushed. The git identity is already set to the owner.
2. **No version branches.** `main` is the only branch. Every finished update is a **tag** `vX.Y.Z` plus a **GitHub
   Release**. Old `v0.N` branches were converted to tags and deleted.
3. When a piece of work is finished, commit and push it without asking again (the owner asked for this), then check it.
4. Ask before anything destructive or outward-facing that is new (deleting branches, force pushes, publishing somewhere new).
5. The owner said once "no need to test run" for one specific change. That was for that change only. Normally run the tests.
6. Art is made by another AI/artist. Do not repaint it. Mechanics and wiring are the developer's job.
7. Do not claim difficulty or feel is verified: the only difficulty numbers come from scripted divers, never people.

## 3. How to release a version

```
# docs: add an entry at the top of CHANGELOG.md (newest first), update README / QA_REPORT if relevant
git add -A . ':!ProjectSettings/PackageManagerSettings.asset'     # that file is Unity noise, leave it out
git commit -m "vX.Y.Z: short summary"
git tag -a vX.Y.Z -m "Drowned Bell vX.Y.Z"
git push origin main refs/tags/vX.Y.Z
```

There is **no `gh` CLI** on this PC. Publish the release in the browser (the owner is signed in to GitHub in the app's
browser pane): open `https://github.com/RubahAnomali/drowned-bell/releases/new?tag=vX.Y.Z&title=Drowned%20Bell%20vX.Y.Z&body=...`
(URL-encoded one-line body), then click **Publish release**. Verify that `/releases/tag/vX.Y.Z` loads.
Line-ending warnings ("LF will be replaced by CRLF") are normal on this machine.

## 4. Layout

```
Assets/Scripts/Core/    all game rules, pure C#, NO UnityEngine. Tested headless.
Assets/Scripts/Game/    thin Unity glue: GameController (loop, HUD, screens), LevelView, ActorViews, EngineFoeView,
                        ArchiveFoeView, PumpMachineView, AudioSynth (all sound is synthesised), CampaignArt (loads PNGs),
                        AutoPlay (QA bot), AutoTour (screenshot tour), SaveStore
Assets/Editor/          BuildTools.cs (menu + batch builds)
Assets/Resources/Art/   finished art PNGs used at runtime (point filter, uncompressed, via the .meta files)
art/                    the art deliverables as received (Campaign, TidalEngine incl. .aseprite sources, Rooms)
tests/Core.Tests/       the headless suite (net9.0)
tools/                  python generators for the room maps (rooms.py, archive_rooms.py, engine_rooms.py, ...)
docs/                   BOSS_DESIGN, CAMPAIGN_PLAN, ART_DIRECTION, QA_REPORT, HANDOFF_NOTES, qa-*.txt, screenshots
```

Key Core files: `World.cs` (fixed 60 Hz step, events, snapshot/restore), `Player.cs`, `Physics.cs`, `Level.cs` (parses the
ASCII maps; marker characters such as `Y` latch, `X` wheel, `S` pad, `N` knight, `U` Tidekeeper, `V` vent, `W` ripple outlet,
`F` stair lift, `A` rest lamp, `E` exit), `SaveGame.cs` (versioned key=value save), `Dice.cs` (seeded random, shuffle bags).

## 5. Commands

Run the headless suite (about 20 s). It must say `all checks passed` (256 checks at v0.8.2):

```powershell
cd "D:\portofolio web\game dev porto\drowned-bell\tests\Core.Tests"
dotnet run -c Release
```

Useful environment switches for that command: `DIFFICULTY_SEEDS=1` (8-seed boss table: reader / greedy / idle divers),
`DIFFICULTY_TABLE=1`, `ENGINE_TRACE=1`, `PAD_TRACE=1` (print the scripted routes). Other traces live in `tests/Core.Tests`.

Build the Windows player from the command line (Unity Hub must be open and signed in with an active Personal licence,
otherwise Unity exits with code 198; the editor must NOT be open on the same project):

```powershell
& "D:\game\unity\unity installs\6000.6.10f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\portofolio web\game dev porto\drowned-bell" -buildTarget Win64 -executeMethod BuildTools.BuildWindows -quit -logFile build.log
```

Output: `Builds/Windows/DrownedBell.exe` (starts full screen, cursor hidden). `BuildTools.BuildWebGL` builds a WebGL
version into `Builds/WebGL` (kept, but not hosted any more). `Builds/` is git-ignored.

QA bot, plays all four chapters through the real build and writes `report.txt` plus screenshots (about 9 minutes):

```powershell
.\Builds\Windows\DrownedBell.exe -autoplay "C:\some\empty\folder" -retries 2 -seed 777
```

Check `runtime errors: 0` and `result: VICTORY` in the report. `-seed N` fixes the boss attack order. `-autoshot <dir>` is the
older screenshot tour of chapter I. Both modes stay windowed and never touch the player's save.

## 6. Design rules that must not break

- **Core stays free of UnityEngine** and deterministic: time advances only through `World.Update(FixedDt, input)` at 60 Hz.
  The drawing interpolates between steps (`Body.Prev`, `Interp.Of`). Anything that moves on screen must be drawn through
  `Interp.Of`. Core rules never read `Prev`.
- **Randomness** only through `Dice` / `Bag` seeded from `World.Seed` (saved; `ReseedBosses` runs on construction and on each
  respawn with the death count). Never use `UnityEngine.Random` or `System.Random` in Core.
- **Boss fairness floors** (checked by `BossDesignChecks.cs`): telegraph at least 0.45 s, punish window at least 0.35 s, a
  heavy hit costs 2 masks, bosses heal the diver every 7 hook hits. If you change a boss, keep these and rerun
  `DIFFICULTY_SEEDS=1`. Tuning numbers: `BossTuning` (Undertoll; `Main.unity` must match, a test checks it),
  `PumpMachinery.cs`, `ArchiveFoes.cs`, `EngineFoes.cs`.
- **Saves are versioned and additive.** New fields must be optional so old saves still load (tests cover v0.3 and older).
- **Maps are generated.** Edit `tools/*_rooms.py` and rerun it; do not hand edit `*Data.cs`. Keep the critical path inside the
  measured limits (climbs of at most 2 tiles, gaps of at most 3). A test enforces this.
- **Engine plates** (the room paintings) are drawn with fixed pixel shifts that were measured from the art
  (`PlateShift` in `LevelView.cs`: Dock, Cradle, Condenser, Gate, Nave 3 px high, Night Watch 4 px low). If the art changes, remeasure.
- The art atlases and their cell layouts are described in `art/Campaign/README.md` and `art/TidalEngine/Art guide.md`.
  Engine foe cels are 128 x 96, face left, soles at pixel row 85; attacks are 12 columns: 0-4 anticipation, 5-8 strike, 9-11 reset.

## 7. Current state and loose ends

Done and verified (28 Nov 2026): 256 headless checks, 8-seed difficulty table (Warden, Archivist and Tidekeeper fall to the
perfect scripted diver on every seed with no deaths; the Undertoll only 5 of 8 within 7 minutes), and the bot cleared all four
chapters in the real Windows build on seeds 1234 and 777 with 0 runtime errors. See `docs/QA_REPORT.md` and `docs/qa-engine-standalone.txt`.

Not done or not verified:

- **Played by hand, but not measured.** The owner has played the game and had friends play it, so feel and difficulty have been judged by real players.
  No formal data was recorded (completion times, where people died, how long each boss took), so the 55 to 80 minute target and the
  difficulty numbers are still only scripted guesses. A short written playtest with real numbers is the next most valuable step.
- `Assets/Editor/PreviewTools.cs` (menu **Drowned Bell > Preview**, draws a chapter in the Scene view without pressing Play) was
  written while the owner had Unity open, so it was **never compiled or run and is not committed**. Compile it, try each
  entry, fix errors, then commit it (or delete it).
- The owner is looking at the project in the Unity editor (Hub shows platform "Web" because the last batch build was WebGL;
  switch with File > Build Profiles > Windows). Unity shows two harmless warnings: Built-In Render Pipeline deprecated, and
  Input Manager deprecated.
- The Windows zip `Builds/Drowned-Bell-Archive-Art-Windows-x64.zip` is an old art-review package. Make a new one with
  `python tools/package_windows.py --output <zip>` after a Windows build if a download is needed (none is attached to the releases).
- Small art gaps: the painted record note and lamps stay on the Engine plates after use (only glows and thrown latches are
  drawn on top); room banners can overlap painted details such as the Night Watch clock. Condenser hazard art
  (`IV_Condenser_hazards.png`) and `IV_Dawn_sky.png` are imported but not used separately.
- The Undertoll is the hardest boss for the scripted diver; the scripted diver itself sometimes ends a recovery inside the bell.
- Older docs (`HANDOFF_NOTES.md`, older QA entries, older release notes) still mention the browser edition. That is history.
- The GitHub profile README (repo `RubahAnomali/RubahAnomali`) was not updated for v0.8.x.

Ideas the owner has not asked for (do not start without asking): URP migration, gamepad polish, more rooms, a human-readable
difficulty setting.

## 8. Working tips for this machine

- Windows, PowerShell and Git Bash. Bash heredocs with apostrophes break: write Python helper scripts to a file instead.
  Some docs (for example `docs/QA_REPORT.md`) are cp1252, not UTF-8: read and write them with that encoding.
- No PIL on this PC. For image work use `ffmpeg` (raw frames) plus numpy.
- Screenshots from the bot are 1600 x 900. Room screenshots are taken at the moment of the room change, so the camera may still
  be easing from the previous room; that is normal.
- Do not open the game window with computer-use (the owner declined that). Use `-autoplay` and read the screenshots.
- Unity batch builds fail with exit 198 when Unity Hub is closed or the licence expired: ask the owner to open Hub.
