# Drowned Bell art direction

## Game frame

A salvage diver carries a hook and a single lamp into a drowned bell tower. The player runs, jumps, dashes, dodges, and punishes recoveries. The camera is a 2D orthographic side view in Unity, with the diver facing left or right. Windows is the current target.

The game renders at 512 × 288 at 16 pixels per tile with the default camera. It scales to the window with nearest-neighbor filtering, preferring an integer scale when it fills at least 85% of the available area. The camera and actor roots align to the pixel grid. The HUD uses a 5×7 bitmap face and helmet health icons.

## Visual system

| Role | Swatches |
|---|---|
| Ink, dark stone, deep shadow | `#0B0F14` `#142028` `#050709` |
| Lit stone and patina | `#1F3340` `#2F4B55` |
| Water and wave crests | `#4B7378` `#8FB3A8` |
| Diver and readable neutral details | `#E6E1CF` |
| Landmarks and interaction | `#D79A3B` `#F2C14E` |
| Hazards and oxidized brass | `#E0584A` `#8C4A2F` |
| Jelly and its charge | `#6E5A9E` `#B7A6E0` |

Use broad stone arches and straight courses in the background. Give the diver a round bone helmet, narrow shoulders and a distinct hook silhouette. Use broad claws for the crab and a tall translucent cap for the jelly. Each bell has a narrow crown and a wide brass lip with a square clapper. Front-left highlights pick out seams in the metal. Reserve crisp value changes for walkable edges, enemies and signals. Flooded recesses stay in two low-contrast value bands.

The training bell leans before it rings. Its floor signal widens from the source, then the wave shows the same crest as the Undertoll's slam. Use warm brass for an object the player can act on and coral for immediate danger. Avoid continuous screen flashes, fine texture noise and soft halos on foreground silhouettes.

## Technical contract

Keep pixel maps in code with transparent empty cells, no mipmaps and point sampling. Use a centred pivot for hanging bells and a bottom-centre anchor for actors. Keep the baselines stable between poses, and check silhouette against the lightest stone and darkest recess on screen. Assemble the environment with 16-pixel tiles. The asset manifest records current status and frame plans.

## Visual target

The Ripple Walk bell in `Assets/Scripts/Game/Art.cs` sets the material and outline style for the sprite family. `PixelArt.cs` contains the diver, hook, crab, jelly and Undertoll, including the phase-two crack. `LevelView.cs` draws stepped arches, masonry and the waterline on the native grid. Wind-up, recovery and impact cues combine sprites with stepped tints and effects.

## Flooded belfry and route landmarks

The 192 by 288 belfry portrait occupies the right side of the title and ending. Its large suspended bell and small diver establish scale before the first room. The 24-color raster uses point sampling, native dimensions, uncompressed import and no mipmaps. The source record and generation prompt are in the asset manifest. The checkpoint uses a caged pilot light, the shortcut uses a spoked winch, and the Brass Lung has two ribbed chambers joined at the regulator. Each sprite uses the gameplay palette at 16 pixels per tile.

## Sluice Works

Riveted iron mains carry the eye across room boundaries. Vertical pipe drops lead to slotted valve plates at floor level. A quiet plate has a dark gauge. A coral indicator and short puff give one second of warning; the full three-tile jet then occupies its bright column for one second. The two-second quiet interval gives room to cross on foot. The outlet has a stepped steel surround and a brass direction arrow. The vent animation follows simulation time, including pause. All three new source sprites use the existing ink table.

## Pump machinery

The limpet carries a low stepped shell around a sliding brass piston. The porter has two planted boots and a chain drum on its back. The Warden is a four-footed boiler with a large pressure dial and exposed gold service panel during recovery. The Warden has four poses. The Limpet and Porter each have six, including a three-drawing windup on a wider 40-pixel canvas. The Limpet sinks low over its piston; the Porter keeps both boots planted and draws the chain weight above its rear shoulder. Both hold the last silhouette for the final part of the warning. Horizontal attack markers follow the locked firing lane. The Warden marks its relief outlet before the full steam column appears, and a separate exhaust plume signals overpressure. The updated actor atlas contains 11 RGBA entries including transparency and passes the 12-color art check.

## Pressure and ink

For a vent, the Warden lengthens its legs, extends three brass chimneys and lifts the pressure caps. For a sweep, the feet spread, the casing lowers and the arm folds behind the flywheel. The final pose holds until the live attack, including delayed sweeps. The floor signal still identifies the affected lane.

Archive enemies use ink-violet outlines against green glass. The moth closes broad wings to a vertical nib; the widow separates twin tuning forks, strikes and sags; the Archivist narrows its robe before a dive and opens its hem on landing. The phase-two costume gains torn shoulders. The double wears the same costume and pose. Only the real figure carries the swinging brass clapper. The practice drum has a strap across its head; the live drum has bright tension rods. Pickups share brass details, and the ledger has an enlarged pause-screen drawing.
