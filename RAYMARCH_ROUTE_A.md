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

**Implementation walkthrough (CPU→GPU, with diagrams):** [RAYMARCH_IMPLEMENTATION.md](RAYMARCH_IMPLEMENTATION.md).

## Status legend
`TODO` not started · `WIP` in progress · `DONE` complete · `BLOCKED` needs input

## Milestones
| # | Milestone | Status |
|---|-----------|--------|
| 0 | **Unify the camera model** — one canonical pinhole (C# + HLSL), provably-inverse project/unproject, validated. | DONE |
| 1 | Single-camera software march — one depth texture, write to RT, composite with depth. Match the splat cloud. | WIP |
| 2 | Multi-camera — depth/color `Texture2DArray` + `CameraGPU` buffer; all-camera surface test per sample. | WIP |
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
      legacy-only quirk we are NOT carrying forward. **[SUPERSEDED 2026-06-10 by the R1 fix: the mirror
      is real data layout, not a quirk — the native buffers are stored 180°-rotated relative to their
      intrinsics grid, and the legacy color shader mirrors its UVs identically. It is now explicit as
      `NativePixelTransform.mirroredNativeBuffer` (default ON).]** Hand camera orientation (likely
      Upright) still to be confirmed when we first get a hand-camera frame.

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

## Step 2 detail (current) — multi-camera gather
Started 2026-06-10. Every output ray is tested against ALL cameras' upright depth maps; nearest
refined crossing wins. Step-2 color is a simple average of the cameras that agree they see the hit
(visibility check per camera); proper view-dependent weighting is Milestone 3.

**Design decisions:**
- Depth/color live in `Texture2DArray`s sized to the LARGEST upright frame (all 5 body cams are
  480x640 upright; the hand cam may differ). Smaller cameras zero-pad their slice; each camera's
  true resolution rides in the camera buffer so `InFrame` stays exact.
- Camera models go to GPU as `StructuredBuffer<CameraGPU>`; matrices passed as four explicit
  column `float4`s (transpose-reconstructed on GPU) to avoid Matrix4x4-vs-HLSL packing ambiguity.
- `MarchMulti` keeps per-camera bracketing state (prev delta/depth + validity bitmask,
  `MAX_CAMERAS = 8`); per-camera silhouette (discontinuity) rejection as in Step 1.
- Both view-camera kernels march from the TRUE eye origin (`_ViewEyePos`), with `t` as the
  single absolute march distance from `_Near` to `_Far` (R2 fixed 2026-06-11).
- Color visibility/agreement uses its OWN tolerance `_ColorAgreeEps`, SEPARATE from the geometric
  crossing `_DepthEps` and clamped `>= _DepthEps` in the driver (2026-06-11). The reprojected+rounded
  hit point carries more error than crossing detection, so sharing one eps means tightening geometry
  starves the color test and every hit falls back to magenta. NOT a regression — it's a deliberate
  decouple; the binary accept/reject goes away with Step 3's soft lumigraph weights. Default 0.1.
- Single-camera paths (March kernel, both Step 1 drivers) stay untouched as the validation
  reference. The multi driver is a new component: `MultiCameraRaymarch` (OnRenderImage compositor
  style), one entry per source camera with its own `NativePixelTransform` + color flips.

**Sub-tasks:**
- [x] a) Pack pass `PackArray` kernel: one camera -> one texture-array slice through the shared
      `NativePixelTransform` mapping; full-slice dispatch zeroes the padding (`DepthToUpright.compute`).
- [x] b) `CameraGPU` struct + `_Cameras` buffer + decode-to-`CameraModel` helper (`MultiDepthRaymarch.compute`).
- [x] c) `MarchMulti` kernel: all-camera surface test per sample (per-camera bracketing state +
      validity bitmask), nearest-hit refinement, per-camera silhouette rejection, agreement-averaged
      color (magenta debug for unconfirmed hits), grayscale = eye distance.
- [x] d) `MultiCameraRaymarch` driver: source list, array allocation at max upright dims, per-source
      pack dispatch, camera-buffer upload, march + composite, diagnostics + snap-to-source menu.
- [ ] e) Live validation (user): two overlapping cameras first (FL+FR) — reconstructions must agree in
      the overlap (no double walls/seams) and match the splat cloud under orbit; then all 5 body cams.
