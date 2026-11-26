# Sounding Archive art brief (for the art pass)

Chapter III's mechanics are finished and tested. Everything visual in it is a **placeholder** made of plain shapes (`Art.Square`, `Art.Circle`, `Art.Dome`). This brief lists every placeholder that needs real pixel art. Follow `docs/ART_DIRECTION.md` (palette, 16 px per tile, point sampling, bottom-centre anchors, no mipmaps) and match the Sluice Works pass (`PumpMachines.png` + `PumpMachineView.cs`).

**Rule:** art may change how things look, never where they are or when they happen. Gameplay reads only `Assets/Scripts/Core`; hit boxes, timings and marks come from there. If a sprite's bright attack edge does not line up with the hit box shown by the F1 inspector, the sprite is wrong, not the box. Do not edit files under `Assets/Scripts/Core` or the tests.

Where to look in the game: play to the Archive, or continue a save in it. F1 (I in the browser) shows hit boxes and states. The QA bot captures every beat: `DrownedBell.exe -autoplay <folder>` writes `archive_room_*`, `moth_*`, `widow_*` and `archivist_*` screenshots.

## Actors: replace the shapes in `Assets/Scripts/Game/ArchiveFoeView.cs`

The view already switches on each beat. Swap each `Art.NewSprite(..., Art.Dome, ...)` for atlas sprites (suggested: `Assets/Resources/ArchiveFoes.png`, 64 px cells, same layout idea as `PumpMachines.png`) and pick the frame from the beat it already reads.

| Actor | Hit box (tiles) | Beats to draw (from Core) | What each beat must say |
|---|---|---|---|
| **Ink Moth** (`InkMoth`) | 0.9 × 0.7, flies | `Hover` (2 frames), `Mark` (wings folding, 0.9 s), `Dive` (straight streak), `Lodged` (stuck in the floor, 0.9 s), `Rise` | Mark = "I am about to drop on that spot". Lodged must look helpless: that is the punish window. Only Dive hurts. |
| **Moth landing mark** | 1.2 wide strip on the floor | shown during `Mark` and `Dive` at `moth.Mark` | Coral floor mark, readable on dark stone. Grows or pulses with `MarkProgress`. |
| **Tuning Widow** (`TuningWidow`) | 1.4 × 1.1, on the floor | `Idle`, `Spread` (forks opening over 0.8 s, use `SpreadProgress`), `Ring` (struck pose; rings twice, 0.75 s apart), `Recover` (sagging, 1.2 s) | Spread is the tell. Recover must look open and vulnerable (it only takes damage then). Touching it is safe; its rings are the danger. |
| **Archivist of Depths** (`Archivist`) | 2.2 × 2.4 | `Dormant`, `Rise`, `Hover`, `Mark` (1.0 s), `Dive`, `Recover` (grounded, 1.1 s) + a phase-two look when `SecondPhase` | A bell-shaped ink figure. Recover = grounded and exposed. Phase two needs a persistent silhouette change (the plan suggests ink bleeding/cracking). |
| **Ink decoy** | same size | drawn at `DecoyMark` during `Mark` only | Must be identical to the real one **except** the clapper. |
| **Real clapper** | small, under the real body | swings during `Mark` (`Mathf.Sin(time * 14)`) | This is the whole fight's tell: only the real Archivist swings it. It must stay visible with reduced flashing on and be readable at native 512×288. |
| **Archivist marks** | 2.4-wide floor strips at `RealMark` and `DecoyMark` | `Mark` and `Dive` | Both marks look the same (the clapper is what tells them apart). |
| Ripples from the widow and the Archivist | `Boss.WaveBox`: 0.8 wide, `WaveHeight` 0.9 tall | already drawn with `Art.DrawWave` | Keep the Undertoll's wave crest so players recognise "jump this". |

Frame budget from `docs/CAMPAIGN_PLAN.md`: regular enemies 24–32 px canvas, 2 idle, 2 anticipation, 2 active, 2 recovery, 1 hurt. Archivist 48–64 px, 4 idle, 3 anticipation, 3 recovery, plus the phase-two change. Flash on hit is already handled (`Flash`, white tint, off with reduced flashing).

## Props: replace the placeholders in `Assets/Scripts/Game/LevelView.cs`

Search for `PLACEHOLDER`. Positions are already correct.

| Placeholder | Where | Needs |
|---|---|---|
| `PLACEHOLDER echo drum` | Receiving Desk (practice drum) and Return Chute (live drum), standing on the floor | A recording drum. The practice drum is harmless and the live one hurts, so tell them apart (e.g. the practice drum unstrung or covered). Both already tint warm during the wind-up (`Windup`). |
| `PLACEHOLDER keeper ledger` | Map Loft, at `Level.Record` | Second keeper record (the first is the Sluice gauge plate). Also needs a 16 px inventory silhouette and a larger inspection drawing (campaign plan). |
| `PLACEHOLDER drum key` | Appears at the widow's feet after it dies | Brass key, warm (interactable). |
| `PLACEHOLDER sea-gate order` | Appears where the Archivist fell | Sealed scroll; the chapter's goal item. |
| `PLACEHOLDER chute sill` / `chute light` | Return Chute stop (`SoundingArchive.ChuteStop`) | Gridded counterweight chute. The light turns warm when open (`Level.GrateOpen`). The counterweight itself currently reuses the Belfry winch sprite (`PixelArt.Winch`); give it its own look. |
| `PLACEHOLDER engine-room exit` | Recording Choir east wall (`Level.Outlet`) | Exit to the Tidal Engine. |
| `drum-key door` | Return Chute → Choir passage (`Tile.KeyDoor`) | Locked door bars that vanish when `Level.KeyOpen`. |

## Environment

`LevelView.BuildRoom` draws every room with the Belfry backdrop. The Archive needs its own kit (campaign plan, "Archive" row): oxidized shelving, ink-stained stone, green-glass niches, recording drums and suspended walkways. Add it as an `if (lv.Chapter == Chapter.Archive)` branch like the existing Sluice branch. The one-way shelves (`=`) in Silt Stacks, the Listening Cells stack and the Map Loft should read as bookshelves you can stand on. Also wanted: a chapter plate (192 × 288 portrait, 24 colours) for the title and ending screen.

## Checklist before handing back

1. `cd tests/Core.Tests && dotnet run -c Release` still ends with "all checks passed" (art must not change Core).
2. Unity builds with 0 errors: **Drowned Bell > Build Windows**.
3. `DrownedBell.exe -autoplay <folder>` still reports VICTORY through all three chapters with zero runtime errors. Look at its `moth_*`, `widow_*` and `archivist_*` screenshots at native size.
4. With F1 on, every bright attack edge matches its red box.
5. Update `docs/asset-manifest.json` with each new file (size, frames, palette, origin).
