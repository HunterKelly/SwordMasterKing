# Sprint, roll, and charged attacks

- Tap either Shift key and release within 0.18 seconds to roll. Roll begins on release, so it can be distinguished from sprinting. Rolling still requires ground contact and respects the existing recovery.
- Hold Shift for at least 0.18 seconds while moving to sprint at 1.6 times walk speed (9.6 m/s with the default 6 m/s walk). Releasing a sprint does not trigger a roll.
- Left click: quick tap slashes; holding at least 0.25 seconds while walking releases a 360-degree spin that hits all around you. While sprinting, left click remains a narrow forward thrust with 110% longer reach.
- Right click: overhead attack with 60% longer reach; while sprinting: jumping overhead heavy with 60% longer reach, which holds the sword on ascent, swings on descent and hits on landing.
- Hold either mouse button to charge, then release to attack. Damage increases smoothly up to a 0.6-second cap for the spin and a two-second cap for other attacks. Holding longer retains a full charge and never repeats attacks automatically.
- At full charge, damage is twice that variant's uncharged damage. Default variant multipliers: slash 1, overhead 1.25, thrust 1.15, jumping overhead 3.5. The existing sword stats and recovery-based damage still apply.
- Only one attack can charge at a time. If both buttons start together, right click takes priority. Sprinting during a charge selects the sprint variant for that charge, even if Shift is released first.
- Menus, loss of focus, respawns and chapter transitions cancel pending attacks. Rolls cancel active attacks when permitted by the recovery clock. Releasing a button during a cooldown still uses the short single-attack buffer. Airborne jumping heavies cannot be stacked.

Select `Assets/_SwordKing/Data/Player/DefaultPlayer.asset` to adjust Sprint Multiplier, Sprint Hold Threshold Full Charge Damage Multiplier, and the three Special Attack Reach Multipliers. Attack reach, angle and style multipliers are in `PlayerCombat.cs` and `PlayerAttackModel.cs`. The charge cap is `PlayerAttackModel.MaxChargeSeconds`.

Both old and new Unity input backends are supported. HUD controls describe the new behavior and show charge percentage / sprint status. Current models use procedural sword poses and trails; model-specific animation clips can replace this presentation later.

Validation: edit-mode tests cover Shift tap/hold/cancellation, attack selection and charge scaling/capping. Unity is not installed in the authoring environment, so those tests and gameplay need to be run in the Editor. Check tap versus hold at low frame rates, click-release timing, both mouse buttons, charging into a roll, pausing while held, jumping-heavy collisions, chapter transfer, and both levels.

The walking spin completes in 0.45 seconds. New mouse attacks wait until it finishes; it has 0.15 seconds of recovery before another attack or roll. Damage resolves once per enemy on release, with the existing wall checks. The spin rotates only the visual body and leaves camera aim/collisions unchanged.

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