- [ ] f) Hand camera: add as an `Upright` source once a live frame confirms its transform.

## Review items (correctness & design debt)
From `RAYMARCH_REVIEW_CHECKLIST.md` (2026-06-09). All six claims were verified against the code and
found valid (R5 partial — see note). Fix-checklists/detail live in that file; tracked here as work,
in priority order.

- [x] **R1 (HIGH) — single orientation/flip mapping contract.** FIXED 2026-06-10. One serializable
  `NativePixelTransform` (orientation + flipU/flipV + `mirroredNativeBuffer`) now drives BOTH the model
  build (`DepthCameraModelBuilder.Build/BuildFor`) and the pack pass (`DepthToUpright.compute`, bound
  via `ApplyTo`; `_BodyCamera` replaced by `_Transpose/_FlipU/_FlipV/_MirrorNative`). Both drivers pass
  the same instance to both. KEY INSIGHT from the analysis (resolves the 2026-06-08 flipU-vs-flipV
  PENDING item): when model and pack flips are synced they cancel exactly — flips are pure relabelings
  and CANNOT change world geometry. What changes geometry is the legacy buffers' 180°-rotated storage
  (CSMain's mirrored fetch `W*(H-row-1)+(W-col-1)`; the legacy color shader mirrors identically), now
  explicit as `mirroredNativeBuffer` (default ON). The user's "Transpose+flipU aligns, flipV doesn't"
  observation was the old desync (model flipU × hardcoded pack flipV = net 180° = exactly this mirror);
  the new default (Transpose, flipV, mirror ON) produces mathematically identical geometry to that
  validated config. `CameraModelValidator` gained contract tests (mapping round-trip inverse +
  relabeling equivalence, all orientation/flip combos). Color cuv flips stay a separate native
  texture-origin compensation; validated values unaffected (mirror moves depth and color together).
  TO VERIFY live: orbit vs the splat cloud should still align with defaults (one quick check).
- [x] **R2 (HIGH) — view-camera ray double near-offset.** FIXED IN CODE 2026-06-11. `March`
  now matches `MarchMulti`: view-camera rays start at `_ViewEyePos`, direction comes from the far-plane
  point, and `t` is the single absolute distance over `[_Near,_Far]`. `SingleCameraRaymarch` and
  `RaymarchCompositor` both bind `_ViewEyePos`. Live snap-to-source/orbit validation remains recommended.
  (Checklist item 1.)
- [x] **R3 (HIGH) — compositing ignores scene depth.** FIXED IN CODE 2026-06-11.
  `RaymarchCompositor` and `MultiCameraRaymarch` request `DepthTextureMode.Depth`, bind
  `_CameraDepthTexture`, pass the view matrix + `_ZBufferParams`, and enable `_UseSceneDepth` when
  compositing. `March`/`MarchMulti` compare each hit's Unity eye depth against linearized scene depth
  and leave the scene color when real Unity geometry is closer. This remains color-only compositing
  with depth rejection; no depth buffer is written. Mesh-in-front validation remains pending.
  (Checklist item 2.)
- [x] **R4 (MED) — probe must validate the flipped runtime model.** FIXED IN CODE 2026-06-11.
  `CameraModelLiveProbe` now uses the same `NativePixelTransform` contract as runtime, including
  `mirroredNativeBuffer`: it builds `cleanModel` from the transform, fetches sample depth through
  `NativeToBufferPixel`, maps samples through `NativeToModel`, logs the full transform, and draws
  frustum corners directly through the flipped clean model. The old flip-less `NativeToModelPixel`
  shim was deleted. Live body-camera preset confirmation remains pending. (Item 4.)
- [x] **R5 (MED, partial) — `_DepthEps` hit consistency.** FIXED IN CODE 2026-06-11. Both
  `March` and `MarchMulti` now accept `|delta| <= eps` for any valid sample after preferring bracketed
  crossing refinement, and epsilon hits are rejected across depth discontinuities so silhouette holes are
  preserved. A synthetic/coarse-step debug test is still useful. (Checklist item 6.)
- [ ] **R6 (REFACTOR) — decouple ingestion from the splat renderer.** Raymarch consumes
  `DrawMeshInstanced` directly, and its `Update` can't refresh depth without also drawing splats.
  Introduce an `IDepthFrameSource`/`RaymarchDepthSource` (frame size, canonical model, native↔upright
  mapping, depth buffer, color, sequence, ownership) consumed by BOTH the splat renderer and raymarch;
  add a "depth without splat draw" switch; keep buffer ownership in the provider. (Checklist item 5 +
  API-design target.)

- [x] **R7 (LOW) — pixel-center convention is inconsistent (half-pixel bias).** FIXED 2026-06-11,
  REVISED 2026-06-11. KEY DISTINCTION (cost a real misalignment first): there are TWO independent
  pixel grids and they must NOT share a convention. (a) The DEPTH-camera model grid (world→depth
  pixel projection + array sampling) is integer-centered — principal point at integer `u=cx`; fixed
  by rounding projected pixels (`CameraModel_RoundPixel`) before `.Load`. (b) The VIEW-camera ray
  grid (`PixelToNdc`) must follow Unity's raster convention `(i+0.5)/res`, matching
  `cam.projectionMatrix` and where the splat cloud rasterizes via `UNITY_MATRIX_VP` — that is what
  the reconstruction is overlaid against. The first revision wrongly made the VIEW grid
  integer-centered too (`pix/(W-1)`), which biased rays up to ~½ px toward the image edges (zero at
  center) and pulled the march off the cloud near the borders. Now: depth grid integer-centered +
  round; view grid half-pixel. The `_RayFromModel` validation path correctly stays integer-centered
  (it samples the depth model itself, not a Unity camera). Validator + snap-to-source checks remain
  pending.
- [x] **R8 (LOW) — `EnsureRT` reuse check ignores sRGB/ReadWrite.** FIXED 2026-06-11.
  `SingleCameraRaymarch`, `RaymarchCompositor`, and `MultiCameraRaymarch` now compare `rt.sRGB` against
  the requested `RenderTextureReadWrite` mode before reusing RTs/texture arrays, preventing stale color
  read/write state after configuration changes.

Done already (from the checklist's "observed" section): linear packed-color RTs (sRGB double-decode
fix) and the compositor heartbeat log.

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
- 2026-06-09: Reviewed `RAYMARCH_REVIEW_CHECKLIST.md`. Verified all six claims against the code: all
  valid (R5 partial). Added tracked items R1–R6 under "Review items". Notable: R1 (pack pass ignores
  `flipU/flipV` while the model honors them → possible data/model desync) may be entangled with the
  open orientation reconciliation, and R2 (view-camera ray starts `_Near` beyond the near plane) is a
  real ray-origin bug masked by the self-consistent identity test.
- 2026-06-10: **R1 fixed** — introduced `NativePixelTransform` (orientation + labeling flips +
  `mirroredNativeBuffer`) as the single native↔model mapping contract, used by the builder
  (`DepthCameraModelBuilder.Build/BuildFor`), the pack pass (`DepthToUpright.compute`; `_BodyCamera`
  → `_Transpose/_FlipU/_FlipV/_MirrorNative` via `ApplyTo`), and both drivers (new
  `mirrorNativeBuffer` field, default ON). Analysis resolved the 2026-06-08 PENDING flip discrepancy:
  synced model+pack flips cancel (pure relabeling, geometry-invariant), so the only
  geometry-affecting term is the legacy buffers' 180°-rotated storage — previously dismissed as a
  "legacy-only quirk", actually real data layout (legacy CSMain mirrors its depth fetch AND
  `InstancedIndirectColor` mirrors its color UV the same way). The user's working "flipU" compositor
  config was the desync accidentally reproducing that mirror; defaults (Transpose, flipV, mirror ON)
  now give identical geometry by construction. Added `CameraModelValidator` contract tests
  (round-trip + relabeling equivalence). Existing scene components pick up `mirrorNativeBuffer=true`
  automatically (new serialized field default). Pending: one quick orbit-vs-cloud visual confirm.
  Flagged two new review items while in the code: R7 (half-pixel center convention mismatch), R8
  (`EnsureRT` ignores sRGB/ReadWrite on reuse).
- 2026-06-10: **Step 2 (multi-camera) implementation landed** (sub-tasks a–d; see Step 2 detail).
  `DepthToUpright.compute` gained `PackArray` (one camera per `Texture2DArray` slice, shared
  `NativePixelTransform` mapping refactored into helpers, full-slice dispatch zeroes padding).
  `MultiDepthRaymarch.compute` gained `CameraGPU` (`StructuredBuffer`, matrices as explicit columns
  to dodge Matrix4x4/HLSL packing ambiguity, `MAX_CAMERAS=8`) and `MarchMulti` (per-sample
  all-camera test with per-camera bracketing state, nearest refined crossing wins, per-camera
  discontinuity rejection, agreement-averaged color with per-camera occlusion check; marches from
  the true eye origin `_ViewEyePos`, so the multi path is born WITHOUT the R2 near-offset bug —
  at that point R2 remained open for the single-cam `March` kernel only; fixed 2026-06-11).
  New `MultiCameraRaymarch` driver
  (OnRenderImage compositor): per-source orientation/flips/mirror + color flips, slices sized to
  the largest upright frame, camera buffer rebuilt per frame, heartbeat diagnostics, snap-to-source
  context menu. Single-camera paths untouched (validation reference). NEXT: live validation (e) —
  two overlapping cameras (FL+FR), overlap must agree (no double walls) and match the cloud under
  orbit; then all 5 body cams; hand camera (f) once its transform is confirmed.
- 2026-06-11: Closed three bounded review gaps in code. R2: single-camera `March` now starts
  view-camera rays at `_ViewEyePos` and both single-camera drivers bind that input. R5: epsilon-band
  hit detection now applies to any valid sample in both `March` and `MarchMulti`, while bracketed
  refinement still wins and depth discontinuities still reject hits. R8: RT/texture-array reuse now
  checks the requested sRGB/read-write state in all raymarch drivers. Pending validation: snap/orbit
  check for R2 and a coarse-step/synthetic case for R5.
- 2026-06-11: Closed R3/R4/R7 in code. R3: compositor drivers now request/bind Unity scene depth and
  `March`/`MarchMulti` reject hits behind closer scene geometry using linearized eye depth (color-only
  rejection, no depth write). R4: `CameraModelLiveProbe` now uses the runtime `NativePixelTransform`
  end-to-end, including native-buffer mirror, and the old flip-less pixel shim was removed. R7: ray
  generation and projected texture reads now consistently use integer pixel centers, with projected
  samples rounded before load. Pending validation: mesh-in-front test, live body-camera probe preset,
  validator/snap checks.
- 2026-06-11: R7 correction — the prior fix over-unified the pixel grids and made the VIEW-camera ray
  grid integer-centered (`pix/(W-1)`), causing a sub-pixel reconstruction-vs-cloud misalignment that
  grew toward the image edges (zero at center). Root cause: the view ray belongs to the Unity test
  camera and must use the raster half-pixel convention `(i+0.5)/res` (matching `cam.projectionMatrix`
  and the splat cloud's `UNITY_MATRIX_VP` rasterization); it is a SEPARATE grid from the
  integer-centered depth model (which keeps `RoundPixel` sampling). Reverted `PixelToNdc` to
  half-pixel; depth-side rounding unchanged. Affects both `March` and `MarchMulti` (view path only).
- 2026-06-11: Splat billboard is now a true per-point camera-facing quad (`InstancedIndirectColor`
  builds its basis from `_WorldSpaceCameraPos`), replacing the uniform yaw-only `_BillboardRight/Up`
  + `angle` matrix (now unused in C#). Reason: the old cloud only faced one fixed yaw, so when viewed
  from a different camera the flat cards projected off the true surface and read as "doubles" vs the
  geometrically-exact march — an unfaithful reference, not a march bug. Now the cloud faces whatever
  camera renders (test cam, and per-eye for the XR rig), so it's a valid comparison from any angle.
- 2026-06-11: Split the multi-cam color visibility tolerance into `_ColorAgreeEps`, separate from the
  geometric `_DepthEps` and clamped `>= _DepthEps` in `MultiCameraRaymarch` (default 0.1). Symptom
  that prompted it: tightening `depthEps` to clean up geometry pushed the color agreement test (which
  had reused `_DepthEps`) too strict, so confirmed hits collapsed to the magenta "no camera agrees"
  fallback everywhere. Deliberate decouple, not a regression — superseded by Step 3's soft weights.
