# Drowned Bell campaign

## Premise and scope

Silencing the Undertoll opens a way below the belfry. The diver follows the pressure mains through the Sluice Works, learns who ordered the flood in the drowned signal archive, and reaches the tidal engine before the next high tide. The final action is to reverse the sea gate and walk out into the drained nave.

Design target for a first-time player: 55–80 minutes over four connected chapters, including exploration and retries. This is a target for playtesting. The current Windows slice contains all seven Belfry rooms and all seven Sluice rooms. The other rooms, enemies, bosses, persistent saves and ending below are production work still to do.

Keep one move set throughout: run, jump, dash, directional hook and pogo. Upgrades are optional. Every required rise stays at or below two tiles and every mandatory gap at or below three, against the measured 3.26-tile rise, 5.33-tile running reach and 7.78-tile jump-dash. Reserve demanding combinations for optional routes with a safe return. Use the existing 512 by 288 camera, 16 pixels per tile and bitmap UI.

## Areas and rooms

### I. Drowned Belfry · 10–15 minutes · playable

Belfry Landing teaches controls and establishes the checkpoint lamp. Flooded Nave introduces the Brine Crab on flat ground. Ripple Walk introduces the floor wave with a safe observation point. Gutter Shaft introduces the Knell Jelly and drops into Choir Gallery. The gallery combines those lessons and offers the optional Brass Lung, one permanent extra mask. Bell Ropes opens the lamp shortcut. The Undertoll chamber tests the floor-wave jump and hook punish. Beating it unlocks the chapter transition. The Brass Lung, death count and elapsed play time carry into the Works.

### II. Sluice Works · 15–20 minutes · seven rooms playable

| Room | Player task and route purpose | Exit or reward |
|---|---|---|
| Intake Dock | Rest at the lamp, watch the first floor vent from safety, then cross its quiet interval. | East service tunnel |
| Bleeder Gallery | Read a second valve beneath a riveted main. Low service ledges offer elevation and a view of the crossing. | East into Valve Hall |
| Valve Hall | Separate a returning crab from the wheel, swing the hook at the wheel, then pass the lifted grate. | Inspection gate opens into Impeller Well. |
| Impeller Well | Descend around a stationary impeller before meeting a Piston Limpet in a wide landing bay. | Drain a bypass channel back to the intake lamp |
| Sediment Trap | Optional side pocket. Cross two staggered vents to recover the keeper's gauge plate and open a dry return stair. | First of three optional keeper records |
| Counterweight Walk | Bait a Chain Porter into throwing across a safe recess, then cross during its rewind. | Release the lift beside the local lamp |
| Duty Pump | Read the relief cycle while fighting the Pump Warden. | Pump key and the eastern archive hatch, the current build ending |

### III. Sounding Archive · 15–20 minutes · planned

| Room | Player task and route purpose | Exit or reward |
|---|---|---|
| Receiving Desk | Light the green-glass lamp. An empty recording drum demonstrates two successive echo rings over a harmless floor. | Open stacks |
| Silt Stacks | Bait an Ink Moth's marked landing away from the nearest shelf, then counter. | Drum lift |
| Listening Cells | Meet a Tuning Widow alone. Cross the first pulse and wait for its delayed echo. | Drum key |
| Map Loft | Optional upper shelves with a safe fall to the main path. Follow an engraved pipe diagram. | Second keeper record |
| Return Chute | Slide the gridded counterweight to open the lamp shortcut. Mix one moth with one echo source in a wider room. | Choir antechamber |
| Recording Choir | Distinguish the Archivist's real body by its moving clapper. | Original sea-gate order and engine-room access |

### IV. Tidal Engine · 15–25 minutes · planned

| Room | Player task and route purpose | Exit or reward |
|---|---|---|
| Service Dock | Light the final lamp and inspect a model of the reversal mechanism. | Accessible practice bay |
| Flywheel Cradle | Meet a Ballast Knight alone. Pass under the lifted hammer, then strike the exposed hinge. | First governor latch |
| Condenser Bridge | A fixed vent alternates with a visible floor-wave source. Dry islands let the player wait between them. | Second governor latch |
| Night Watch | Optional side room reached from the bridge. A short pogo sequence crosses three stationary pads; a lower walk returns the player to the start. | Third keeper record |
| Governor Stair | Both latches release a lift back to the dock. A quiet overlook shows the sea gate and offers a final rest. | Final arena |
| Sea Gate | Fight the Tidekeeper; the bypass wheel becomes usable after its defeat. | Manually reverse the tide |
| Morning Nave | A quiet playable return through the drained first room. The recovered records add names to the keeper memorial. | Credits, completion time, deaths and new-game action |

## Enemy and boss specifications

Use the silhouette and posture as the primary tell. Support each with a sound and a local color change. Each new enemy has a solo introduction before any mixed encounter. Timings below are initial tuning targets in seconds, subject to real controller tests.

| Regular enemy | Tell and response | Punish |
|---|---|---|
| Brine Crab / Knell Jelly | Existing raised claw and radial swell teach spacing and ring timing. Reuse the crab in Valve Hall. | Existing 0.8 / 1.3 recovery |
| Piston Limpet | A wall shell retracts for 0.8, then drives a piston across a marked short lane. Wait behind the lane end. | 1.0 with piston extended |
| Chain Porter | Plants its feet and draws a straight chain taut for 0.85; the throw locks to the original bearing. Jump or change elevation. | 1.1 rewind |
| Ink Moth | Mark appears on the floor for 0.9 while wings fold, then the moth dives at that mark. Walk out and turn. | 0.9 lodged in the shelf |
| Tuning Widow | Forks spread for 0.8, followed by a ring and a second ring 0.75 later. Jump at the two distinct expanding rims. | 1.2 after the echo |
| Ballast Knight | Lifts hammer for 1.0; oil drips from the exposed hinge. Move behind it as the facing locks. | 1.1 after impact |

