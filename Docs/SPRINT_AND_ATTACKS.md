# Current controls

- **C:** roll on press, respecting the existing ground and recovery requirements.
- **Hold Shift:** sprint immediately while moving; selects special attacks while stationary too. No tap/hold delay and releasing Shift never rolls.
- **Left click:** slash. Hold at least 0.25 seconds for a spin, fully charged at 0.6 seconds.
- **Right click:** overhead. Hold at least 0.25 seconds for a stronger overhead, fully charged at 0.6 seconds.
- **Shift + left click:** forward piercing thrust with a tiny hop and slide; chargeable up to two seconds.
- **Shift + right click:** jumping overhead heavy with landing impact; chargeable up to two seconds.
- **Space:** jump. **Q:** heal. **E:** interact. **Escape:** pause.

Mouse attacks fire on release. Shift can be held at a standstill to select either special. C cancels charging when a roll can start. Roll wins over jump if both inputs arrive together; sprint or charge recovery still prevents premature rolls. Holding C does not repeat rolls.

## Attack timing polish

Jump heavy holds its windup throughout ascent, animates downward during descent, and resolves damage exactly once on ground contact. It has 0.2 seconds of landing recovery, and its uncharged style damage multiplier is now 3.5.

Sprint thrust holds the sword forward for 0.55 seconds. It can hit new enemies for 0.45 seconds, one hit per enemy per attack, at up to 5.88 metres with default settings. It pierces other enemies while solid walls still block it. Its forward trail follows the player through the active window. Thrust and spin have 0.15 seconds of recovery after their animation; other charged attacks have 0.12 seconds. Movement remains available during recovery, while attacking and rolling wait.

Three quick, uncharged overheads within 0.5 seconds activate the overhead flurry pose and layered vertical cartoon cloud trails. These extra trails are cosmetic and do not create extra damage.

Thrust Duration, Thrust Damage Window, Special Attack Recovery, Jumping Heavy Recovery, Charged Attack Recovery and reach multipliers are in DefaultPlayer.asset. The spin charge cap and damage multipliers are in PlayerAttackModel.cs.

## Slide and impact update

Sprint thrust adds a collision-resolved 1.2-metre forward slide over 0.2 seconds, with the direction captured on release. Movement and the existing 0.45-second one-hit-per-target window continue. Thrust Slide Distance and Thrust Slide Duration are editable in DefaultPlayer.asset. Menu, respawn and roll cancellation clear the slide.

Quick horizontal clicks regain the horizontal flurry scribbles after three accepted slashes within 0.5 seconds. These and the overhead cloud are visual only.

Walking right-click held at least 0.25 seconds selects ChargedOverhead: a stronger overhead with base style multiplier 1.8, charging to twice that at two seconds. Quick right clicks retain the 1.25 multiplier and overhead flurry. Sprint right click remains JumpingOverhead with base multiplier 3.5, charging to twice that at two seconds. Existing charge/sprint recovery windows remain.

Jumping overhead's landing adds an expanding cosmetic shockwave, sparks, low impact sound and stronger camera shake even on a miss. Damage still resolves once through the forward heavy-attack volume, not from the cosmetic shockwave.

## Front-facing flurry and spin targeting correction

Horizontal cloud trails stay entirely in front of the player (their forward offset stays positive), without wrapping behind. Walking right-click overheads now fully charge in 0.6 seconds, matching the spin. Sprint attack charge caps remain two seconds.

Spin strikes test every eligible enemy within the 360-degree radius. Other enemies no longer occlude those strike checks, so a cluster can all be hit; solid scenery still blocks strikes. Each enemy receives one damage event per spin.

Jumping overhead animation starts about 0.04 seconds before the apex, with its animation duration extended by that same small lead. Damage and landing effects still trigger on ground contact.

## Final hyper-speed slash presentation

The flurry visual is exactly three parallel copies of the ordinary slash trail: the main slash and two offset bars. Horizontal quick slashes offset the copies vertically; overhead quick slashes offset the copies left and right. Three accepted quick attacks of that type within 0.5 seconds activate the effect. Normal speed shows one bar. Circular scribble/cloud geometry has been removed from flurries. The extra bars never resolve extra damage.

## Standing Shift specials and tiny thrust hop

Holding Shift selects thrust (left click) or jumping overhead (right click), even with no movement input. Moving while holding Shift still sprints, and C rolls. The player does not need to run forward to select either special.

Grounded thrusts add a tiny 0.12-metre hop alongside their existing forward slide. Thrust Hop Height in DefaultPlayer.asset adjusts it; zero disables it. An already airborne thrust does not add another jump. The existing damage window, one hit per enemy and recovery remain in effect.

## Stamina

The bar holds 100 points. Shift thrust and jumping heavy cost 35 per accepted attack. Sprinting drains 8 points per second while moving. Standing with Shift held costs nothing. Normal slash/overhead, charged spin/overhead, jumping and rolling cost nothing. At zero, sprint falls back to normal walking; Shift still selects specials, which require at least 35 points to execute.

Stamina pauses regeneration while sprinting, charging, executing a special, or in a menu. After 0.2 seconds of eligible recovery time, it refills at 60 points per second. Respawns and chapter arrivals refill it. Max Stamina, Special Stamina Cost, Sprint Stamina Drain, Stamina Regeneration and Stamina Regeneration Delay are editable in DefaultPlayer.asset.

Spin now rotates in 0.28 seconds (previously 0.45), with a base damage multiplier of 1.5 (50% more). Its charge cap remains 0.6 seconds and its recovery remains 0.15 seconds. It and charged overhead consume no stamina.

Unity is not installed here; run the edit-mode tests and gameplay checks in Unity, including sustained sprint drain, empty-stamina walking, Shift specials at a standstill, and free charged attacks.
