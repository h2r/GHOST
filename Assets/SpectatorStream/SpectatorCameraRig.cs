using System.Linq;
using UnityEngine;

namespace Ghost.SpectatorStream
{
    /// <summary>
    /// Frames the robots from above and behind. The robot models are moved to
    /// each robot's body by SpotTFSubscriber (map -> spot/body); this finds
    /// those target objects, tracks their centroid, and parks the camera up
    /// and back looking down-and-forward at them — so it stays on the robots
    /// wherever localization puts them, instead of a fixed world pose that
    /// lands off to the side.
    ///
    /// Until robot transforms exist (pre-localization) it holds a fallback
    /// pose, and it never throws — a framing problem must not kill the stream.
    /// </summary>
    public class SpectatorCameraRig : MonoBehaviour
    {
        [Tooltip("Metres above the robots.")]
        public float height = 3.0f;
        [Tooltip("Metres behind the robots (along their heading).")]
        public float distance = 4.5f;
        [Tooltip("Aim point dropped this far below the centroid, so the view tilts down.")]
        public float lookDownBias = 0.4f;
        [Tooltip("Position/orientation smoothing; higher = snappier.")]
        public float smoothing = 3.0f;

        public Vector3 fallbackPosition = new Vector3(0f, 3f, -4.5f);
        public Vector3 fallbackEuler = new Vector3(28f, 0f, 0f);

        private Transform[] robots;
        private float nextScan;

        private void Start() => Rescan();

        private void Rescan()
        {
            try
            {
                robots = Object
                    .FindObjectsByType<RosSharp.RosBridgeClient.SpotTFSubscriber>(FindObjectsSortMode.None)
                    .Where(s => s != null && s.targetObject != null
                                && !string.IsNullOrEmpty(s.target) && s.target.Contains("body"))
                    .Select(s => s.targetObject.transform)
                    .ToArray();
            }
            catch
            {
                robots = null;
            }
        }

        private void LateUpdate()
        {
            // Robots can spawn / localize after we start, so keep rescanning
            // until we have them.
            if ((robots == null || robots.Length == 0) && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 1f;
                Rescan();
            }

            Vector3 targetPos;
            Quaternion targetRot;

            if (robots != null && robots.Length > 0)
            {
                Vector3 centroid = Vector3.zero;
                foreach (var r in robots) centroid += r.position;
                centroid /= robots.Length;

                // "Behind" = opposite the lead robot's heading, flattened.
                Vector3 fwd = robots[0].forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                fwd.Normalize();

                targetPos = centroid - fwd * distance + Vector3.up * height;
                targetRot = Quaternion.LookRotation((centroid - Vector3.up * lookDownBias) - targetPos);
            }
            else
            {
                targetPos = fallbackPosition;
                targetRot = Quaternion.Euler(fallbackEuler);
            }

            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
    }
}
