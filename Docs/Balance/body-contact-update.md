# 2026-10-18 combat contact / tempo revision

Dojaehak MMA kick removed from the combat whitelist entirely. Native source audit found both hands above the head at frames 102–103, with a rising two-hand tail already at 90–98. One-two finishing kick now uses SideKick; SideKick begins at frame 28 to skip the high-hand preparation. QuadPunch, Kicking and RoundhouseKick remain excluded. Source clips remain available in isolated review only. Live chain tests inspect both hand heights throughout every pattern.

All campaign enemy start intervals shortened about 12%, movement about 8%; startup/recovery modestly shortened. Dojaehak legal attacks run 8% shorter with the contact frame still mapped to the exact damage event; pattern rests 1.20 / .90 seconds. Damage, enemy counts and simultaneous attack caps retained.

Attack lanes: punches .64m, kicks .82m (was 1.20m); height 1.10m (was 1.50m). The far edge now stops at configured reach instead of reach + .30m. Physical overlap is a broad phase only; oriented body rectangles perform an independent narrow-phase check. Hurt regions ignore extended arms and accessories, shrink during crouch, move with jumps and reject dead actors. Movement/grounding capsules remain separate.

Widths/depths are fitted to weighted torso mesh vertices, not whole T-pose bounds. Blender audit uses Hips/Spine weights > .7 and normalized height .52–.78. Measured width/depth (m): Minion1 .455/.368; Minion2 .401/.330; Minion3 .482/.387; Minion4 .404/.332; Dojaehak .396/.368. Gameplay profiles round down to .46/.36, .40/.33, .48/.38, .40/.33, .40/.36. Player .46/.32; broad Deongchi .70/.48. These are stable torso regions, not per-triangle animated limb collisions.

Real-physics tests cover slim-body near miss, true slim-body edge hit, wide-body contact at the same lane, distant punch miss and player dimensions, plus existing guard/parry/dodge/multitarget/corpse tests.

Lobby image replaced with newly generated ink/halftone comic artwork, restrained teal/ivory palette and quiet space behind the menu. It is still generated artwork; this revision removes the glossy photographic rendering style.

Runtime verification pending.
