# Sprint, roll, and charged attacks

- Tap either Shift key and release within 0.18 seconds to roll. Roll begins on release, so it can be distinguished from sprinting. Rolling still requires ground contact and respects the existing recovery.
- Hold Shift for at least 0.18 seconds while moving to sprint at 1.6 times walk speed (9.6 m/s with the default 6 m/s walk). Releasing a sprint does not trigger a roll.
- Left click: quick tap slashes; holding at least 0.25 seconds while walking releases a 360-degree spin that hits all around you. While sprinting, left click remains a narrow forward thrust with 80% longer reach.
- Right click: overhead attack with 60% longer reach; while sprinting: jumping overhead heavy with 60% longer reach, which hits on descent rather than takeoff.
- Hold either mouse button to charge, then release to attack. Damage increases smoothly up to a two-second cap. Holding longer retains a full charge and never repeats attacks automatically.
- At full charge, damage is twice that variant's uncharged damage. Default variant multipliers: slash 1, overhead 1.25, thrust 1.15, jumping overhead 1.65. The existing sword stats and recovery-based damage still apply.
- Only one attack can charge at a time. If both buttons start together, right click takes priority. Sprinting during a charge selects the sprint variant for that charge, even if Shift is released first.
- Rolls, menus, loss of focus, respawns and chapter transitions cancel pending attacks. Releasing a button during a cooldown still uses the short single-attack buffer. Airborne jumping heavies cannot be stacked.

Select `Assets/_SwordKing/Data/Player/DefaultPlayer.asset` to adjust Sprint Multiplier, Sprint Hold Threshold Full Charge Damage Multiplier, and the three Special Attack Reach Multipliers. Attack reach, angle and style multipliers are in `PlayerCombat.cs` and `PlayerAttackModel.cs`. The charge cap is `PlayerAttackModel.MaxChargeSeconds`.

Both old and new Unity input backends are supported. HUD controls describe the new behavior and show charge percentage / sprint status. Current models use procedural sword poses and trails; model-specific animation clips can replace this presentation later.

Validation: edit-mode tests cover Shift tap/hold/cancellation, attack selection and charge scaling/capping. Unity is not installed in the authoring environment, so those tests and gameplay need to be run in the Editor. Check tap versus hold at low frame rates, click-release timing, both mouse buttons, charging into a roll, pausing while held, jumping-heavy collisions, chapter transfer, and both levels.

The walking spin completes in 0.45 seconds. New mouse attacks wait until it finishes; rolling can cancel its presentation. Damage resolves once per enemy on release, with the existing wall checks. The spin rotates only the visual body and leaves camera aim/collisions unchanged.
