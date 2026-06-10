using UnityEngine;

namespace Raymarch
{
    /// <summary>
    /// Step 0 verification harness for <see cref="DepthCameraModel"/>.
    ///
    /// The core test needs no scene/robot data: it confirms that
    /// <c>ProjectFromWorld(UnprojectToWorld(u, v, d)) == (u, v, d)</c> over a grid of
    /// pixels, depths, and a non-trivial extrinsic — i.e. the canonical project/unproject
    /// are exact inverses. Run it from the component context menu in the editor, or set
    /// <see cref="runOnStart"/> to log on play.
    ///
    /// The make-or-break Step 0 task (reconciling the canonical extrinsic with the live
    /// SpotObserver camera poses) is done separately in play mode against real frames.
    /// </summary>
    public class CameraModelValidator : MonoBehaviour
    {
        [Header("Test intrinsics (pixels)")]
        public float fx = 552f;
        public float fy = 552f;
        public float cx = 320f;
        public float cy = 240f;
        public int width = 640;
        public int height = 480;

        [Header("Test extrinsic (camera -> world)")]
        public Vector3 cameraPosition = new Vector3(1.2f, -0.4f, 2.0f);
        public Vector3 cameraEuler = new Vector3(15f, -40f, 8f);

        [Tooltip("Maximum acceptable round-trip error in pixels (u,v) / metres (depth).")]
        public float tolerance = 1e-3f;

        public bool runOnStart = false;

        private void Start()
        {
            if (runOnStart)
            {
                RunRoundTrip();
                RunPixelTransformTests();
            }
        }

        [ContextMenu("Run Round-Trip Test")]
        public void RunRoundTrip()
        {
            Matrix4x4 cameraToWorld = Matrix4x4.TRS(
                cameraPosition, Quaternion.Euler(cameraEuler), Vector3.one);

            DepthCameraModel cam = DepthCameraModel.Create(fx, fy, cx, cy, width, height, cameraToWorld);

            float maxError = 0f;
            int samples = 0;

            for (int v = 5; v < height; v += 37)
            {
                for (int u = 5; u < width; u += 37)
                {
                    for (float d = 0.5f; d <= 6f; d += 1.5f)
                    {
                        Vector3 world = cam.UnprojectToWorld(u, v, d);
                        Vector3 reproj = cam.ProjectFromWorld(world);

                        float error = Mathf.Max(
                            Mathf.Abs(reproj.x - u),
                            Mathf.Abs(reproj.y - v),
                            Mathf.Abs(reproj.z - d));

                        maxError = Mathf.Max(maxError, error);
                        samples++;
                    }
                }
            }

            if (maxError <= tolerance)
                Debug.Log($"[CameraModelValidator] PASS: project/unproject are inverses. " +
                          $"max error = {maxError:E3} over {samples} samples (tol {tolerance:E3}).");
            else
                Debug.LogError($"[CameraModelValidator] FAIL: round-trip error {maxError:E3} " +
                               $"exceeds tolerance {tolerance:E3} over {samples} samples.");
        }

        /// <summary>
        /// R1 contract tests (no scene/robot needed), over every orientation/flip combination:
        ///   1. NativeToModel / ModelToNative are exact inverses (the pack pass implements
        ///      ModelToNative on the GPU, so this pins the shared mapping).
        ///   2. Relabeling equivalence: a model built WITH flips, fed the flipped pixel from
        ///      NativeToModel, unprojects to the SAME world point as the unflipped model fed the
        ///      unflipped pixel — i.e. synced labeling flips never change world geometry. (Only
        ///      mirroredNativeBuffer changes which depth value sits on which ray, and that lives
        ///      purely in the pack's data fetch.)
        /// </summary>
        [ContextMenu("Run Pixel-Transform Contract Tests")]
        public void RunPixelTransformTests()
        {
            Matrix4x4 cameraToWorld = Matrix4x4.TRS(
                cameraPosition, Quaternion.Euler(cameraEuler), Vector3.one);
            Vector4 intr = new Vector4(cx, cy, fx, fy); // (cx, cy, fx, fy) packing

            float maxMapError = 0f;
            float maxWorldError = 0f;
            int samples = 0;

            foreach (ImageOrientation orientation in new[] { ImageOrientation.Upright, ImageOrientation.Transpose })
            {
                var baseXform = new NativePixelTransform(orientation, false, false, false);
                DepthCameraModel baseModel = DepthCameraModelBuilder.Build(intr, width, height, cameraToWorld, baseXform);

                for (int f = 0; f < 4; f++)
                {
                    var xform = new NativePixelTransform(orientation, (f & 1) != 0, (f & 2) != 0, false);
                    DepthCameraModel model = DepthCameraModelBuilder.Build(intr, width, height, cameraToWorld, xform);

                    for (int row = 5; row < height; row += 37)
                    {
                        for (int col = 5; col < width; col += 37)
                        {
                            // 1) mapping round-trip
                            Vector2 uv = xform.NativeToModel(col, row, width, height);
                            Vector2 back = xform.ModelToNative(uv.x, uv.y, width, height);
                            maxMapError = Mathf.Max(maxMapError,
                                Mathf.Max(Mathf.Abs(back.x - col), Mathf.Abs(back.y - row)));

                            // 2) flipped model + flipped pixel == base model + base pixel
                            Vector2 uvBase = baseXform.NativeToModel(col, row, width, height);
                            const float d = 2.5f;
                            Vector3 wFlipped = model.UnprojectToWorld(uv.x, uv.y, d);
                            Vector3 wBase = baseModel.UnprojectToWorld(uvBase.x, uvBase.y, d);
                            maxWorldError = Mathf.Max(maxWorldError, (wFlipped - wBase).magnitude);
                            samples++;
                        }
                    }
                }
            }

            if (maxMapError <= tolerance && maxWorldError <= tolerance)
                Debug.Log($"[CameraModelValidator] PASS: NativePixelTransform contract holds. " +
                          $"map round-trip err = {maxMapError:E3} px, relabeling world err = " +
                          $"{maxWorldError:E3} m over {samples} samples (tol {tolerance:E3}).");
            else
                Debug.LogError($"[CameraModelValidator] FAIL: NativePixelTransform contract broken. " +
                               $"map round-trip err = {maxMapError:E3} px, relabeling world err = " +
                               $"{maxWorldError:E3} m (tol {tolerance:E3}).");
        }
    }
}
