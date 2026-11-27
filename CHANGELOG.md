# Versions

- `main` is the current version. New work lands here.
- Each release gets its own branch, `v0.1`, `v0.2` and so on, cut from `main` when that version is done.
  A version branch is a frozen snapshot: nothing new is committed to it.
- To play or build an old version: `git switch v0.1`. Come back with `git switch main`.

## v0.8.1 (full screen)

- The Windows build starts full screen (borderless window) and the mouse cursor is hidden. The QA modes (`-autoplay`, `-autoshot`) stay windowed.
- Full check: 256 headless checks, the 8-seed difficulty table and a four-chapter bot run on seed 777 all pass with 0 runtime errors.

## v0.8 (the Tidal Engine, chapter IV)

- Chapter IV is playable: seven rooms on the finished room plates. Service Dock (lamp and a model of the reversal
  mechanism), Flywheel Cradle (a Ballast Knight and the first governor latch), Condenser Bridge (vents, floor waves and
  the second latch), the optional Night Watch under the bridge (three spring pads and the third keeper record),
  Governor Stair (a lift back to the dock and a last lamp), Sea Gate and the Morning Nave. Walking out into the
  morning ends the campaign.
- New foes: the Ballast Knight (brace, strike, reset; its hinge is only open while it resets) and the Tidekeeper, the
  last boss: hammer, floor wave, pressure lane and echo in random order, the hinge only open during the reset, phrases
  of three attacks with a resting beat below half health. It is hardened like the other bosses and checked by the
  same difficulty harness.
- The campaign art is in: continue screen with the lamp register and chapter seals, "dive saved" notices, the Sluice
  portrait, gauge and pipe-diagram inspections, machinery sprites, the completion screen with the memorial of the three
  keepers found, the credits, and the Engine room plates, Knight and Tidekeeper cels.
- Saves carry the Engine state (latches, record, Tidekeeper down, tide reversed, rest lamp); finishing the Engine
  clears the save and the next dive starts fresh. Saves from v0.7 and earlier still load.
- The autoplay bot now plays all four chapters, takes screenshots of the Engine and the ending, and checks the
  dive-again run starts clean.

## v0.7 (random boss fights)

- The three bosses now pick their attacks at random, so no fight shows a fixed pattern and every new save plays
  the bosses in a different order. A run seed is chosen at the start of a new game and kept in the save; each attempt
  at a boss mixes in the death count, so a retry differs too.
- Choices come from shuffle bags (random order, but never a long drought or three of a kind in a row) and the
  fairness rules do not move: telegraph and punish times are fixed, and a rush needs the diver to be 6+ tiles away.
- Undertoll: slam or rush, delayed slams, rain and chained slams in random order, with a different pause every time.
  Warden: pulse count, which pipe vents, which pulse is double, tracked jets and quick or delayed sweeps.
  Archivist: real or decoy, decoy side and distance, chained dives and hover time.
- `-seed N` replays a fight order; the autoplay report prints the seed it played.
- Saves from before v0.7 still load (they get a seed the first time they are continued).
- Checked over eight seeds: the Warden and Archivist fall to a flawless scripted diver every time, the Undertoll is the hardest, and swinging without dodging is punished everywhere (docs/BOSS_DESIGN.md).

## v0.6 (Archive art and smooth movement)

- The Sounding Archive has its final pixel art (from the art pass): bookshelf rooms with green-glass niches, the
  Ink Moth, Tuning Widow and Archivist with its swinging clapper, echo drums, keeper ledger, chute and key door,
  a chapter plate for the title and ending, and redrawn Warden poses.
- Smoother movement: the game steps at 60 Hz while the screen draws at 144, 75 or 60 Hz, which made motion judder.
  Every sprite and the camera are now drawn between the last two steps, the camera look-ahead eases by time instead
  of by frame, and the Windows build uses vsync. Game rules, jump distances and the level design are unchanged.
  Measured in the real build at 144 Hz: before, the diver stood still on 58% of frames and jumped up to 5 px in
  one; now it moves 0 to 2 px per frame, and the only repeated frames are the pixel grid's own 9%.

