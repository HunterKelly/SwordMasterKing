# Plate-armored knight

`Assets/_SwordKing/Prefabs/Player/ArmoredKnight.prefab` now uses human proportions,
curved breastplate surfaces, a slotted closed helmet, overlapping shoulder lames,
fluted arm and leg armor, modeled gauntlets, pointed sabatons, linked mail,
a tapered diamond-section sword, and a continuous crimson cape. Steel, silver
edges, dark steel, blue arming cloth, leather, and restrained gold details have
separate materials under `Art/Materials/Knight_*`.

This is an original rigid-part model inspired by the supplied armor reference.
It is not a photoreal scanned character, a skinned Humanoid avatar, or a set of
Mecanim animation clips. The cape is one connected mesh, deformed smoothly at its vertices, not separate
rotating panels. It uses procedural motion, not cloth physics.

## Open and customize

Pull the changes and allow Unity to finish compiling. Open the prefab or play
any of the three existing levels. The prefab's GUID and PlayerRig component ID
are preserved, so existing scene assignments still resolve. No scene files are
changed in this revision.

Create a Prefab Variant for custom armor proportions or material changes.
Keep mesh children beneath their joint pivots. Meshes use actual authored
vertices rather than scaled cubes and spheres. To modify the source geometry,
edit `Tools/build_knight_prefab.py` and regenerate from the repository root.
This overwrites the base prefab, generated source geometry, and knight materials.

`KnightAnimation` controls stride, knee bends, torso poses, and cape motion.
Step timing is driven by horizontal distance actually traveled after
CharacterController collision, rather than a fixed timer. Support ankles lock
in world space until lift-off; stops and blocked movement stop the step cycle.
Longer strides, foot clearance, heel-to-toe rotation, and opposing pelvis/shoulder
rotation support the motion. Walk/sprint transitions blend gradually. The torso
leans only slightly. Grounded body height remains steady; bounded visual leg
extension (up to 22%) accommodates longer steps without lifting/lowering the
whole body or dragging support feet. This affects joint offsets and leg meshes
only; jump and roll restore authored leg dimensions. Walking Stride,
Sprinting Stride, Walking Foot Lift, and Sprinting Foot Lift tune this motion.
The feet target the character's local floor plane; this is not terrain-raycast
foot placement. The cape is 16% narrower than the approved first plate-knight pass (about 10%
narrower than the previous continuous cape).
Two-bone visual arm fitting keeps the sword connected to the right hand, and
supports it with the left hand during overhead windups. The visible sword stays
within arm reach. Gameplay damage volumes, timings, movement collision, stamina,
knockback, and invulnerability still belong to PlayerController.

## Mesh import repair

The previous model guessed Unity's OBJ mesh subasset ID. That caused invisible
armor in the supplied screenshot. This version removes that OBJ dependency.
`Art/Models/Knight/*.bytes` are deterministic vertex/normal/UV/index source data.
`KnightMeshSource` decodes them into shared Unity meshes in Edit Mode, Prefab
Mode, and standalone builds. It uses normal TextAsset references and has no
external package dependency. It never generates gameplay colliders.

For persistent native `.asset` meshes, choose **SwordKing > Player > Bake Knight
Mesh Assets**. This optional command saves mesh assets under `Knight/Baked`
and updates the prefab's MeshFilters. Keep the source data as a fallback.

## Validation

`python Tools/validate_knight.py` checks all source geometry, index bounds,
finite vertices, normals, prefab references, material references, and the
joint hierarchy. The preview is rendered from those same vertices and material
colors outside Unity; it does not demonstrate Unity lighting or animation.

Run the Unity Edit Mode `KnightPrefabTests`, then test idle, walking, strafing,
sprinting, jumps, rolls, rapid attacks, both charges, and both Shift attacks.
Unity is unavailable in the implementation environment, so import, animation,
and standalone build verification still need to be performed there.

Left-arm swing now follows the solved left-foot position directly: forward leg
means backward arm, and backward leg means forward arm. A small pelvis yaw and
side-to-side weight shift accompany opposing shoulder rotation. Combat poses
still override the free walking arm, and the sword arm keeps its existing IK.

Cape ripples increase with movement, with a stronger trailing wave during sprinting.
A damped sideways response follows strafing and turns, and the fabric settles
gradually after stopping. The shoulder edge stays pinned; cloth and gold hem
use the same continuous deformation.
