using UnityEngine;

namespace Raymarch
{
    /// <summary>
    /// Per-camera mounting orientation, relative to an upright pinhole image.
    /// Spot's body cameras (front-left/right, left, right, back) are mounted rotated 90°,
    /// so their native frames are transposed; the HAND camera is upright.
    /// </summary>
    public enum ImageOrientation
    {
        Upright,    // native (col, row) is already the canonical (u, v)
        Transpose,  // native frame is sideways: canonical (u, v) = (row, col), axes/intrinsics swapped
    }

    /// <summary>
    /// THE single native&lt;-&gt;model pixel-mapping contract for one depth camera (R1 fix,
    /// 2026-06-10). The same instance must drive BOTH the camera model
    /// (<see cref="DepthCameraModelBuilder.Build"/>) and the data pack pass
    /// (<c>DepthToUpright.compute</c>, bound via <see cref="ApplyTo"/>) so the packed
    /// textures and the model can never desync. The GPU mapping in DepthToUpright.compute
    /// must be kept in lockstep with <see cref="ModelToNative"/>.
    ///
    /// Three independent layers:
    ///   - <see cref="orientation"/>: physical sensor mounting (Transpose for the 90°-rotated
    ///     body cameras, Upright for the hand camera).
    ///   - <see cref="flipU"/>/<see cref="flipV"/>: labeling flips of the upright frame. They are
    ///     baked into the model intrinsics AND applied to the pack identically, so they relabel
    ///     texture coordinates WITHOUT changing the rendered world geometry (the two mirrors
    ///     cancel exactly — see CameraModelValidator's relabeling-equivalence test). Keep flipV
    ///     for body cameras so the packed upright image is also visually upright (Step 0 frustum
    ///     finding).
    ///   - <see cref="mirroredNativeBuffer"/>: the native post-CVD buffers store pixels
    ///     180°-rotated relative to the (col, row) grid their intrinsics describe. The legacy
    ///     splat pipeline compensates at fetch time (PointcloudComputeShader CSMain:
    ///     depthIndex = W*(H-row-1) + (W-col-1); InstancedIndirectColor mirrors its color UV the
    ///     same way). Unlike the flips this is a DATA property, not a labeling choice: it changes
    ///     which depth value sits on which ray, so it must be ON for the march to reproduce the
    ///     trusted splat-cloud geometry. (Resolves the 2026-06-08 flipU-vs-flipV discrepancy: the
    ///     old hardcoded pack desynced from a flipU model into an accidental net-180° mapping,
    ///     which is exactly this mirror.)
    /// </summary>
    [System.Serializable]
    public struct NativePixelTransform
    {
        public ImageOrientation orientation;
        public bool flipU;
        public bool flipV;
        public bool mirroredNativeBuffer;

        public NativePixelTransform(
            ImageOrientation orientation, bool flipU, bool flipV, bool mirroredNativeBuffer)
        {
            this.orientation = orientation;
            this.flipU = flipU;
            this.flipV = flipV;
            this.mirroredNativeBuffer = mirroredNativeBuffer;
        }

        /// <summary>Validated mapping for Spot's five body cameras (2026-06-08 + 2026-06-10).</summary>
        public static NativePixelTransform SpotBodyCamera =>
            new NativePixelTransform(ImageOrientation.Transpose, flipU: false, flipV: true,
                mirroredNativeBuffer: true);

        /// <summary>
        /// Expected mapping for the (upright) hand camera. The legacy pipeline applies the same
        /// mirrored fetch to every camera, so the mirror is assumed ON. UNCONFIRMED until we get
        /// a live hand-camera frame.
        /// </summary>
        public static NativePixelTransform SpotHandCamera =>
            new NativePixelTransform(ImageOrientation.Upright, flipU: false, flipV: false,
                mirroredNativeBuffer: true);

        public bool Transposed => orientation == ImageOrientation.Transpose;

