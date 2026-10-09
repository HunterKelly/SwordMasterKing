# SwordKing

Unity **6000.6.4f1**, Universal Render Pipeline. Open `SwordKing` in Unity Hub (the repository root is not the Unity project).

## First run after restructuring

1. Let Unity finish importing and compiling.
2. Choose **SwordKing > Setup > Create Level 1 - The Broken Gate**.
3. The tool saves `Assets/_SwordKing/Scenes/BrokenGate.unity`, the player prefab, default settings, level data, and material assets.
4. Press Play, choose a sword style, and begin the journey.
5. Commit the generated scene, prefab, settings, materials, and all their `.meta` files.

The setup tool also puts Level 1 first in shared Build Settings. Check your active Unity 6 Build Profile if it overrides the shared scene list.

Once generated, **edit the saved scene directly**. Do not run setup again to apply ordinary edits: rebuilding replaces the level scene after confirmation. Existing player prefab and settings assets are reused.

## Where to make changes

| Change | Location |
| --- | --- |
| Move walls, floors, bridge pieces, scenery | `World - editable Level 1` in BrokenGate's Hierarchy |
| Move shrine / coffer interaction | `BrokenGateWorld` Camp / Cache Position; also move their visible geometry |
| Change player appearance, sword, pivots | `Assets/_SwordKing/Prefabs/Player/Swordsman.prefab` |
| Movement, jump height, roll distance and duration | `Assets/_SwordKing/Data/Player/DefaultPlayer.asset` |
| Enemy placements, names, styles; arrival point | `Assets/_SwordKing/Data/Levels/BrokenGate.asset` |
| Attack math | `Scripts/Combat/SwingModel.cs` |
| Movement and defense | `Scripts/Player/PlayerMotor.cs` |
| Attack resolution and slash effects | `Scripts/Player/PlayerCombat.cs` |
| Input bindings / mouse sensitivity | `Scripts/Input/PlayerInputReader.cs` |
| Camera collision / following | `Scripts/Camera/ThirdPersonCamera.cs` |
| Enemy AI, health and tells | `Scripts/Enemies/GateEnemy.cs` |
| Objectives, checkpoints, progression | `Scripts/Levels/BrokenGate/BrokenGateLevel.cs` |
| Save writeback | `Scripts/Persistence/BrokenGatePersistence.cs` |
| Menus and HUD | `Scripts/UI/BrokenGateHUD.cs` |
| Generated sounds | `Scripts/Audio/BrokenGateAudio.cs` |

`PlayerController`'s Inspector values are runtime state and legacy defaults. When assigned, the Player Settings asset initializes movement/combat settings on entering Play mode. Starting journey styles and saved upgrades set Power, Recovery, and Speed afterward.

## Project layout

All game-owned content lives in `Assets/_SwordKing`. Scripts are grouped into Player, Combat, Input, Camera, Enemies, Levels, Persistence, UI, Audio, Debug, and Compatibility. Editor setup code has its own assembly; runtime code cannot depend on UnityEditor. Art, Audio, Data, Prefabs, Scenes, Resources, and UI asset folders are separate from source.

Unity template assets under `Assets/Settings`, `Assets/TutorialInfo`, and `Assets/Scenes` are retained. `SampleScene` is not the game's entry scene.

## Architecture boundaries

The player uses independent input, camera, settings, and prefab-reference components. Its movement, combat, presentation, and sandbox files are partial sections of **one PlayerController component**, sharing its state deliberately; they are not extra components to attach. The level's HUD, audio, and persistence are likewise partial sections of **one BrokenGateLevel component**.

The fortress is now a serialized scene object graph, built once by the editor. Runtime uses those scene references and does not regenerate the authored fortress. A procedural fallback remains for old scenes and the combat sandbox. Enemies are still instantiated from level data with procedural placeholder visuals. The HUD still uses IMGUI and audio is still synthesized. This is an editable development foundation; future production art/UI can replace those independently.

## Compatibility and editing constraints

- Existing `BrokenGate.Level1.Save.v1` saves and encounter IDs are preserved.
- Keep the seven encounter IDs ordered 0–6; zones 0/0/0/1/1/1/2; the final encounter is the boss. Changing counts or zone topology requires a save/progression migration.
- Enemy zone ranges, gate coordinates, boss entry and completion checks still describe this Level 1 layout. If you redesign the route, update those checks in BrokenGateLevel and GateEnemy along with the scene.
- Player prefab pivots and CharacterController references must remain assigned. Keep the roll pivot at the body's center and damage volumes upright.
- The legacy SwordplayArena adapter is for locally generated old scenes. The original repo did not include metadata for its prototype scripts; if a local old scene shows a missing script, regenerate Level 1 with the new menu.
- Commit `.meta` files alongside assets. Unity regenerates IDE project/solution files; they are excluded from Git.

## Validation

Open **Window > General > Test Runner**, select EditMode, and run SwordKing's combat regression tests. Then follow `Docs/PLAYTEST.md` for scene, defense, save, and build checks.

### Chapter III — Cinder Castle

Clearing Ice World now transitions automatically to CinderCastle, carrying sword upgrades and embers. The snowy approach leads through a double-staircase great hall, side rooms on both floors, an open ground courtyard with an upstairs perimeter gallery, and a two-turn spiral tower to the crown arena. Eight guards patrol the castle; the Cinder King has 5,000 HP and stands approximately 7.5 metres tall. His marked fireballs leave a 2.5 metre radius burning patch for 10 seconds. Roll the impact or move out of the marker; jump or avoid burning ground. Four green globes each restore 40 HP and refresh on retry. The upper gallery shrine saves a checkpoint.

Open `Assets/_SwordKing/Scenes/CinderCastle.unity` and press Play to test directly. Geometry generates at runtime by default. To author it in Scene view, run **SwordKing → Setup → Create Level 3 - Cinder Castle**. Tune boss HP, visual scale, and fire duration in `Data/Levels/CinderCastle.asset`; layout and encounters live in `Scripts/Levels/Castle/CastleWorldBuilder.cs`. Keep CinderCastle enabled in any active Build Profile scene overrides.
