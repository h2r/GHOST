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
    /// mirrored. So the canonical body-camera orientation is Transpose + flipV (see
    /// <see cref="BuildForSpotBodyCamera"/>). The flip is baked into the model by mirroring the
    /// affected principal-point component and negating the corresponding focal length (negative
    /// focal == that image axis is reflected); this is exactly equivalent to feeding the unflipped
    /// model a mirrored pixel coordinate. Depth/color must be packed into the matching upright frame
    /// (Step 2) so model.ProjectFromWorld(...) indexes the texture arrays correctly.
    /// </summary>
    public static class DepthCameraModelBuilder
    {
        /// <summary>
        /// Canonical orientation for Spot's body cameras (front-left/right, left, right, back), which
        /// are mounted rotated 90°: Transpose + vertical flip. Validated 2026-06-08. The hand camera
        /// is upright and should use <see cref="BuildFor"/> with <see cref="ImageOrientation.Upright"/>.
        /// </summary>
        public static DepthCameraModel BuildForSpotBodyCamera(DrawMeshInstanced renderer)
        {
            return BuildFor(renderer, ImageOrientation.Transpose, flipU: false, flipV: true);
        }

        public static DepthCameraModel BuildFor(
            DrawMeshInstanced renderer, ImageOrientation orientation, bool flipU = false, bool flipV = false)
        {
            Vector4 intr = renderer.Intrinsics; // native (cx, cy, fx, fy)
            int w = renderer.FrameWidth;
            int h = renderer.FrameHeight;
            Matrix4x4 cameraToWorld = renderer.GetCurrentPose();

            float cx = intr.x;
            float cy = intr.y;
            float fx = intr.z;
            float fy = intr.w;

            // Base model: optional transpose (de-rotated image spans native height x native width;
            // model u-axis runs along native rows -> native fy/cy, v-axis along native cols -> fx/cx).
            float mfx, mfy, mcx, mcy;
            int mw, mh;
            if (orientation == ImageOrientation.Upright)
            {
                mfx = fx; mfy = fy; mcx = cx; mcy = cy; mw = w; mh = h;
            }
            else
            {
                mfx = fy; mfy = fx; mcx = cy; mcy = cx; mw = h; mh = w;
            }

            // Flips: mirror the principal point and negate the focal length on that axis. A negative
            // focal encodes the reflection and is exactly equivalent to sampling pixel (width-1-u).
            if (flipU) { mcx = (mw - 1) - mcx; mfx = -mfx; }
            if (flipV) { mcy = (mh - 1) - mcy; mfy = -mfy; }

            return DepthCameraModel.Create(mfx, mfy, mcx, mcy, mw, mh, cameraToWorld);
        }

        /// <summary>
        /// Maps a native source pixel (col, row) to the canonical model pixel (u, v) for the given
        /// orientation. Used when sampling native depth/color through a model built by
        /// <see cref="BuildFor"/> (and, later, when de-rotating frames into the texture arrays).
        /// </summary>
        public static Vector2 NativeToModelPixel(int col, int row, ImageOrientation orientation)
        {
            return orientation == ImageOrientation.Transpose
                ? new Vector2(row, col)
                : new Vector2(col, row);
        }
    }
}
