# Raymarch Review Checklist

Review date: 2026-06-09

Purpose: track the correctness, flexibility, and API-design issues found in the current Route A raymarching implementation. This file is separate from `RAYMARCH_ROUTE_A.md` so the route tracker can stay milestone-focused while this document tracks review debt.

## Current Status

- [ ] View-camera ray origin/range is correct.
  - Status: OPEN.
  - Evidence: `Assets/Raymarch/MultiDepthRaymarch.compute` reconstructs `nearW`/`farW`, sets `ro = nearW.xyz`, then marches with `t = _Near + stepSize * i`. That applies the near offset twice in view-camera mode.
  - Fix checklist:
    - [ ] Add an explicit `_ViewCameraWorldPos` or equivalent eye-origin input for view-camera rays.
    - [ ] In view-camera mode, set `ro` to the real eye/camera origin and use `farW - ro` for the ray direction.
    - [ ] Keep `t` as the single absolute march distance from `_Near` to `_Far`, or if `ro` remains `nearW`, start `t` at zero and adjust shading/range consistently.
    - [ ] Update both `SingleCameraRaymarch` and `RaymarchCompositor` to set the new shader input.
    - [ ] Validate with the snap-to-source identity test and by comparing against the splat cloud from a translated free camera.

- [ ] Compositing respects scene depth.
  - Status: OPEN.
  - Evidence: `MultiDepthRaymarch.compute` only samples `_SceneColor`; no scene depth texture, linearized scene depth, or output depth is used. Hits always blend over the color buffer when `_Composite` is enabled.
  - Fix checklist:
    - [ ] Enable depth texture generation on the compositor camera (`DepthTextureMode.Depth`) or route an equivalent render depth source.
    - [ ] Bind scene depth to the raymarch/composite path.
    - [ ] Compute each raymarch hit's view/camera depth in the compositor camera's space.
    - [ ] Compare hit depth against scene depth so real Unity geometry in front occludes raymarched reconstruction.
    - [ ] Decide whether to keep color-only compositing with depth rejection or move to a pass that can also write depth.
    - [ ] Add a validation scene with a simple Unity mesh in front of the depth reconstruction.

- [x] Orientation and flip handling uses one shared mapping contract.
  - Status: FIXED 2026-06-10 (see `RAYMARCH_ROUTE_A.md` R1 for the full analysis).
  - Evidence (was): `DepthCameraModelBuilder.BuildFor` supports `orientation`, `flipU`, and `flipV`, but `DepthToUpright.compute` only accepts `_BodyCamera` and hardcodes either Upright or Transpose+flipV. The drivers pass only `_BodyCamera` into the pack shader.
  - Fix checklist:
    - [x] Define a single native-to-model/model-to-native pixel transform type for orientation and flips — `NativePixelTransform` in `DepthCameraModelBuilder.cs` (also carries `mirroredNativeBuffer`, see note below).
    - [x] Use that transform in `DepthCameraModelBuilder` (`Build`/`BuildFor` overloads; renderer-free `Build` core).
    - [x] Replace `_BodyCamera` in `DepthToUpright.compute` with explicit orientation/flip parameters — `_Transpose`, `_FlipU`, `_FlipV`, `_MirrorNative`, bound by `NativePixelTransform.ApplyTo`.
    - [x] Update `SingleCameraRaymarch` and `RaymarchCompositor` so the pack pass receives the same transform used to build the model (single `PixelTransform` instance feeds both).
    - [x] Replace or extend `NativeToModelPixel` so it handles flips — `NativePixelTransform.NativeToModel`/`ModelToNative`; the flip-less static remains as a probe-only legacy shim (item 4).
    - [x] Add tests or editor validation cases for Upright, Transpose, Transpose+flipV — `CameraModelValidator.RunPixelTransformTests` covers all 8 orientation/flip combos (round-trip inverse + relabeling equivalence). Hand-camera mapping still needs live confirmation.
  - Resolution note: the analysis showed synced model+pack flips cancel exactly (pure relabeling), so the geometry-affecting part of the mapping is the native buffers' 180°-rotated storage (legacy CSMain's mirrored depth fetch; the legacy color shader mirrors identically). That is now explicit as `NativePixelTransform.mirroredNativeBuffer` (default ON), which also resolves the 2026-06-08 "flipU vs flipV" compositor discrepancy: the desync was producing an accidental net-180° mapping equal to this mirror.

