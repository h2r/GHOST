using UnityEngine;

namespace Raymarch
{
    /// <summary>
    /// Canonical pinhole camera model for the depth-raymarch pipeline (Route A).
    /// This is the single source of truth for intrinsics, extrinsics, and the
    /// (un)projection math. The GPU side lives in <c>CameraModel.hlsl</c> and MUST
    /// be kept in lockstep with this struct.
    ///
    /// Conventions:
    ///   - Pixel coords: <c>u</c> = column (0..width, increasing right),
    ///     <c>v</c> = row (0..height, increasing down).
    ///   - Principal point (cx, cy) and focal lengths (fx, fy) are in pixels.
    ///   - Camera space: +X right, +Y down, +Z forward (into the scene).
    ///     depth == Z_cam (metres).
    ///   - <c>cameraToWorld</c> maps camera space -> Unity world space.
    ///   - Intrinsics are packed (cx, cy, fx, fy) to match the legacy
    ///     <c>GetIntrinsicsVector()</c> / shader <c>intrinsics</c> ordering.
    ///
    /// <see cref="UnprojectToCamera"/> and <see cref="ProjectFromCamera"/> are exact
    /// analytic inverses for any point with Z_cam &gt; 0 (verified by
    /// <c>CameraModelValidator</c>).
    /// </summary>
    [System.Serializable]
    public struct DepthCameraModel
    {
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public int width;
        public int height;

        public Matrix4x4 cameraToWorld;
        public Matrix4x4 worldToCamera;

        public static DepthCameraModel Create(
            float fx, float fy, float cx, float cy,
            int width, int height, Matrix4x4 cameraToWorld)
        {
            DepthCameraModel m;
            m.fx = fx;
            m.fy = fy;
            m.cx = cx;
            m.cy = cy;
            m.width = width;
            m.height = height;
            m.cameraToWorld = cameraToWorld;
            m.worldToCamera = cameraToWorld.inverse;
            return m;
        }

        /// <summary>Build from the (cx, cy, fx, fy) intrinsics vector used elsewhere in the project.</summary>
        public static DepthCameraModel FromIntrinsicsVector(
            Vector4 intrinsics, int width, int height, Matrix4x4 cameraToWorld)
        {
            return Create(intrinsics.z, intrinsics.w, intrinsics.x, intrinsics.y, width, height, cameraToWorld);
        }

        /// <summary>Pixel (u, v) + metric depth -> camera-space point (+Z forward).</summary>
        public Vector3 UnprojectToCamera(float u, float v, float depth)
        {
            float x = (u - cx) * depth / fx;
            float y = (v - cy) * depth / fy;
            return new Vector3(x, y, depth);
        }

        /// <summary>Pixel (u, v) + metric depth -> world-space point.</summary>
        public Vector3 UnprojectToWorld(float u, float v, float depth)
        {
            return cameraToWorld.MultiplyPoint3x4(UnprojectToCamera(u, v, depth));
        }

        /// <summary>
        /// Camera-space point -> (u, v, depth). The returned z component is Z_cam:
        /// values &lt;= 0 are behind the camera. Inverse of <see cref="UnprojectToCamera"/>.
        /// </summary>
        public Vector3 ProjectFromCamera(Vector3 cam)
        {
            float u = fx * cam.x / cam.z + cx;
            float v = fy * cam.y / cam.z + cy;
            return new Vector3(u, v, cam.z);
        }

        /// <summary>World-space point -> (u, v, depth). Inverse of <see cref="UnprojectToWorld"/>.</summary>
        public Vector3 ProjectFromWorld(Vector3 world)
        {
            return ProjectFromCamera(worldToCamera.MultiplyPoint3x4(world));
        }

        public bool InFront(Vector3 cam)
        {
            return cam.z > 0f;
        }

        public bool InFrame(float u, float v)
        {
            return u >= 0f && u < width && v >= 0f && v < height;
        }

        /// <summary>
        /// Reference reproduction of the LEGACY GameObject-local point center produced by
        /// <c>PointcloudComputeShader.compute</c> (CSMain), kept here so the quirk is documented
        /// in exactly one place. Note the transposed / axis-mismatched convention:
        /// the row drives X (paired with cy but divided by fx) and the column drives Y
        /// (paired with cx but divided by fy). Used only by validation/migration tooling,
        /// never by the new pipeline.
        /// </summary>
        /// <param name="col">pixel column</param>
        /// <param name="row">pixel row</param>
        /// <param name="depth">metric depth</param>
        /// <param name="intrinsics">(cx, cy, fx, fy)</param>
        public static Vector3 LegacyGoLocalCenter(int col, int row, float depth, Vector4 intrinsics)
        {
            float cxL = intrinsics.x;
            float cyL = intrinsics.y;
            float fxL = intrinsics.z;
            float fyL = intrinsics.w;

            float x = (row - cyL) * depth / fxL;
            float y = (col - cxL) * depth / fyL;
            float z = depth;
            return new Vector3(x, y, z);
        }
    }
}