| Boss | Phase one | Phase two and fair response | Skill rehearsed |
|---|---|---|---|
| Undertoll | Existing slam and rush, then hook the mouth on recovery. | Five marked rain drips. Move from the targeted position. | Ripple Walk and Flooded Nave |
| Pump Warden | Three pipes along the floor. A rising stem marks one pipe for 1.0 before it vents. Cross to its dark neighbor. A separate piston sweep leaves a 1.2 punish. | Below half health, vents fire sequentially. Keep one lane quiet at all times. The active pipe closes before a sweep can cover its escape. | Intake vent, limpet reach, porter rewind |
| Archivist of Depths | Ink silhouettes mark a dive; only one has a swinging clapper. Step out of the marked landing and attack the grounded body for 1.1. | Its landing sends a delayed second ring. The clapper stays visible in reduced-flash mode. | Moth targeting and Widow's paired pulses |
| Tidekeeper | Hammer lift exposes a passage underneath. A separate slam sends a visible floor wave. Attack the hinge during the 1.2 reset. | Rotates wave, pressure lane and echo attacks in short phrases. Leave a grounded resting beat and one safe lane between phrases; no stacked unavoidable damage. | Knight hinge, condenser timing, archive echoes |

## Pixel art production list

The diver's bone helmet, brass interactables, coral damage cues and seafoam water link all four areas. Reuse player poses, hook, HUD, particle renderer, lamp and winch. Give each area its own architectural silhouette so players can recognize it with the HUD hidden.

| Area | Tile and backdrop kit | Interactive props, effects and illustrations |
|---|---|---|
| Belfry | Existing arches, blue masonry, ledges and flooded recesses | Existing actors, ripple bell, Brass Lung, title and chapter portrait |
| Sluice | Riveted mains, impeller vanes, the optional drain, dry return stairs, hoist chains and counterweights. | Slotted valves, relief plumes, moving piston, chain spool, keeper gauge plate, pump key, pressure dial and archive hatch. |
| Archive | Oxidized shelving, ink-stained stone, green-glass niches, recording drums and suspended walkways | Landing mark, echo rims, two silhouette decoys, drum key, pipe diagram, order scroll and archive chapter plate |
| Engine | Overlapping flywheels, chain lifts, cutaway gear teeth, seawall blocks and a dawn sky plane | Two governor latches, stationary pogo pads, hammer hinge, reversal wheel, third record, drained nave tiles and final dawn illustration |

Actor budget per new regular enemy: one 24–32 px source canvas, two idle frames, four locomotion frames if mobile, two anticipation frames, two active frames, two recovery frames and one hurt pose. Anchor feet or attachment bolts consistently. Budget a 48–64 px boss canvas with separate moving appendages, four idle frames, three anticipation and three recovery frames per attack plus a persistent phase-two silhouette change. Match the actual damaging extent to the bright attack edge. Environment tiles are 16 px, landmarks 16–48 px. Chapter plates use a 192 by 288 portrait with a 24-color budget. Design a 16 px inventory silhouette and a larger readable inspection drawing for each record. Include save, continue and credits screens in the final UI pass.

Track created files, dimensions, frame count, origin, palette and approval in the asset manifest. Check alpha and point filtering, inspect a contact sheet, compare warning and active silhouettes at native scale, then capture both in a standalone combat room.

## Progression and delivery

1. **Opening slice (completed).** Undertoll clear → chapter interlude → intake lamp → vent lesson → gallery → valve → hatch. Confirm normal inputs unlock the gate, death preserves the open gate, and one replay clears both chapters.
2. **Full Works (current build).** Both branches converge at the lift lamp. The Limpet and Porter have solo introductions before the Warden combines the pressure and sweep lessons. Human pacing and first-time readability remain to be measured.
3. **Campaign persistence (done, 27 November 2026).** Before expanding into the Archive, implement one local save slot with Continue/New Game, atomic writes at lamps, chapter entry, pickup and boss clear, and recoverable versioned reads. Store chapter, lamp, gates, records, upgrades, elapsed time and deaths. Verify cold launch after a chapter transition and checkpoint death. Implemented in `Core/SaveGame.cs` (format, `World.Snapshot` and `World.Restore`, covered by the headless "Save and continue" checks) and `Game/SaveStore.cs` (temp-file write then move, fallback read, off during QA). Positions are not saved: Continue wakes at the last lamp or lift.
4. **Archive and engine.** Deliver each area as a complete lamp-to-boss loop. Build mandatory rooms first, then optional loops, chapter plates and final audio. Encounter resets must preserve opened shortcuts; no optional record may gate an ending.
5. **Release.** Verify a fresh extracted Windows x64 build on a second PC, a full uninterrupted campaign and a saved/resumed campaign. Time at least three first-time runs; tune encounters and wayfinding before adding rooms for length. Target under 25 seconds from respawn to a boss retry. Verify reduced flashing, shake settings, keyboard/mouse, gamepad, window resize, restart, credits and a return to title. Rebuild browser separately if distributing that edition.
