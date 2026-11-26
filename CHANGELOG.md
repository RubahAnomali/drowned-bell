# Versions

- `main` is the current version. New work lands here.
- Each release gets its own branch, `v0.1`, `v0.2` and so on, cut from `main` when that version is done.
  A version branch is a frozen snapshot: nothing new is committed to it.
- To play or build an old version: `git switch v0.1`. Come back with `git switch main`.

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
