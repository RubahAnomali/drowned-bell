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
