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
                RunRoundTrip();
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
    }
}
