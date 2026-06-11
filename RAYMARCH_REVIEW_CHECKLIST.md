# Raymarch Review Checklist

Review date: 2026-06-09

Purpose: track the correctness, flexibility, and API-design issues found in the current Route A raymarching implementation. This file is separate from `RAYMARCH_ROUTE_A.md` so the route tracker can stay milestone-focused while this document tracks review debt.

## Current Status

- [x] View-camera ray origin/range is correct.
  - Status: FIXED IN CODE 2026-06-11. Live snap/orbit validation is still recommended.
  - Evidence (was): `Assets/Raymarch/MultiDepthRaymarch.compute` reconstructed `nearW`/`farW`, set `ro = nearW.xyz`, then marched with `t = _Near + stepSize * i`. That applied the near offset twice in view-camera mode.
  - Resolution: `March` now uses `_ViewEyePos` as the true eye origin, matching `MarchMulti`; both `SingleCameraRaymarch` and `RaymarchCompositor` bind that input. `t` remains the single absolute march distance over `[_Near, _Far]`.
  - Fix checklist:
    - [x] Add an explicit `_ViewCameraWorldPos` or equivalent eye-origin input for view-camera rays.
    - [x] In view-camera mode, set `ro` to the real eye/camera origin and use `farW - ro` for the ray direction.
    - [x] Keep `t` as the single absolute march distance from `_Near` to `_Far`, or if `ro` remains `nearW`, start `t` at zero and adjust shading/range consistently.
    - [x] Update both `SingleCameraRaymarch` and `RaymarchCompositor` to set the new shader input.
    - [ ] Validate with the snap-to-source identity test and by comparing against the splat cloud from a translated free camera.

- [x] Compositing respects scene depth.
  - Status: FIXED IN CODE 2026-06-11. Mesh-in-front validation is still recommended.
  - Evidence (was): `MultiDepthRaymarch.compute` only sampled `_SceneColor`; no scene depth texture, linearized scene depth, or output depth was used. Hits always blended over the color buffer when `_Composite` was enabled.
  - Resolution: `RaymarchCompositor` and `MultiCameraRaymarch` now request `DepthTextureMode.Depth`, bind `_CameraDepthTexture`, pass the view matrix and `_ZBufferParams`, and enable `_UseSceneDepth` when compositing. `March` and `MarchMulti` compute each hit's Unity eye depth and leave the background unchanged when scene geometry is closer. This stays color-only compositing with depth rejection; it does not write a new depth buffer.
  - Fix checklist:
    - [x] Enable depth texture generation on the compositor camera (`DepthTextureMode.Depth`) or route an equivalent render depth source.
    - [x] Bind scene depth to the raymarch/composite path.
    - [x] Compute each raymarch hit's view/camera depth in the compositor camera's space.
    - [x] Compare hit depth against scene depth so real Unity geometry in front occludes raymarched reconstruction.
    - [x] Decide whether to keep color-only compositing with depth rejection or move to a pass that can also write depth.
    - [ ] Add a validation scene with a simple Unity mesh in front of the depth reconstruction.

