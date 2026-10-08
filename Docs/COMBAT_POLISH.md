# Combat feedback and active enemies

Apply SwordKing_CombatPolish.patch from the repository root with Unity closed. Open the existing BrokenGate scene afterward; **do not rebuild Level 1**. Existing scene, player prefab, progress, and authored geometry are preserved.

## Changes

- Regular pursuit: 2.25 -> 3.4 units/sec; boss: 2.5 -> 3.2.
- Regular windup: .9 -> .62 seconds; recovery: 1.25 -> .85.
- Boss windup: .9 -> .72; phase two: .65 -> .5. Recovery: 1.3 -> .95; phase two: .85 -> .7.
- Sword follow-through: .16 -> .12 seconds, interpolated from windup to strike.
- Group attack spacing: 1.05 -> .8 seconds. Attack facing still locks at the tell; each strike resolves once.
- Earlier detection, wider encounter pursuit, close-range circling, separation between enemies, obstacle steering, and a ground-support check to stop at ledges. Enemy navigation remains local steering, not NavMesh pathfinding; complex maze layouts may need NavMesh later.
- Hits trigger gold sparks, cosmetic enemy recoil, brief impact pauses (.018 light / .045 heavy), and restrained rotational camera feedback. Heavy audio adds a metallic transient. Kills produce the larger spark burst and a faster collapse.
- Impact pauses have a cooldown to prevent rapid cuts from keeping the game frozen. Pause, shrine, title, death, victory, disable, and teardown cancel the temporary freeze.

## Tuning in Unity

Select `Assets/_SwordKing/Data/Levels/BrokenGate.asset`. Expand **Enemy Combat** for pursuit, strafe speed, windup, recovery, and group spacing. Expand **Combat Feedback** to disable hit stop, camera shake, or sparks, or reduce shake strength. Existing assets receive the new serialized defaults; if a field is missing or unset, runtime fallbacks provide defaults.

No generated asset or scene rebuild is necessary for this update. The combat sandbox gets feedback automatically; adventure feedback uses the assigned Level Definition asset.

## Playtest checks

1. Enter the approach: wardens notice and pursue sooner. Retreat within the encounter route and watch them chase rather than stop at their former courtyard limit.
2. Kite in a circle: waiting enemies strafe, keep some separation, and attack with quicker windups. Retreat beyond attack range during the locked tell: the attack should miss.
3. Fight at walls, gates, and near the bridge: collision still blocks movement; enemies must not follow you into a gap. Check that narrowing corridors does not cause teleporting.
4. Land light and charged hits: compare sounds, recoil, sparks, and subtle camera feedback. Misses must not trigger impact feedback.
5. Rapidly hit several enemies: damage remains once per accepted press and hit pauses do not accumulate indefinitely.
6. Pause immediately after a heavy hit, wait, and resume. The title/pause screen must stay paused. Repeat with focus loss, shrine, death, and exiting Play mode.
7. Jump sweeps and roll overheads: faster attacks retain amber/red warnings and original defense exclusions.
8. Test boss phase two, death/respawn, saves, and a standalone build. Check feedback toggles in the Level Definition.

Remote validation: C# syntax, metadata, assembly JSON, unchanged core combat formulas, and patch application checked. Unity compilation, EditMode tests, and gameplay have not been run in the remote environment.
