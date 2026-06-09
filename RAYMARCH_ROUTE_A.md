# Route A — Direct Multi-Depth-Map Ray Casting (Progress Tracker)

Living document. **Update this at every step** (status + changelog at the bottom).

## Goal
Replace the forward-projecting splat cloud (`DrawMeshInstanced` +
`PointcloudComputeShader.compute` + `InstancedIndirectColor.shader`) with a
**gather**-based renderer: cast one ray per output pixel from the VR eye and
intersect it against *all* available camera depth maps, shading each hit by
blending the contributing cameras' RGB. Software compute-shader march only
(no hardware ray tracing for now — see Decisions).

Route B (TSDF fusion + SDF ray march) is a later, separate effort.

## Status legend
`TODO` not started · `WIP` in progress · `DONE` complete · `BLOCKED` needs input

## Milestones
| # | Milestone | Status |
|---|-----------|--------|
| 0 | **Unify the camera model** — one canonical pinhole (C# + HLSL), provably-inverse project/unproject, validated. | DONE |
| 1 | Single-camera software march — one depth texture, write to RT, composite with depth. Match the splat cloud. | WIP |
| 2 | Multi-camera — depth/color `Texture2DArray` + `CameraGPU` buffer; all-camera surface test per sample. | TODO |
| 3 | Color blending — view-dependent weights (Unstructured Lumigraph style) + per-camera exposure compensation. | TODO |
| 4 | Acceleration — per-camera min/max depth pyramid (Hi-Z) + sphere-trace stepping. | TODO |
| 5 | VR perf — per-eye dispatch, reduced-res march + upsample, temporal reprojection to hit framerate. | TODO |

## Step 0 detail (current)
**Why first:** the legacy unprojection is transposed/axis-mismatched and mirror-tuned
(`PointcloudComputeShader.compute` CSMain: `pos.x=(row-cy)·z/fx`, `pos.y=(col-cx)·z/fy`,
plus a 180° mirror on the depth fetch). Ray casting needs the *forward* projection
(world → pixel,depth) to be the exact analytic inverse of the unprojection, and identical
across all cameras. So we define one clean model and make everything else conform to it.

**Canonical convention (source of truth):**
- Pixel `u` = column (right+), `v` = row (down+). `(cx, cy, fx, fy)` in pixels.
- Camera space: `+X` right, `+Y` down, `+Z` forward (into scene). `depth = Z_cam` (metres).
- `cameraToWorld` maps camera space → Unity world. `intrinsics` packed as `(cx, cy, fx, fy)`
  to match the existing `GetIntrinsicsVector()` order.

**Artifacts:**
- `Assets/Raymarch/DepthCameraModel.cs` — canonical C# model + factory + legacy reference.
- `Assets/Raymarch/CameraModel.hlsl` — GPU mirror (kept in lockstep with the C#).
- `Assets/Raymarch/CameraModelValidator.cs` — round-trip self-test (no scene/robot needed).
- `Assets/Raymarch/CameraModelLiveProbe.cs` — live reconciliation probe (Phase 1).
- `Assets/Scripts/DrawMeshInstanced.cs` — added read-only accessors (`PointDepthBuffer`,
  `FrameWidth`, `FrameHeight`, `Intrinsics`, `HasFrameData`) for the probe. No behavior change.

**Sub-tasks:**
- [x] Canonical pinhole math, C# (`DepthCameraModel`).
- [x] GPU mirror (`CameraModel.hlsl`).
- [x] Round-trip validator (`project∘unproject == identity`).
- [x] Phase 1: live probe that reproduces the legacy world point exactly and measures the clean
      (u*, v*, d*) that reproduces it via `ProjectFromWorld` (= the transpose/mirror correction).
- [x] Phase 2a: measured the correction on live `front_left_cloud` (640x480,
      intr cx,cy,fx,fy = 315.9, 244.7, 328.2, 327.1). Result is a **pure transpose**:
      `u* = row + (cx-cy)`, `v* = col - (cx-cy)`, depth unchanged. `(cx-cy)=71.2` exactly.
      Interpretation: Spot body cameras are mounted rotated 90° → native frames are transposed.
- [x] Phase 2b: baked it into `DepthCameraModelBuilder.BuildFor(renderer, ImageOrientation)`
      (`Transpose` swaps cx<->cy, fx<->fy, and frame dims; `Upright` for the hand camera).
      Added `CleanTransposed` verify candidate to the probe (green should overlay red).
- [x] Phase 2c (DONE 2026-06-08): live frustum-corner test confirmed the body-camera orientation is
      **Transpose + vertical flip** — a true 90° rotation (all four corners matched reality, left/right
      NOT mirrored). Baked into `DepthCameraModelBuilder.BuildForSpotBodyCamera` (and `BuildFor` gained
      `flipU`/`flipV`). Green==gray confirmed the transpose reproduces the live cloud.
      NOTE for Step 2: depth/color must be packed into the matching upright frame so
      `model.ProjectFromWorld` indexes the arrays correctly; the legacy 180° mirrored depth-fetch is a
      legacy-only quirk we are NOT carrying forward. Hand camera orientation (likely Upright) still to
      be confirmed when we first get a hand-camera frame.

**Step 0 is COMPLETE.** Canonical model = `DepthCameraModel` (C# + `CameraModel.hlsl`), built per camera
via `DepthCameraModelBuilder` (`BuildForSpotBodyCamera` for the 5 body cams; `Upright` for the hand cam).

## Step 1 detail (current) — single-camera march, debug overlay
First cut renders to a debug `RawImage` (user chose this over scene-integration); geometry-only,
**depth-shaded grayscale** so we validate the march geometry before entangling color orientation.

**Artifacts:**
- `Assets/Raymarch/DepthToUpright.compute` — packs native post-CVD depth into an upright RFloat RT
  (isolates the orientation mapping to one place; `_BodyCamera` flag picks Transpose+flipV vs Upright).
- `Assets/Raymarch/MultiDepthRaymarch.compute` — `March` kernel: view-camera ray per pixel, marches
  near→far, projects each sample via `CameraModel.hlsl`, detects surface crossing (sign change of
  `zcam - storedDepth`) or ε-band hit, shades by depth.
- `Assets/Raymarch/SingleCameraRaymarch.cs` — driver: builds the model, runs pack+march, shows on RawImage.

**Sub-tasks:**
- [x] Pack pass + march compute + driver (geometry-only, depth-shaded).
- [x] Validated geometry: depth march is upright, frame-filling (SourceCameraImage mode), 3D-consistent.
- [~] Add color: pack native RGB into an upright color RT (same orientation mapping as depth, since
      color/depth are pixel-aligned), shade hits with it. `colorFlipU`/`colorFlipV` toggles cover the
      color texture's own row/col convention — verify against the RGB and lock the flips.
- [ ] Then proceed to Step 2 (multi-camera texture arrays).

## Decisions & constraints
- **No hardware ray tracing (for now).** Confirmed by user. Keeps the renderer portable and
  needs no render-pipeline migration. Project is on the **built-in render pipeline** (no URP/HDRP
  in `Packages/manifest.json`), targeting **Meta XR / OpenXR**, with a Windows x86_64 native plugin
  (`Assets/Plugins/x86_64/`) → runs on a PC driving the headset.
- **Open question (does not block Step 0):** is the deployment target Quest-standalone-capable, or is
  PC/Link the permanent target? Affects milestone 5 (perf budget) and whether HW RT is revisited later.
- Depth is metric, stored per-pixel; only the depth channel of the CVD output is meaningful to the renderer.
- Cameras available: `BACK, FRONTLEFT, FRONTRIGHT, LEFT, RIGHT, HAND` (`SpotObserverClient.SpotCamera`).

## Key references
- McGuire & Mara, *Efficient GPU Screen-Space Ray Tracing*, JCGT 2014 — depth-buffer marching / Hi-Z.
- *Multi-Depth-Map Raytracing for Efficient Large-Scene Reconstruction* (IEEE TVCG) — closest analog.
- Buehler et al., *Unstructured Lumigraph Rendering*, SIGGRAPH 2001 — view-dependent color blending.

## Changelog
- 2026-06-04: Created tracker. Step 0 started: added `DepthCameraModel.cs`, `CameraModel.hlsl`,
  `CameraModelValidator.cs`. Canonical convention defined; round-trip self-test in place.
- 2026-06-04: Step 0 Phase 1 done: added `CameraModelLiveProbe.cs` + read-only accessors on
  `DrawMeshInstanced`. Probe reproduces the legacy world point exactly and measures the clean
  (u*, v*, d*) correction.
- 2026-06-04: Step 0 Phase 2a/2b: probe run on `front_left_cloud` showed a pure transpose
  (u*=row+(cx-cy), v*=col-(cx-cy), depth unchanged; cx-cy=71.2). Baked into
  `DepthCameraModelBuilder.BuildFor(renderer, ImageOrientation)` + a `CleanTransposed` verify
  candidate. Remaining: user visual confirmation the cloud isn't left-right mirrored (Phase 2c).
- 2026-06-04: Reworked probe visualization to persistent gizmo spheres (Debug.DrawLine was
  invisible) and added a color-coded frustum-corner orientation test for the handedness check.
- 2026-06-08: Gizmos were disabled in the Scene view (now on). Findings: (1) "green perpendicular
  to gray" means the probe instance was running `orientation=Upright` (Unity didn't pick up the
  Transpose default on the pre-existing component) — fix is to set Orientation=Transpose explicitly.
  (2) Frustum corners showed a pure VERTICAL flip (top<->bottom, left/right unchanged). Added
  `flipU`/`flipV` toggles to the probe (applied ship->model in the frustum). Recolored legacy
  dots gray to avoid clashing with the red top-right corner.
- 2026-06-08: Root-caused the lingering "green perpendicular": the probe's `candidate` field was
  stale-serialized to `Identity` (which ignores orientation and uses the raw native model). Added a
  one-click `PresetBodyCameraAndSample` context menu (sets candidate=CleanTransposed, Transpose, flipV).
  With it, green snapped onto gray and the frustum read correctly. **Step 0 closed**: baked the
  validated orientation into `BuildForSpotBodyCamera` / `BuildFor(..., flipU, flipV)`.
- 2026-06-08: Step 1 started (debug-overlay, geometry-only). Added `DepthToUpright.compute`,
  `MultiDepthRaymarch.compute`, `SingleCameraRaymarch.cs`. Depth-shaded march; surface detected by
  sign-change of (zcam - storedDepth).
- 2026-06-08: First run showed depth (march works!) but upside-down + left/right margins. Fixes:
  `_FlipV` (RWTexture2D top-left vs RawImage bottom-left display flip) and a `SourceCameraImage`
  ray mode that generates rays from the depth model itself (output sized to the camera's portrait
  resolution) to reproduce its own image and validate cleanly without view-camera FOV/aspect issues.
  ViewCamera mode now builds the projection with the output aspect. Next: confirm upright+filled,
  then switch to ViewCamera mode and move the camera to check 3D consistency vs the cloud.
- 2026-06-08: Geometry validated (looks good). Added color: `DepthToUpright` now also packs native
  RGB into an upright ARGB32 RT (`_HasColor`, `_ColorFlipU/V`); the march shades hits via `_UseColor`.
  Driver exposes `useColor` + color flip toggles.
- 2026-06-08: Color confirmed pixel-aligned (correct from the source viewpoint). Findings:
  (1) `_FlipV` was doing double duty — the model-ray and view-camera paths need opposite vertical
  flips. Fixed: the model path now flips relative to the view path in-shader, so `flipOutputV=OFF`
  is upright for BOTH modes (default changed to false). (2) Translating the view shows colors on
  wrong surfaces: this is the EXPECTED single-camera DIBR limit (parallax disocclusion + rubber-sheets
  across depth discontinuities), not a bug — rotation is fine. Real fix = multi-camera (Step 2) +
  edge-aware discontinuity handling (Step 4). Optional single-cam cleanup available now: hit
  refinement + depth-discontinuity skip (holes instead of smears).
- 2026-06-08: Added single-cam cleanup to `MultiDepthRaymarch`: (a) hit refinement — interpolate the
  zero-crossing between the two bracketing samples and reproject for an accurate hit pixel/depth;
  (b) `_DiscontinuityThreshold` — reject crossings where stored depth jumps (silhouette edges) and
  keep marching to the real surface behind (holes, not rubber-sheets). Driver exposes
  `discontinuityThreshold` (default 0.15m, 0 disables). Disocclusion gaps remain (fundamental to one
  camera) until Step 2.
- 2026-06-08: Hooked into the main camera (mono-first composite, user's choice). Added
  `RaymarchCompositor.cs` (OnRenderImage on the camera): packs + marches this camera's rays and
  composites over the rendered frame. Composite is done IN the march (`_Composite`/`_SceneColor`):
  on a miss it shows the scene, sampled at the ray row so it stays aligned under `_FlipV`. Built-in
  RP, mono matrices (`cam.projectionMatrix * worldToCameraMatrix`). CAVEAT: VR stereo runs
  OnRenderImage per eye but uses mono matrices here -> validate with XR off / a non-XR camera first;
  per-eye stereo is a later step. Marches at full screen res each frame (perf later).
- 2026-06-08: Added validation tooling to the compositor: `_HitBlend`/`marchBlend` (see-through
  reconstruction for overlay checks against the splat cloud) and a "Snap to source camera pose"
  context menu (identity test). Validation method: cross-check the march against the trusted splat
  cloud (gather vs scatter, both world-space) — set blend ~0.5, keep the cloud on, orbit a free
  TEST camera (not CenterEyeAnchor, which is head-tracked + stereo); the march must stay glued to the
  cloud from every angle. Main cam in scene = `CenterEyeAnchor` (Meta OVR rig; eyes render per-eye).
- 2026-06-08: Added `RaymarchCompositor` diagnostics (logs whether OnRenderImage fires + COMPOSITE
  vs PASSTHROUGH(reason) + heartbeat) to debug "no compositing" — likely the test camera isn't the
  displayed one (XR rig renders the view). Added `FlyCamera.cs` (legacy Input, Active Input Handling
  = Both) for WASD+QE/Space/Ctrl + RMB-look to fly the test camera during validation.
- 2026-06-08: Compositor working. Two findings: (1) Color was too dark — project is LINEAR
  (`m_ActiveColorSpace=1`) and `uprightColor` was a default(sRGB) RT, so the UAV-stored linear color
  was sRGB-decoded again on `.Load()`. Fixed: `uprightColor` now created `RenderTextureReadWrite.Linear`
  (both drivers). (2) ORIENTATION DISCREPANCY: to align the compositor to the cloud the user needed
  Transpose + **flipU** (NOT flipV, NOT flipOutputV) — contradicts the Step 0 frustum reading (flipV).
  The compositor-vs-cloud is the first independent world-orientation test (SourceCameraImage is
  self-consistent and can't detect a wrong flip; the Step 0 frustum was eyeballed amid display-flip
  confusion). PENDING: confirm flipU holds under multi-angle orbit + matches reality; if so, bake
  `flipU=true, flipV=false` into `BuildForSpotBodyCamera`/defaults (supersedes Step 0). If it drifts
  under orbit, a flip is masking a ray-direction bug — debug instead.
