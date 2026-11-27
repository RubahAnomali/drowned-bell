# Next tasks for the next AI

Read `docs/START_HERE.md` first (rules, commands, design rules). This file is the job list. Work through it in order,
one task at a time, and finish each task completely (code, tests, docs, release) before starting the next.
Do not start anything that is not on this list without asking the owner.

## The standard for "done" (applies to every task)

A task is done only when all of this is true. If you could not do a step, say so in your report. Never write "verified"
for something you did not run.

1. The change is as small as it can be and follows the rules in `START_HERE.md` section 6.
2. `cd tests/Core.Tests && dotnet run -c Release` prints `all checks passed`. A new rule or bug fix has a new check that fails without it.
3. If any file under `Assets/` changed: a Windows batch build succeeds with 0 `error CS` lines in the log (the Unity editor must be
   closed first, or the project is locked), and the bot run (`-autoplay`, see `START_HERE.md`) ends with `result: VICTORY` and
   `runtime errors: 0`. Look at a few of its screenshots yourself.
4. `CHANGELOG.md` has a new entry on top. README, `docs/QA_REPORT.md` and `docs/START_HERE.md` are updated if the facts in them changed.
5. Commit under the owner's identity with no AI attribution, tag `vX.Y.Z`, push `main` and the tag, publish the GitHub Release
   (`START_HERE.md` section 3), and confirm the release page loads.
6. Report to the owner in plain language: what changed, what you checked and how, what you did not check.

## Task 1. Make the editor preview work, or remove it (small)

`Assets/Editor/PreviewTools.cs` adds **Drowned Bell > Preview** (draws a chapter in the Scene view without pressing Play). It
was written while the owner had Unity open, so it was never compiled and is not committed.

- Close the editor, run the Windows batch build (it compiles Editor scripts too) and fix any errors.
- Open the project in the editor, choose each of the four preview entries, and check the rooms appear and **Clear Preview** removes them.
  The preview object must not be saved into `Main.unity` (hide flags `DontSave`); check `git diff Assets/Scenes/Main.unity` is empty after.
- Done when it works and is committed, or when you delete the file and tell the owner. Release as a patch version.

## Task 2. Package a Windows download (small)

There is no downloadable game. The only zip, `Builds/Drowned-Bell-Archive-Art-Windows-x64.zip`, is old.

- Build Windows, run `python tools/package_windows.py --output Builds/Drowned-Bell-Windows-x64.zip`, unzip it in an empty folder and
  start the exe (it must start full screen, show the title, and start a new dive with Enter). Do not use the computer-use tool on the
  game window (the owner declined); instead run the unzipped exe with `-autoplay` and read the report.
- Attach the zip to the GitHub Release for that version (the release form has an attach-files box; the file is large, so use the
  browser upload, not a URL parameter). Update the README **Play** section with the release link.
- Done when a fresh download from the release page was unzipped and the bot beat the game from it.

## Task 3. Finish the Engine plate polish (medium)

Known gaps, all in `Assets/Scripts/Game/LevelView.cs` (`BuildEnginePlate`, `BuildEngineFixtures`, `TickEngine`):

1. The record note in the Night Watch stays painted after the keeper record is taken. Cover it with a patch cut from the same plate
   (a few background pixels copied from beside the note via `CampaignArt.Cell`), shown when `world.EngineRecord` is true.
2. The dock and rest lamps are painted lit even before they are lit. Draw a small dark cover or dim overlay until `LampLit` / `RestLit`.
3. The room banner text can overlap painted details (the Night Watch clock). Move the banner higher for the Engine or add a dark backing strip.
4. `IV_Condenser_hazards.png` (separate bridge hazard art) is imported but unused. Decide with the owner whether the floor-wave outlet and the vents should use it.

Check each fix with the bot screenshots (`engine_room_*`, `engine_watch_record`) and compare before and after. Keep the plate pixel shifts.

## Task 4. Keep the scripted tests honest about the Undertoll (medium)

In the 8-seed table the scripted "reader" beats the Undertoll within 7 minutes on only 5 of 8 seeds, and it sometimes ends a recovery
inside the bell (a bot weakness, not necessarily a boss problem). Investigate with `DIFFICULTY_SEEDS=1` and `BossTrace`:

- Find out whether the three slow seeds are the bot misplaying or a genuinely unfair order (for example a delayed slam straight after a rush).
- If the boss is unfair, fix the shuffle rules in `Boss.cs` so no order breaks the fairness floors, and add a check that fails on that order.
- If it is the bot, improve the reader in `tests/Core.Tests/BossDifficulty.cs` and the Undertoll bot in `Assets/Scripts/Core`/`AutoPlay`.
- Do not make the boss easier just to make the table pretty. The owner asked for "Dark Souls" level difficulty. Report the new table honestly.

## Task 5. Prepare a human playtest (medium, needs the owner)

Nobody has played the whole game by hand, and the difficulty and pacing numbers are only scripted guesses. You cannot do this yourself,
but you can make it easy:

- Write `docs/PLAYTEST.md`: a one-page sheet for a tester (how to start, controls, what to note per chapter: where they died, where they
  were lost, how long each boss took, anything unclear), plus a table to fill in.
- Add an optional, off-by-default log to the game (for example `-playlog <file>` writing time, room, deaths and boss attempts as plain
  text) so results do not rely on memory. It must not write anything unless the flag is given, and must not change game rules or saves.
- Ask the owner to run it, then summarise the results into concrete tuning changes with the numbers to move (see `START_HERE.md` section 6).

## Task 6. Housekeeping (small)

- The GitHub profile README (repo `RubahAnomali/RubahAnomali`) lists Drowned Bell with old facts. Update it to four chapters, no browser play,
  current version. It is an outward-facing change on the owner's public profile: show the owner the exact text before pushing.
- Old docs and release notes still mention the browser edition. Add one line at the top of `docs/HANDOFF_NOTES.md` saying that history
  before v0.8.2 mentions browser play, which was removed. Do not rewrite history.
- `ProjectSettings/PackageManagerSettings.asset` is regenerated by Unity and is deliberately never committed. Leave it.
- Run `/ponytail-audit` style review (or a careful read) of `Assets/Scripts/Game/GameController.cs`, which has grown large, and list findings
  for the owner. Do not refactor it without asking.

## Things you must not do

- Do not add AI attribution, co-author lines or "generated" footers anywhere that is pushed.
- Do not create branches or force-push. Do not delete tags or releases.
- Do not change art files in `art/` or `Assets/Resources/Art/` (another artist owns them). Ask for changes.
- Do not claim the game is balanced, fun or hand-tested. State only what you ran.
- Do not leave the work half done at the end of a session: either finish a task or write exactly where you stopped in `docs/HANDOFF_NOTES.md`.