        public int ModelWidth(int nativeWidth, int nativeHeight) =>
            Transposed ? nativeHeight : nativeWidth;

        public int ModelHeight(int nativeWidth, int nativeHeight) =>
            Transposed ? nativeWidth : nativeHeight;

        /// <summary>
        /// Native intrinsics-grid pixel (col, row) -> model pixel (u, v).
        /// (Pixel centers at integer coordinates; flips mirror about size-1.)
        /// </summary>
        public Vector2 NativeToModel(float col, float row, int nativeWidth, int nativeHeight)
        {
            float u = Transposed ? row : col;
            float v = Transposed ? col : row;
            if (flipU) u = (ModelWidth(nativeWidth, nativeHeight) - 1) - u;
            if (flipV) v = (ModelHeight(nativeWidth, nativeHeight) - 1) - v;
            return new Vector2(u, v);
        }

        /// <summary>Model pixel (u, v) -> native intrinsics-grid pixel (col, row). Exact inverse of
        /// <see cref="NativeToModel"/>.</summary>
        public Vector2 ModelToNative(float u, float v, int nativeWidth, int nativeHeight)
        {
            if (flipU) u = (ModelWidth(nativeWidth, nativeHeight) - 1) - u;
            if (flipV) v = (ModelHeight(nativeWidth, nativeHeight) - 1) - v;
            return Transposed ? new Vector2(v, u) : new Vector2(u, v);
        }

        /// <summary>
        /// Native intrinsics-grid pixel (col, row) -> the buffer/texture pixel that actually holds
        /// its data (applies <see cref="mirroredNativeBuffer"/>). Self-inverse.
        /// </summary>
        public Vector2Int NativeToBufferPixel(int col, int row, int nativeWidth, int nativeHeight)
        {
            return mirroredNativeBuffer
                ? new Vector2Int((nativeWidth - 1) - col, (nativeHeight - 1) - row)
                : new Vector2Int(col, row);
        }

        /// <summary>Binds this transform to the pack pass (DepthToUpright.compute).</summary>
        public void ApplyTo(ComputeShader shader)
        {
            shader.SetInt("_Transpose", Transposed ? 1 : 0);
            shader.SetInt("_FlipU", flipU ? 1 : 0);
            shader.SetInt("_FlipV", flipV ? 1 : 0);
            shader.SetInt("_MirrorNative", mirroredNativeBuffer ? 1 : 0);
        }

        public override string ToString() =>
            $"(orientation={orientation}, flipU={flipU}, flipV={flipV}, " +
            $"mirroredNativeBuffer={mirroredNativeBuffer})";
    }

    /// <summary>
    /// Builds a canonical <see cref="DepthCameraModel"/> from a live <see cref="DrawMeshInstanced"/>,
    /// folding away the legacy transposed convention so the rest of Route A can assume a clean
    /// standard pinhole (u = column, v = row).
    ///
    /// Measured on 2026-06-04 against 'front_left_cloud' (a 90°-mounted body camera): the legacy
    /// pipeline places a depth pixel at (u* = row + (cx-cy), v* = col - (cx-cy), depth) — a pure
    /// transpose with a principal-point offset, depth unchanged. We reproduce that by treating the
    /// image as transposed: swap the principal-point and focal-length components and swap the frame
    /// dimensions. The extrinsic stays as the renderer's GameObject pose.
    ///
    /// RESOLVED 2026-06-08 (live frustum-corner test on 'front_left_cloud'): the legacy transpose
    /// alone is a reflection and is vertically flipped vs. physical up. Adding a vertical flip makes
    /// it a true 90° rotation — all four image corners matched reality and left/right was NOT
    /// mirrored. The flip is baked into the model by mirroring the affected principal-point
    /// component and negating the corresponding focal length (negative focal == that image axis is
    /// reflected); this is exactly equivalent to feeding the unflipped model a mirrored pixel
    /// coordinate.
    ///
    /// R1 FIX 2026-06-10: orientation/flips/mirror now live in <see cref="NativePixelTransform"/>,
    /// shared by this builder and the pack pass so model and data cannot desync. Note the flips
    /// only relabel the upright frame (model flip and pack flip cancel); the part of the mapping
    /// that changes world geometry is <see cref="NativePixelTransform.mirroredNativeBuffer"/>,
    /// which encodes the legacy buffers' 180°-rotated storage (it affects the pack's data fetch
    /// only, never the model intrinsics built here).
    /// </summary>
    public static class DepthCameraModelBuilder
    {
        /// <summary>
        /// Canonical model for Spot's body cameras (front-left/right, left, right, back), which are
        /// mounted rotated 90°: <see cref="NativePixelTransform.SpotBodyCamera"/>. The hand camera
        /// is upright; use <see cref="NativePixelTransform.SpotHandCamera"/> once confirmed live.
        /// </summary>
        public static DepthCameraModel BuildForSpotBodyCamera(DrawMeshInstanced renderer)
        {
            return BuildFor(renderer, NativePixelTransform.SpotBodyCamera);
        }