- [x] Orientation and flip handling uses one shared mapping contract.
  - Status: FIXED 2026-06-10 (see `RAYMARCH_ROUTE_A.md` R1 for the full analysis).
  - Evidence (was): `DepthCameraModelBuilder.BuildFor` supports `orientation`, `flipU`, and `flipV`, but `DepthToUpright.compute` only accepts `_BodyCamera` and hardcodes either Upright or Transpose+flipV. The drivers pass only `_BodyCamera` into the pack shader.
  - Fix checklist:
    - [x] Define a single native-to-model/model-to-native pixel transform type for orientation and flips — `NativePixelTransform` in `DepthCameraModelBuilder.cs` (also carries `mirroredNativeBuffer`, see note below).
    - [x] Use that transform in `DepthCameraModelBuilder` (`Build`/`BuildFor` overloads; renderer-free `Build` core).
    - [x] Replace `_BodyCamera` in `DepthToUpright.compute` with explicit orientation/flip parameters — `_Transpose`, `_FlipU`, `_FlipV`, `_MirrorNative`, bound by `NativePixelTransform.ApplyTo`.
    - [x] Update `SingleCameraRaymarch` and `RaymarchCompositor` so the pack pass receives the same transform used to build the model (single `PixelTransform` instance feeds both).
    - [x] Replace or extend `NativeToModelPixel` so it handles flips — `NativePixelTransform.NativeToModel`/`ModelToNative`; the flip-less static was deleted after R4 migrated.
    - [x] Add tests or editor validation cases for Upright, Transpose, Transpose+flipV — `CameraModelValidator.RunPixelTransformTests` covers all 8 orientation/flip combos (round-trip inverse + relabeling equivalence). Hand-camera mapping still needs live confirmation.
  - Resolution note: the analysis showed synced model+pack flips cancel exactly (pure relabeling), so the geometry-affecting part of the mapping is the native buffers' 180°-rotated storage (legacy CSMain's mirrored depth fetch; the legacy color shader mirrors identically). That is now explicit as `NativePixelTransform.mirroredNativeBuffer` (default ON), which also resolves the 2026-06-08 "flipU vs flipV" compositor discrepancy: the desync was producing an accidental net-180° mapping equal to this mirror.

- [x] Camera-model live probe validates the same flipped model used at runtime.
  - Status: FIXED IN CODE 2026-06-11. Body-camera preset should still be rerun live.
  - Evidence (was): `CameraModelLiveProbe.PresetBodyCameraAndSample` set `flipV = true`, but `SampleAndCompare` built `cleanModel` with `BuildFor(sourceRenderer, orientation)` and did not pass `flipU/flipV`. The per-sample `NativeToModelPixel` path also ignored flips.
  - Resolution: `CameraModelLiveProbe` now owns a full `NativePixelTransform` including `mirroredNativeBuffer`, builds `cleanModel` from it, fetches the same native buffer pixel via `NativeToBufferPixel`, converts per-sample pixels via `NativeToModel`, logs the full transform, and draws frustum corners directly through the flipped clean model. The flip-less `NativeToModelPixel` shim was removed.
  - Fix checklist (updated 2026-06-10 for the item-3 fix):
    - [x] Build `cleanModel` from the shared `NativePixelTransform` (including `mirroredNativeBuffer` — the probe's green==gray comparison feeds the mirrored depth index by hand today, which is now the transform's job via `NativeToBufferPixel`).
    - [x] Update per-sample native-to-model conversion to use `NativePixelTransform.NativeToModel` (then delete the flip-less `NativeToModelPixel` shim).
    - [x] Remove the separate `AddCorner` ship-to-model flip workaround once the clean model already represents the shipped mapping.
    - [x] Log the full transform state (orientation, flips, mirror) in probe output.
    - [ ] Re-run the body-camera preset and confirm green markers overlay the legacy points under the same model used by the pack/march path.

- [ ] Raymarch data ingestion is decoupled from splat rendering.
  - Status: OPEN.
  - Evidence: the raymarch components read `DrawMeshInstanced.PointDepthBuffer`, `Intrinsics`, `FrameWidth`, `FrameHeight`, pose, and color. `DrawMeshInstanced.Update` returns early when `depthManager.show_spot` is false and still calls `Graphics.DrawMeshInstancedIndirect` when active.
  - Fix checklist:
    - [ ] Extract a dedicated frame/depth provider component or interface that owns frame fetch, CVD processing, pose, intrinsics, color texture, sequence, and lifetime.
    - [ ] Let `DrawMeshInstanced` and raymarching both consume that provider rather than making raymarching consume `DrawMeshInstanced`.
    - [ ] Add an explicit way to disable splat drawing while continuing to update depth data.
    - [ ] Ensure frozen/saved frames and live frames expose the same provider contract.
    - [ ] Keep resource ownership clear: provider owns buffers/textures; renderers borrow them.

