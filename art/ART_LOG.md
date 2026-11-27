# Drowned Bell artist handoff

After every art creation or revision, append a dated entry here in the same work session. Include the chapter, exact source and export paths, the visual decision, how it was checked, whether the build displays it, and the next concrete action. When a later pass closes an item, reference the earlier entry so the next artist can follow it.

## Earlier passes · 27 November 2026

- **Tidal Engine.** Seven room plates, Ballast Knight and Tidekeeper animations, the nave at dawn, and credits. [Asset guide and atlas coordinates](TidalEngine/Art%20guide.md). The two GIFs in `TidalEngine/Previews` are authored art mockups. Later game work integrated the chapter. In-game captures are under `docs/screenshots/engine_*`.
- **Four-chapter completion pass.** Added the Sluice portrait and gauge, Archive pipe diagram, machinery and recoil poses, chapter seals and save layouts, dawn sky, memorial variants and completion design. [Inventory and dimensions](Campaign/README.md). Mara Vale, Ivo Reed and Nel Orr are the chosen keeper names. The completion artwork uses blank fields for the game's time and deaths.

## 27 November 2026 · All-chapter re-review and Night Watch pickup

| Chapter | Art reviewed | Finding |
|---|---|---|
| I · Drowned Belfry | Title portrait, diver, enemies, Undertoll, lamp, winch, Brass Lung, HUD and checkpoint designs. Compared the sprite definitions with archived room and boss captures. | Older QA captures include early geometric sprites. Use the pixel-art Ripple Walk, Undertoll and release-title captures as visual references. The Belfry portrait has finer shading than the other three. Preserve that distinction unless a future brief calls for a unified portrait pass. |
| II · Sluice Works | Piston Limpet, Chain Porter, both Warden attacks, recoil, machinery, pressure gauge, portrait and save art. | The vent raises three caps. The sweep lowers the casing and shifts the arm behind it. Their silhouettes remain distinct in the saved motion clips. |
| III · Sounding Archive | Actor and fixture atlases, recorder and ledger, pipe map, chapter portrait, save design and attack clip. | Keep the decoy's body identical to the Archivist. The moving clapper identifies the real one. The existing clip is useful for poses but is too wide to judge clapper readability at small window sizes. |
| IV · Tidal Engine | Seven rooms, fittings, tiles, discharges, both editable actors, records, four boss attacks, phase-two crown, nave, memorial, completion and credits. | The Night Watch pickup capture shows the card still on the shelf while the collection banner is visible. The room renderer uses a plate with the card painted in. |

**Changed artwork.** [WatchCollected.png](TidalEngine/WatchCollected.png) is the 512 × 288 Night Watch plate after collection. It restores the background behind the punched card. Compared with [Watch.png](TidalEngine/Watch.png), exactly 488 pixels change, all inside x=411–436, y=124–150, counted from the top left. Compositing the existing 48 × 48 card from `Fittings.png` (source x=240, y=0) back at x=399, y=114 reproduces the original plate pixel-for-pixel.

**Checks.** Before adding the collected plate, decoded 37 design PNGs, 37 Unity resource PNGs, 20 archived capture PNGs and every frame of seven GIFs. Reviewed contact sheets of the designs, atlases, captures and sampled motion frames. All 31 PNGs in `Assets/Resources/Art` matched their design sources on dimensions and RGBA pixels. All 37 resource texture metadata files specified point filtering and disabled mipmaps. Aseprite opened the Ballast Knight (19 cels, four tags) and Tidekeeper (90 cels, twelve tags). The new plate was reopened and checked separately. These checks used existing captures; no fresh interactive playthrough was performed.

**Next work, in order**

1. **Game integration owner:** Select the collected plate once the third record is owned, including after reload. Compare the shelf before pickup, after the banner clears and after resuming the save. The alternative plate is currently only in the art folder. [Original pickup evidence](../docs/screenshots/engine_watch_record.png).
2. **Artist:** When current build captures are available, inspect the Archive clapper against the shelves and the Tidekeeper's hammer versus vertical slam at native 512 × 288. Capture the peak windups without labels before deciding whether either pose needs another cel.
3. **Artist:** Inspect the open governor latch and rotating bypass wheel against their painted backgrounds. Both have state overlays. Check for a second handle or rim before preparing additional clean plates. This is a verification item, not a confirmed defect.
