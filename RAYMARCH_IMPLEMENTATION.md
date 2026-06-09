# Route A Ray Marching — Implementation Walkthrough

A detailed tour of the direct multi-depth-map ray-cast renderer, from the CPU
orchestration down through the GPU compute kernels. Companion to the progress
tracker in [RAYMARCH_ROUTE_A.md](RAYMARCH_ROUTE_A.md).

> Status: single-camera (Step 1) + main-camera composite. Multi-camera (Step 2)
> and per-eye stereo are not implemented yet; this doc describes what exists today.

## Contents
1. [Big picture](#1-big-picture)
2. [The cast (files)](#2-the-cast-files)
3. [CPU side](#3-cpu-side)
   - [3.1 Inputs from the robot pipeline](#31-inputs-from-the-robot-pipeline)
   - [3.2 The canonical camera model](#32-the-canonical-camera-model-depthcameramodelcs)
   - [3.3 Building the upright model](#33-building-the-upright-model-depthcameramodelbuildercs)
   - [3.4 Per-frame orchestration](#34-per-frame-orchestration-raymarchcompositorcs)
4. [GPU side](#4-gpu-side)
   - [4.1 Shared camera math](#41-shared-camera-math-cameramodelhlsl)
   - [4.2 Pass 1: pack to upright](#42-pass-1-pack-to-upright-depthtouprightcompute)
   - [4.3 Pass 2: the march](#43-pass-2-the-march-multidepthraymarchcompute)
5. [Coordinate conventions & the orientation story](#5-coordinate-conventions--the-orientation-story)
6. [Color space](#6-color-space)
7. [Limitations & what's next](#7-limitations--whats-next)

---

## 1. Big picture

We render the scene by **gathering**: for every output pixel we cast a ray from the
view camera and ask "what surface, seen by the depth camera, does this ray hit, and
what color is it?" — the opposite of the old splat cloud, which **scatters** each
depth pixel into the world.

```
 SpotObserverClient  (native camera frames; up to 6 cameras)
        │   borrows: depth buffer, color texture, intrinsics, pose
        ▼
 DrawMeshInstanced  (one per camera)
        │   CVD-processed depth  ─▶  PointDepthBuffer : float3  (.z = metric depth)
        │   colorImage : Texture2D,  Intrinsics (cx,cy,fx,fy),  GetCurrentPose()
        ▼
 RaymarchCompositor.OnRenderImage(src, dst)        ── CPU, per frame, on the view camera ──
        │
        ├─ DepthCameraModelBuilder.BuildFor(...)  ─▶  DepthCameraModel  (upright pinhole)
        │
        ├─ PASS 1  DepthToUpright.compute / "Pack"
        │       PointDepthBuffer ─▶ uprightDepth  (RFloat)
        │       colorImage       ─▶ uprightColor  (ARGB32, linear)
        │
        ├─ PASS 2  MultiDepthRaymarch.compute / "March"
        │       per output pixel: build ray ▸ march near→far ▸ project each sample
        │       into the depth camera ▸ detect surface ▸ shade ▸ composite over src
        │       ─▶ composited  (ARGBFloat)
        │
        └─ Graphics.Blit(composited, dst)  ─▶  the camera image you see
```

Two GPU passes per frame: **Pack** (cheap reformat) then **March** (the work).

---

## 2. The cast (files)

| File | Side | Role |
|------|------|------|
| [DepthCameraModel.cs](Assets/Raymarch/DepthCameraModel.cs) | CPU | Canonical pinhole: intrinsics + extrinsics + project/unproject. Single source of truth. |
| [CameraModel.hlsl](Assets/Raymarch/CameraModel.hlsl) | GPU | Exact GPU mirror of the model's math. |
| [DepthCameraModelBuilder.cs](Assets/Raymarch/DepthCameraModelBuilder.cs) | CPU | Builds the *upright* model for a camera (folds away the 90° body-camera mounting). |
| [DepthToUpright.compute](Assets/Raymarch/DepthToUpright.compute) | GPU | Pass 1: native depth/color → upright textures. |
| [MultiDepthRaymarch.compute](Assets/Raymarch/MultiDepthRaymarch.compute) | GPU | Pass 2: the ray march + shading + composite. |
| [RaymarchCompositor.cs](Assets/Raymarch/RaymarchCompositor.cs) | CPU | Drives both passes from a camera's `OnRenderImage` and composites into the view. |
| [SingleCameraRaymarch.cs](Assets/Raymarch/SingleCameraRaymarch.cs) | CPU | Debug driver: renders to a RawImage instead of compositing (validation). |
| [FlyCamera.cs](Assets/Raymarch/FlyCamera.cs) | CPU | WASD fly-cam for validating from arbitrary viewpoints. |
| [CameraModelLiveProbe.cs](Assets/Raymarch/CameraModelLiveProbe.cs) | CPU | Step 0 tool: measured/validated the camera-model orientation. |

---

## 3. CPU side

### 3.1 Inputs from the robot pipeline

Each depth camera is represented by a `DrawMeshInstanced` component (the old splat
renderer), which already owns everything we need and now exposes it read-only
([DrawMeshInstanced.cs:468-473](Assets/Scripts/DrawMeshInstanced.cs#L468-L473)):

- `PointDepthBuffer` — `ComputeBuffer` of `float3`, indexed `col + row*width`, where
  `.z` is the CVD-processed **metric depth** (camera-space Z). (`.x/.y` are unused here.)
- `colorImage` — the camera's RGB `Texture2D`.
- `Intrinsics` — `(cx, cy, fx, fy)` in pixels.
- `FrameWidth` / `FrameHeight` — native frame size (e.g. 640×480).
- `GetCurrentPose()` — the camera's world transform (`Matrix4x4.TRS` of the GameObject).
- `HasFrameData` — whether a frame has arrived yet.

### 3.2 The canonical camera model ([DepthCameraModel.cs](Assets/Raymarch/DepthCameraModel.cs))

A plain pinhole, the single source of truth for the geometry. **Conventions:**

- Pixel `u` = column (right+), `v` = row (down+); `(cx,cy,fx,fy)` in pixels.
- Camera space: `+X` right, `+Y` down, `+Z` forward (into the scene); `depth = Z_cam`.
- `cameraToWorld` maps camera space → Unity world; `worldToCamera` is its inverse.

```
        camera space (+X right, +Y down, +Z forward)
                  Zc  (depth, into the scene)
                   ▲          • X = (Xc, Yc, Zc)
                   │         /
                   │        /
                   │       /
         ──────────●──────────────▶  image plane (Zc = 1)
                cam origin

  project   :  u = fx·Xc/Zc + cx ,  v = fy·Yc/Zc + cy ,  depth = Zc
  unproject :  Xc = (u-cx)·d/fx ,  Yc = (v-cy)·d/fy ,  Zc = d        (given depth d)
```

`ProjectFromCamera` and `UnprojectToCamera` are **exact analytic inverses** for any
`Zc > 0` — verified by round-trip in [CameraModelValidator.cs](Assets/Raymarch/CameraModelValidator.cs).
The march relies on this: it unprojects to build rays and projects to look up depth.

### 3.3 Building the upright model ([DepthCameraModelBuilder.cs](Assets/Raymarch/DepthCameraModelBuilder.cs))

Spot's body cameras are **mounted rotated 90°**, so their native frames are sideways.
Rather than scatter that quirk through the renderer, the builder folds it into the
model so everything downstream sees a clean upright pinhole.

`BuildFor(renderer, orientation, flipU, flipV)`:

1. **Transpose** (body camera): swap the intrinsics axes and the frame dimensions —
   `fx↔fy`, `cx↔cy`, `width↔height`. (Upright/hand camera: leave as-is.)
2. **Flips**: a `flipU`/`flipV` mirrors the principal point on that axis **and negates
   the focal length** (`fx → -fx`). A negative focal encodes the reflection and is
   exactly equivalent to sampling the mirrored pixel `(width-1-u)`.

```
 NATIVE  (sideways, 640×480)              UPRIGHT model  (480×640)
   col ─────────────▶                       u ───────▶
 ┌──────────────────┐ row               ┌──────────┐ v
 │                  │  │                 │          │  │
 │     sideways     │  ▼                 │  upright │  ▼
 └──────────────────┘                    │          │
                                         └──────────┘
   transpose + flip  ⇒  the depth/color data is re-sampled into the
   upright frame in Pass 1 so model.Project(u,v) indexes it directly.
```

> The exact flip combination for the body cameras is being finalized against the live
> view (see the orientation note in the tracker); the builder is parameterized so it's
> a one-line change once locked.

### 3.4 Per-frame orchestration ([RaymarchCompositor.cs](Assets/Raymarch/RaymarchCompositor.cs))

Attached to the camera you look through. Built-in render pipeline, so it hooks
`OnRenderImage(src, dst)` — called after the camera renders, with `src` = the rendered
frame and `dst` = where we must write the final image.

Each frame ([RaymarchCompositor.cs:76+](Assets/Raymarch/RaymarchCompositor.cs#L76)):

```
OnRenderImage(src, dst):
    if !Ready():  Graphics.Blit(src, dst); return     // passthrough; logs why (debug)
    model = DepthCameraModelBuilder.BuildFor(sourceRenderer, orientation, flipU, flipV)
    ensure RTs:  uprightDepth (RFloat, model size)
                 uprightColor (ARGB32 LINEAR, model size)   ← linear: see §6
                 composited   (ARGBFloat, screen size)
    ── PASS 1 ──  set Pack params; bind PointDepthBuffer, colorImage; Dispatch(Pack)
    ── PASS 2 ──  invVP = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse
                  set March params (model intrinsics/matrices, near/far/steps/eps…)
                  bind uprightDepth, uprightColor, src(=scene); Dispatch(March) → composited
    Graphics.Blit(composited, dst)
```

Key choices:
- **Real camera matrices.** `invVP` is built from the camera's actual
  `projectionMatrix * worldToCameraMatrix`, so the rays match exactly what the camera
  renders — no FOV/aspect fudging.
- **Compositing is done inside the march** (not a separate blend shader): the march
  samples the scene where a ray misses, so the reconstruction stays pixel-aligned with
  the rendered frame and we only do one `Blit`.
- **Diagnostics.** A throttled log reports whether `OnRenderImage` fires and whether it
  took the `COMPOSITE` or `PASSTHROUGH(reason)` path — invaluable when a camera isn't
  the one actually drawing the screen.

`SingleCameraRaymarch.cs` is the same two passes but writes to a debug `RawImage`, and
adds a `SourceCameraImage` ray mode (rays from the model itself) for validation.

---

## 4. GPU side

### 4.1 Shared camera math ([CameraModel.hlsl](Assets/Raymarch/CameraModel.hlsl))

A `struct CameraModel { float4 intrinsics; float2 resolution; float4x4 cameraToWorld,
worldToCamera; }` plus free functions that mirror the C# model **exactly**:

- `CameraModel_UnprojectToWorld(c, uv, depth)` → world point.
- `CameraModel_ProjectFromWorld(c, world)` → `(u, v, zcam)`.
- `CameraModel_InFrame(c, uv)` → bounds test.

Keeping this in lockstep with `DepthCameraModel.cs` is what lets the CPU validation
(probe, round-trip) certify the GPU march.

### 4.2 Pass 1: pack to upright ([DepthToUpright.compute](Assets/Raymarch/DepthToUpright.compute))

One thread per **upright** pixel. It resamples the native depth (and color) into the
upright frame so the march can treat depth as a clean image and sample with hardware
texture units (and, later, filtering/mips).

For a body camera the mapping ([DepthToUpright.compute:42-61](Assets/Raymarch/DepthToUpright.compute#L42-L61)) is the inverse of the builder's transpose+flip:

```
 upright (u, v)  ⇒  native (col = (nativeWidth-1) - v,  row = u)
 idx = col + row * nativeWidth
 uprightDepth[u,v] = PointDepth[idx].z
```

Color is sampled at the **same native pixel** (depth and color are pixel-aligned from
the same camera), through a sampler so it works on compressed textures; `_ColorFlipU/V`
cover the color texture's own row/column convention
([DepthToUpright.compute:63-75](Assets/Raymarch/DepthToUpright.compute#L63-L75)).

Why a separate pass? It quarantines the per-camera orientation quirk in **one place**.
After Pass 1, the march is pure standard-pinhole with zero special-casing — which is
also exactly what Step 2 needs when these become `Texture2DArray` slices.

### 4.3 Pass 2: the march ([MultiDepthRaymarch.compute](Assets/Raymarch/MultiDepthRaymarch.compute))

One thread per **output** pixel. Three phases: build a ray, march it, shade the hit.

**(a) Ray generation** ([:63-96](Assets/Raymarch/MultiDepthRaymarch.compute#L63-L96)). Two modes:

- *View camera* (real use): unproject the pixel's NDC through `_InvViewProj` at the near
  and far planes, ray = `normalize(far - near)`, origin = near point.
- *Model* (`_RayFromModel`, validation): rays come from the depth model itself, so the
  output reproduces that camera's own image — used to sanity-check the plumbing.

A `_FlipV` term handles the bottom-left-origin RawImage vs top-left compute-write
convention; the two ray modes have opposite vertical conventions, so the model path
flips relative to the view path (off by default = both upright).

**(b) The march loop** ([:111-170](Assets/Raymarch/MultiDepthRaymarch.compute#L111-L170)). Step `t` from near to far. At each sample `X = ro + dir·t`:

1. Project `X` into the depth camera → `(u, v, zcam)`. Skip if behind (`zcam ≤ 0`) or
   out of frame.
2. Read stored depth `D = uprightDepth.Load(u, v)`. Skip if invalid (`≤ minValidDepth`).
3. `delta = zcam − D`. The surface is where `delta` changes sign from negative (the
   sample is *in front of* the stored surface) to non-negative (*behind* it):

```
 view ray ──●────●────●────●────●────●──▶ t
            X0   X1   X2   X3   X4   X5
 depth-map surface:                  ________
                                    /
 delta = zcam − D :   −    −    −    −    +    +
                                       ▲
                       sign flip  ⇒  surface crossed between X3 and X4
```

**(c) Discontinuity skip** ([:139-145](Assets/Raymarch/MultiDepthRaymarch.compute#L139-L145)). A single depth map is a continuous height-field, so a naive crossing at a silhouette edge "rubber-sheets" foreground color onto background. If the stored depth **jumps** across the crossing (`|D − prevD| > _DiscontinuityThreshold`), we reject the hit and keep marching — leaving a hole rather than a smear, and letting the ray reach the real surface behind the edge:

```
   foreground ──┐
                │   |D_cur − D_prev| > threshold  ⇒  REJECT crossing, keep marching
                └──────────── background           (hole, not a stretched "skin")
```

**(d) Hit refinement** ([:147-156](Assets/Raymarch/MultiDepthRaymarch.compute#L147-L156)). The coarse step brackets the surface; we linearly interpolate the exact zero-crossing
`frac = prevDelta / (prevDelta − delta)`, recompute the hit point, and reproject so the
color is sampled at the accurate pixel (much less edge bleed).

**(e) Shading & compositing** ([:48-55](Assets/Raymarch/MultiDepthRaymarch.compute#L48-L55), [:98-102](Assets/Raymarch/MultiDepthRaymarch.compute#L98-L102), [:155](Assets/Raymarch/MultiDepthRaymarch.compute#L155)).
- `Shade()` returns sampled RGB (`_UseColor`) or a depth-grayscale (near = bright).
- The pixel's **background** is the scene (`_SceneColor`, for compositing) or black.
- The hit is blended over the background with `_HitBlend` (1 = opaque; <1 = see-through,
  for overlaying against the splat cloud during validation).
- On a miss, the background (scene) shows through — that's the composite.

---

## 5. Coordinate conventions & the orientation story

This is the subtle part, so it lives in one place:

| Space | Convention |
|-------|------------|
| Native depth buffer | `idx = col + row*W`, `.z` = metric Z. Body cameras delivered **rotated 90°**. |
| Upright model pixel | `u` = column (right+), `v` = row (down+). |
| Camera space | `+X` right, `+Y` down, `+Z` forward; `depth = Zc`. |
| World | Unity world via `cameraToWorld = GetCurrentPose()`. |
| NDC (view rays) | `+X` right, `+Y` up; clip Z ∈ [-1, 1] (we use `cam.projectionMatrix` and divide by w ourselves, so it's platform-independent). |

The legacy splat path used a transposed, mirror-tuned convention; we deliberately do
**not** inherit it. Instead the builder produces a clean upright pinhole, Pass 1
re-samples the data to match, and the march is plain pinhole. The transpose is
established; the exact body-camera flip (`flipU`/`flipV`) is being finalized against the
live view — tracked in [RAYMARCH_ROUTE_A.md](RAYMARCH_ROUTE_A.md).

---

## 6. Color space

The project is **Linear** (`m_ActiveColorSpace = 1`). The native color texture is
sampled (sRGB→linear decode) in Pass 1 and stored into `uprightColor` via a UAV. That RT
is created `RenderTextureReadWrite.Linear` on purpose: a default (sRGB) RT would make the
march's `.Load()` decode the already-linear value a **second** time and darken everything.
The final `Blit` to the (sRGB) framebuffer does the single encode, matching the splat cloud.

---

## 7. Limitations & what's next

- **Single camera.** Only `front_left` is wired. Translating the view reveals
  **disocclusion** (regions the one camera never saw) and is the core motivation for
  **Step 2 — multi-camera** (`Texture2DArray` of all cameras + an all-camera surface test
  + view-dependent color blend).
- **Mono only.** The compositor uses the camera's center matrices; VR needs a **per-eye**
  pass with each eye's `GetStereoView/ProjectionMatrix`.
- **Brute-force march.** Fixed-step, full screen resolution every frame. Acceleration
  (min/max depth pyramid, sphere-tracing) and a render-scale/temporal pass come before VR
  framerates.
- **Heuristic discontinuity threshold.** Absolute (metres); a relative (% of depth)
  threshold would scale better with distance.
```
