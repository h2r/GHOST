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

- [ ] Orientation and flip handling uses one shared mapping contract.
  - Status: OPEN.
  - Evidence: `DepthCameraModelBuilder.BuildFor` supports `orientation`, `flipU`, and `flipV`, but `DepthToUpright.compute` only accepts `_BodyCamera` and hardcodes either Upright or Transpose+flipV. The drivers pass only `_BodyCamera` into the pack shader.
  - Fix checklist:
    - [ ] Define a single native-to-model/model-to-native pixel transform type for orientation and flips.
    - [ ] Use that transform in `DepthCameraModelBuilder`.
    - [ ] Replace `_BodyCamera` in `DepthToUpright.compute` with explicit orientation/flip parameters or a compact transform matrix.
    - [ ] Update `SingleCameraRaymarch` and `RaymarchCompositor` so the pack pass receives the same transform used to build the model.
    - [ ] Replace or extend `NativeToModelPixel` so it handles flips, not just transpose.
    - [ ] Add tests or editor validation cases for Upright, Transpose, Transpose+flipV, and any hand-camera mapping once confirmed.

- [ ] Camera-model live probe validates the same flipped model used at runtime.
  - Status: OPEN.
  - Evidence: `CameraModelLiveProbe.PresetBodyCameraAndSample` sets `flipV = true`, but `SampleAndCompare` builds `cleanModel` with `BuildFor(sourceRenderer, orientation)` and does not pass `flipU/flipV`. The per-sample `NativeToModelPixel` path also ignores flips.
  - Fix checklist:
    - [ ] Build `cleanModel` with `BuildFor(sourceRenderer, orientation, flipU, flipV)`.
    - [ ] Update per-sample native-to-model conversion to use the shared orientation/flip transform.
    - [ ] Remove the separate `AddCorner` ship-to-model flip workaround once the clean model already represents the shipped mapping.
    - [ ] Log the full orientation and flip state in probe output.
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

## Related Improvements Already Observed

- [x] Packed color render textures now request `RenderTextureReadWrite.Linear` in both `SingleCameraRaymarch` and `RaymarchCompositor`, which addresses the color darkening risk from sRGB decode on `Load`.
- [x] `RaymarchCompositor` now logs a steady-state heartbeat while debug logging is enabled, which makes `OnRenderImage` routing failures easier to diagnose.

## API Design Target

- [ ] Introduce a small `RaymarchDepthSource` or `IDepthFrameSource` abstraction.
  - It should expose: frame width/height, canonical camera model, native-to-upright mapping, depth buffer, color texture, sequence/timestamp, and ownership/lifetime expectations.
  - `SingleCameraRaymarch`, `RaymarchCompositor`, future multi-camera texture-array packing, and the legacy splat renderer should depend on this abstraction instead of duplicating `DrawMeshInstanced` setup assumptions.