- [x] Hit detection uses `_DepthEps` consistently, not only as a first-valid-sample fallback.
  - Status: FIXED IN CODE 2026-06-11. Synthetic/live coarse-step validation is still recommended.
  - Evidence (was): `MultiDepthRaymarch.compute` only accepted `abs(delta) < _DepthEps` when `prevValid` was false. Normal valid samples required a negative-to-positive crossing, so coarse steps could skip near-surface samples that did not bracket the zero crossing.
  - Resolution: both `March` and `MarchMulti` now accept `abs(delta) <= _DepthEps` at any valid sample, after preferring bracketed crossing refinement and rejecting epsilon hits across depth discontinuities.
  - Fix checklist:
    - [x] Treat `abs(delta) <= _DepthEps` as a hit for any valid sample unless a discontinuity rule rejects it.
    - [x] Prefer bracketed hit refinement when `prevValid && prevDelta < 0 && delta >= 0`.
    - [x] Keep discontinuity rejection edge-aware so epsilon hits do not reintroduce rubber-sheet smearing at silhouettes.
    - [ ] Add a debug mode or synthetic depth test for thin surfaces and coarse step counts.

- [x] Pixel-center convention is consistent across ray-gen, flips, and sampling. (R7, found 2026-06-10)
  - Status: FIXED IN CODE 2026-06-11. Validator/snap checks are still recommended.
  - Evidence (was): `MultiDepthRaymarch.compute` generated rays at `pix + 0.5` (half-integer centers), while `NativePixelTransform`/the model flips mirrored about `size-1` and depth/color reads truncated via `.Load((int)p)` (both integer-center conventions). Net ~0.5 px sampling bias.
  - Resolution: ray generation now uses integer-centered pixels (`pix`, with view rays mapped over `0..width-1`/`0..height-1`), and projected depth/color samples round to the nearest integer pixel before bounds checks and `.Load`.
  - Fix checklist:
    - [x] Pick one convention (integer centers match how the intrinsics were measured/validated in Step 0).
    - [x] Apply it to ray generation, flip mirrors, and texture sampling (round instead of truncate if integer-centered).
    - [ ] Re-run the validator and a snap-to-source identity check.

- [x] `EnsureRT` reuse accounts for sRGB/ReadWrite. (R8, found 2026-06-10)
  - Status: FIXED 2026-06-11.
  - Evidence (was): both drivers' `EnsureRT` compared only width/height/format before reusing an RT, so an RT created under a different `RenderTextureReadWrite` survived with stale sRGB state — the same bug class as the earlier double-decode darkening.
  - Resolution: `SingleCameraRaymarch`, `RaymarchCompositor`, and `MultiCameraRaymarch` now compare `rt.sRGB` against the requested read-write mode before reusing render textures/arrays.
  - Fix checklist:
    - [x] Include the requested read-write mode in the reuse comparison (`rt.sRGB` is the queryable state).

## Related Improvements Already Observed

- [x] Packed color render textures now request `RenderTextureReadWrite.Linear` in both `SingleCameraRaymarch` and `RaymarchCompositor`, which addresses the color darkening risk from sRGB decode on `Load`.
- [x] `RaymarchCompositor` now logs a steady-state heartbeat while debug logging is enabled, which makes `OnRenderImage` routing failures easier to diagnose.

## API Design Target

- [ ] Introduce a small `RaymarchDepthSource` or `IDepthFrameSource` abstraction.
  - It should expose: frame width/height, canonical camera model, native-to-upright mapping, depth buffer, color texture, sequence/timestamp, and ownership/lifetime expectations.
  - `SingleCameraRaymarch`, `RaymarchCompositor`, future multi-camera texture-array packing, and the legacy splat renderer should depend on this abstraction instead of duplicating `DrawMeshInstanced` setup assumptions.