        public static DepthCameraModel BuildFor(DrawMeshInstanced renderer, NativePixelTransform transform)
        {
            return Build(renderer.Intrinsics, renderer.FrameWidth, renderer.FrameHeight,
                renderer.GetCurrentPose(), transform);
        }

        /// <summary>
        /// Legacy signature (model only — the buffer mirror never affects intrinsics). Prefer the
        /// <see cref="NativePixelTransform"/> overload so the same transform also drives the pack.
        /// </summary>
        public static DepthCameraModel BuildFor(
            DrawMeshInstanced renderer, ImageOrientation orientation, bool flipU = false, bool flipV = false)
        {
            return BuildFor(renderer,
                new NativePixelTransform(orientation, flipU, flipV, mirroredNativeBuffer: false));
        }

        /// <summary>Core builder; usable without a live renderer (validation, future providers).</summary>
        public static DepthCameraModel Build(
            Vector4 nativeIntrinsics, int nativeWidth, int nativeHeight,
            Matrix4x4 cameraToWorld, NativePixelTransform transform)
        {
            float cx = nativeIntrinsics.x;
            float cy = nativeIntrinsics.y;
            float fx = nativeIntrinsics.z;
            float fy = nativeIntrinsics.w;

            // Base model: optional transpose (de-rotated image spans native height x native width;
            // model u-axis runs along native rows -> native fy/cy, v-axis along native cols -> fx/cx).
            float mfx, mfy, mcx, mcy;
            if (transform.Transposed)
            {
                mfx = fy; mfy = fx; mcx = cy; mcy = cx;
            }
            else
            {
                mfx = fx; mfy = fy; mcx = cx; mcy = cy;
            }
            int mw = transform.ModelWidth(nativeWidth, nativeHeight);
            int mh = transform.ModelHeight(nativeWidth, nativeHeight);

            // Flips: mirror the principal point and negate the focal length on that axis. A negative
            // focal encodes the reflection and is exactly equivalent to sampling pixel (width-1-u).
            if (transform.flipU) { mcx = (mw - 1) - mcx; mfx = -mfx; }
            if (transform.flipV) { mcy = (mh - 1) - mcy; mfy = -mfy; }

            return DepthCameraModel.Create(mfx, mfy, mcx, mcy, mw, mh, cameraToWorld);
        }

        /// <summary>
        /// Maps a native source pixel (col, row) to the canonical model pixel (u, v) for the given
        /// orientation, IGNORING flips. Legacy shim kept for <see cref="CameraModelLiveProbe"/>
        /// (tracked as R4); new code should use <see cref="NativePixelTransform.NativeToModel"/>.
        /// </summary>
        public static Vector2 NativeToModelPixel(int col, int row, ImageOrientation orientation)
        {
            return orientation == ImageOrientation.Transpose
                ? new Vector2(row, col)
                : new Vector2(col, row);
        }
    }
}