- [ ] Camera-model live probe validates the same flipped model used at runtime.
  - Status: OPEN.
  - Evidence: `CameraModelLiveProbe.PresetBodyCameraAndSample` sets `flipV = true`, but `SampleAndCompare` builds `cleanModel` with `BuildFor(sourceRenderer, orientation)` and does not pass `flipU/flipV`. The per-sample `NativeToModelPixel` path also ignores flips.
  - Fix checklist (updated 2026-06-10 for the item-3 fix):
    - [ ] Build `cleanModel` from the shared `NativePixelTransform` (including `mirroredNativeBuffer` — the probe's green==gray comparison feeds the mirrored depth index by hand today, which is now the transform's job via `NativeToBufferPixel`).
    - [ ] Update per-sample native-to-model conversion to use `NativePixelTransform.NativeToModel` (then delete the flip-less `NativeToModelPixel` shim).
    - [ ] Remove the separate `AddCorner` ship-to-model flip workaround once the clean model already represents the shipped mapping.
    - [ ] Log the full transform state (orientation, flips, mirror) in probe output.
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

- [ ] Hit detection uses `_DepthEps` consistently, not only as a first-valid-sample fallback.
  - Status: OPEN.
  - Evidence: `MultiDepthRaymarch.compute` only accepts `abs(delta) < _DepthEps` when `prevValid` is false. Normal valid samples require a negative-to-positive crossing, so coarse steps can skip near-surface samples that do not bracket the zero crossing.
  - Fix checklist:
    - [ ] Treat `abs(delta) <= _DepthEps` as a hit for any valid sample unless a discontinuity rule rejects it.
    - [ ] Prefer bracketed hit refinement when `prevValid && prevDelta < 0 && delta >= 0`.
    - [ ] Keep discontinuity rejection edge-aware so epsilon hits do not reintroduce rubber-sheet smearing at silhouettes.
    - [ ] Add a debug mode or synthetic depth test for thin surfaces and coarse step counts.

- [ ] Pixel-center convention is consistent across ray-gen, flips, and sampling. (R7, found 2026-06-10)
  - Status: OPEN (LOW).
  - Evidence: `MultiDepthRaymarch.compute` generates rays at `pix + 0.5` (half-integer centers), while `NativePixelTransform`/the model flips mirror about `size-1` and depth/color reads truncate via `.Load((int)p)` (both integer-center conventions). Net ~0.5 px sampling bias.
  - Fix checklist:
    - [ ] Pick one convention (integer centers match how the intrinsics were measured/validated in Step 0).
    - [ ] Apply it to ray generation, flip mirrors, and texture sampling (round instead of truncate if integer-centered).
    - [ ] Re-run the validator and a snap-to-source identity check.

- [ ] `EnsureRT` reuse accounts for sRGB/ReadWrite. (R8, found 2026-06-10)
  - Status: OPEN (LOW).
  - Evidence: both drivers' `EnsureRT` compares only width/height/format before reusing an RT, so an RT created under a different `RenderTextureReadWrite` survives with stale sRGB state — the same bug class as the earlier double-decode darkening.
  - Fix checklist:
    - [ ] Include the requested read-write mode in the reuse comparison (`rt.sRGB` is the queryable state).

## Related Improvements Already Observed

- [x] Packed color render textures now request `RenderTextureReadWrite.Linear` in both `SingleCameraRaymarch` and `RaymarchCompositor`, which addresses the color darkening risk from sRGB decode on `Load`.
- [x] `RaymarchCompositor` now logs a steady-state heartbeat while debug logging is enabled, which makes `OnRenderImage` routing failures easier to diagnose.

## API Design Target

- [ ] Introduce a small `RaymarchDepthSource` or `IDepthFrameSource` abstraction.
  - It should expose: frame width/height, canonical camera model, native-to-upright mapping, depth buffer, color texture, sequence/timestamp, and ownership/lifetime expectations.
  - `SingleCameraRaymarch`, `RaymarchCompositor`, future multi-camera texture-array packing, and the legacy splat renderer should depend on this abstraction instead of duplicating `DrawMeshInstanced` setup assumptions.

