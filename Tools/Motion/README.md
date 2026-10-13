# Supplied Mixamo motion workflow

Runtime playback uses the already verified `Assets/Resources/Characters/Player/PlayerModel.fbx`. `Assets/Resources/Motions/PlayerMotions.fbx` is the static retarget/export reference, not the instantiated runtime player. `PlayerPoseData.bytes` contains twenty motion candidates baked at 60 FPS as model-space rotation deltas and hip displacement. `SourceArt/Motions/PlayerAuthored.blend` contains the editable retargeted actions and packed character texture. The original uploaded FBXs remain outside the repository at `/workspace/asset-import/motions`.

To rebuild from the original uploads, run both stages in order from the repository root:

```sh
blender -b --python-exit-code 1 --python Tools/Motion/import_mixamo.py
blender -b --python-exit-code 1 --python Tools/Motion/retarget_player.py
```

The first stage normalizes the common source skeleton and saves detailed source metrics outside the checkout. Its intermediate FBX uses the supplied skin, which lacks the matching source texture. **The second stage must follow**: it preserves the existing character's skin/UV/texture and transfers the motion to that exact rest rig. Do not deploy the first-stage intermediate.

Optional Blender inspection:

```sh
blender -b --python-exit-code 1 --python Tools/Motion/preview_candidates.py
```

Unity uses the baked poses in a separate visual component. FBX animation curves are disabled to avoid imported scale/axis curves deforming the skin. It does not apply animation root motion, alter combat timings, drive collisions or add damage events. The browser CI runs two authored-motion reviews plus the existing HUD/character/tutorial/camera checks before Pages deployment.
