# Versions

- `main` is the current version. New work lands here.
- Each release gets its own branch, `v0.1`, `v0.2` and so on, cut from `main` when that version is done.
  A version branch is a frozen snapshot: nothing new is committed to it.
- To play or build an old version: `git switch v0.1`. Come back with `git switch main`.

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
