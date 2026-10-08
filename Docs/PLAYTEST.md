# Restructure acceptance checks

Target Unity: 6000.6.4f1. Automated Unity compilation, editor generation, and playtests have not been run in the remote workspace because Unity is unavailable there.

1. Import project: zero compiler errors. Test Runner EditMode combat tests pass.
2. Create Level 1: world geometry is visible without entering Play mode; player prefab, materials, and both data assets exist. Close/reopen scene to check serialized references.
3. Move a courtyard decoration, save, enter Play: edit remains and no duplicate fortress is generated.
4. Open player prefab, change a cosmetic piece, save: spawned player uses that change. Keep rig references valid.
5. Set roll distance to 2 in DefaultPlayer.asset; enter Play: roll travels the shorter distance. Restore 3.6 afterward.
6. Begin Heavy, Balanced, Fast runs: intended stats and swing timing. Rapid presses resolve once per accepted press; waiting gives a stronger strike.
7. Sandbox: Space jumps about half the controller's height. L low hits are rejected airborne; H high hits still damage. During roll both are rejected; after roll both return. Wall collision and camera collision remain intact.
8. Level 1: defeat courtyard and gatehouse; gates open, cache awards embers, shrine restores health/flasks and saves checkpoint; upgrades persist.
9. Pause, resume, lose app focus: controls and cursor lock behave; no movement while the journey is paused.
10. Die and respawn: defeated encounters remain defeated, surviving encounters refill, and checkpoint position is restored.
11. Quit and reopen: Continue restores the v1 journey. A previously completed journey still displays victory.
12. Defeat Gatekeeper and cross exit: victory fires. Build with BrokenGate first in the active Build Profile; check rendering, audio, save persistence, and input in standalone.

Do not rebuild the level to apply scene edits; rebuild is a destructive replacement of that one scene and asks for confirmation.