## v0.5 (harder bosses)

- All three bosses hardened to Dark Souls difficulty, and kept fair: heavy hits (a boss body hit costs 2 of 5 masks),
  delayed swings, combos, tracked attacks, no safe range and no safe hugging. See docs/BOSS_DESIGN.md.
- Undertoll: 26 hits, shorter beats, delayed slams, a second wave pair and slam-out-of-rush combos in phase two.
- Pump Warden: 22 hits, three or four pulses in a row, a tracked jet, double-pipe jets, and a sweep that alternates a
  quick and a delayed brace and starts at its own centre.
- Archivist: 20 hits, ripples after every landing, echo pairs and chained dives in phase two, 2-mask dives.
- Every 7 hook hits on a boss mend one mask (never from ordinary enemies).
- 26 new headless checks pin the hard parts, the fairness rules (telegraph at least 0.45 s, punish window at least
  0.35 s) and a difficulty envelope: a flawless reader wins, a greedy or idle diver is punished.

## v0.4 (Sounding Archive mechanics, placeholder art)

- Chapter III, the Sounding Archive, follows the Pump Warden: Receiving Desk (green lamp, harmless practice
  drum), Silt Stacks, Listening Cells, the optional Map Loft (a shelf climb to the second keeper record),
  Return Chute (counterweight shortcut ridden with Up, live echo drum, drum-key door) and the Recording Choir.
- New foes: the Ink Moth (marks a spot, dives, lodges), the Tuning Widow (two ripples 0.75 s apart, then a
  punish window) and the Archivist of Depths boss (two ink silhouettes, only the real one swings its clapper;
  in phase two each landing sends a delayed ripple pair).
- Echo drums: ripple bells that ring twice.
- Saves cover the Archive; saves from v0.3 still load.
- The Archive's visuals are placeholders; docs/ARCHIVE_ART_BRIEF.md lists the art to make.

## v0.3 (Sluice Works and save/continue)

- Chapter II, the Sluice Works: seven rooms after the Undertoll (Intake Dock, Bleeder Gallery, Valve Hall,
  Impeller Well, Sediment Trap, Counterweight Walk, Duty Pump) with pressure vents, a valve gate, a bypass,
  a counterweight lift checkpoint, an optional keeper record and the pump key.
- New enemies: Piston Limpet and Chain Porter, and the Pump Warden boss.
- Illustrated title and ending screens.
- Save and continue: one save slot, written at lamps, the lift, pickups, opened shortcuts, boss clears
  and respawns. The title offers Continue (Enter) or a new game (N). The browser edition keeps the save too.

## v0.2 (playable in the browser)

- Web build (Unity WebGL) with its own full-window page, hosted on the portfolio site.
- Browser-safe keys: I opens the inspector, 1 and 2 change shake and flashing while paused
  (F1, F5 and F6 belong to the browser there). Quit is hidden in the browser.
- The frame rate follows the display in the browser.
- The "lamp lit, grate open" line no longer wraps onto a second line.

## v0.1 (first playable demo)

- Seven rooms in one connected area: Belfry Landing, Flooded Nave, Ripple Walk, Gutter Shaft,
  Choir Gallery, Bell Ropes and The Undertoll.
- Diver with run, variable jump, dash, and a three-direction hook swing with pogo.
- Brine Crab, Knell Jelly and ripple bells. The Undertoll boss with slam, rush and rain attacks
  and a second phase.
- Tide lamp checkpoint, Brass Lung health upgrade, winch-opened shortcut, ending screen.
- Keyboard, mouse aim, and gamepad. Shake strength and reduced flashing options.
- Pixel art and sound made in code; 512x288 pixel-perfect rendering.
- Movement and combat test room with live jump and dash measurements.
- Headless test suite, autoplay bot and screenshot tour for QA.
