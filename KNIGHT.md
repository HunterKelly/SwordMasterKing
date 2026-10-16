# Armored knight

Open `SwordKing` in Unity and play BrokenGate, IceWorld, or CinderCastle.
Each scene now references `Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab`.
The existing PlayerRig asset GUID and component ID are preserved.

Double-click the prefab to edit the model. It contains a closed helmet, visor,
plume, layered plate armor, gold trim, split teal tabard, and sword. Armor uses
the original mesh in `Art/Models/ArmorBevel.obj`; small details use Unity primitives.
The model is assembled from editable rigid parts rather than a skinned humanoid.

The root CharacterController and gameplay hurtboxes stay independent of graphics.
Hip, knee, shoulder, and elbow pivots have unit scale; their mesh children carry
the proportions. Retain the PlayerRig references when replacing parts. Keep the
sword shoulder in place: its animation uses the existing combat presentation.

`KnightAnimation` on the prefab controls stride, knee bends, arm motion, idle
breathing, torso follow-through, and tabard sway. Tune Movement Amount,
Attack Amount, and Pose Response in the Inspector. Rolls and spins use the
existing visual root. Attack timing still comes from PlayerController; these
are procedural poses, not Animator clips or root motion. Damage, attack rates,
movement speeds, knockback, and invulnerability rules are unchanged.

To make a second knight, create a Prefab Variant and assign it to the scene's
Game object's Player Prefab field. The scene Game object runs the combat and
spawns the model; dragging a model prefab into an empty scene alone does not
create a complete playable game.

Validation: prefab structure, GUID references, hierarchy, collision isolation,
scene assignments, and whitespace were checked outside Unity. Run Edit Mode
`KnightPrefabTests` after import, then check movement, rapid swings, both charged
attacks, Shift attacks, jumps, and rolls in Play Mode. Unity was unavailable in
the implementation environment, so import and gameplay tests are still pending.

`python Tools/build_knight_prefab.py` rebuilds the base prefab and bevel mesh.
This overwrites hand edits to those two generated assets; use Prefab Variants
for customizations you want to retain across regeneration.
