# Defeated body grounding fix

Verified source: `ff33480af0a80c5113a7bf15f2706528d8306fae`.

The encounter director froze rigid bodies at the moment of defeat, including airborne positions. Existing placeholder/model death rotations did not align the visible body with the floor.

DefeatedBody now disables collision and movement after defeat, animates the visible body down to the classroom floor, and grounds the actual posed mesh rather than bind-pose/culling bounds. Characters without a supplied death clip tip down over 0.45 seconds; the player keeps the supplied Stunned clip. The gameplay root remains unchanged. Final positions are cached to avoid repeated skin-bound work on settled corpses. Rigidbody interpolation is suspended during settling and restored together with original colliders, gravity and the player motor on retry. The old competing death-rotation branches were removed. Encounter progression and corpse cleanup between areas remain intact.

Actual WebGL fixture: ordinary enemy, supplied Deongchi and supplied player were killed with real ReceiveHit calls while airborne. 42 assertions checked lethal damage, disabled collision, player motor stop, completed death poses, floor contact within 2.5 cm, lying-down height, stable positions, restored physics/interpolation/colliders/motor, and attack availability after revival. `corpses-grounded.png` is the execution screenshot from this dedicated fixture, not a mockup or a normal classroom campaign screenshot.

The existing four-resolution HUD suite, two supplied-motion regression reviews, production tutorial input tests and actual browser audio/mute tests also passed with no runtime errors. Full campaign progression is not claimed as manually played through by this fixture.

Play: https://pig556347-boop.github.io/-0-/?v=ff33480a
