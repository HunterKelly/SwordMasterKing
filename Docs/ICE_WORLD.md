# Level 2 — Ice World

## Play it

1. Pull the latest repository changes, then open the `SwordKing` project in Unity.
2. Open `Assets/_SwordKing/Scenes/IceWorld.unity` and press Play. The supplied scene creates its courtyard when it starts.
3. To make the scenery editable outside Play mode, choose **SwordKing → Setup → Create Level 2 - Ice World**. This builds and saves the scenery into that scene. Expand **World - editable Ice World** in the Hierarchy to move cover, towers, lamps, crates, and mountains.
4. Standalone builds start in Level 1. Defeat all seven Level 1 enemies, including the Gatekeeper, to enter Level 2 automatically after a one-second chapter message. Level 2 starts immediately with your current Power, Recovery, Speed and unspent embers, full health and flasks, and fresh encounters. Entering through Level 1 starts a new Level 2 save; opening IceWorld directly still offers its standalone title/Continue flow. Both scenes are in the global build scene list. If your active Build Profile overrides the scene list, add IceWorld there too.

## Tune it

Select `Assets/_SwordKing/Data/Levels/IceWorld.asset` in the Project window.

- **Random Enemy Count**: 12 ordinary enemies by default, plus one boss; supports 1–24 ordinary enemies.
- **Placement Seed**: changes the distribution. Positions exclude cover, the starting refuge, and the boss area. The same seed produces the same positions on restart or load. Keep the seed and count unchanged while continuing an existing save; after editing them, choose New Journey.
- **Enemy Combat**: controls chase, strafe, detection distance, tells, strikes, and recovery using the same combat settings as Level 1.
- **Snowfall Rate**: particles emitted each second, default 180. Zero disables snowfall.
- **Snow Footstep Volume**: default 0.16. Footsteps follow distance walked and stop while airborne, rolling, paused, or teleporting.
- **Arrival**: player start. Keep it on the courtyard floor and away from cover.
- **Boss Name**, **Display Name**, **Save Key**: level identity. Level 2 has a separate save from Level 1.

Enemy positions are generated from the seed, rather than the Level 1 encounter array. Geometry dimensions are in `Scripts/Levels/IceWorld/IceWorldBuilder.cs`. The floor is 44 × 52 metres (2,288 m²), approximately Level 1's total walkable area condensed into an arena. Changing generator code takes effect in the runtime-generated scene; rebuild an authored scene to apply generator changes there.

## Gameplay and visuals

A reference-inspired snowy fortress courtyard with four large cover blocks, a central stone structure, corner towers, snow caps, supply crates, stairs, warm lamps, distant snowy mountains and stylized trees. Uses the project's current procedural geometry art style; this is not a photorealistic recreation of the reference. Snow and soft crunch audio are procedural and need no external assets.

The Frost Warden waits at the far end. Approach within 14 metres to activate his boss encounter. Clear all ordinary enemies and the boss to complete Level 2. The starting refuge restores health/flasks and allows upgrades. The supply coffer grants two upgrade embers. Earned upgrades and defeated enemies persist in the Level 2 save.

## Validation

This environment does not have Unity installed, so Editor compilation, visuals, sound balance, and navigation still need an in-Unity playtest. Check cover corners for enemy navigation, jump/roll protection, boss activation, refuge/coffer interactions, saving/reloading, and the Level 1 → Level 2 transition. Current enemies use local obstacle steering rather than baked NavMesh pathfinding.
