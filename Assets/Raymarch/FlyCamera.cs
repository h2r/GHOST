using UnityEngine;

namespace Raymarch
{
    /// <summary>
    /// Quick WASD + up/down fly camera for validating the ray-march from arbitrary viewpoints.
    /// Uses the legacy Input API (project Active Input Handling = Both).
    ///
    /// Controls (play mode):
    ///   W/A/S/D      move forward/left/back/right (relative to where the camera looks)
    ///   E / Space    move up;   Q / Left Ctrl   move down (world up/down)
    ///   Hold Shift   move faster
    ///   Hold RMB     mouse-look (yaw/pitch)
    ///   Scroll       adjust move speed
    ///
    /// Attach to a free (non-head-tracked) camera. On a head-tracked camera the Tracked Pose Driver
    /// will fight this; validate with a plain camera (and XR off) instead.
    /// </summary>
    public class FlyCamera : MonoBehaviour
    {
        public float moveSpeed = 3f;
        public float boostMultiplier = 4f;
        public float lookSensitivity = 2f;
        [Tooltip("Require holding the right mouse button to look (avoids the cursor stealing rotation).")]
        public bool requireRightMouseToLook = true;

        private float yaw;
        private float pitch;

        private void OnEnable()
        {
            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x;
        }

        private void Update()
        {
            if (!requireRightMouseToLook || Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f);

            Vector3 dir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) dir += transform.forward;
            if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
            if (Input.GetKey(KeyCode.D)) dir += transform.right;
            if (Input.GetKey(KeyCode.A)) dir -= transform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) dir += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) dir -= Vector3.up;

            transform.position += dir.normalized * (speed * Time.deltaTime);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                moveSpeed = Mathf.Clamp(moveSpeed * (1f + scroll), 0.1f, 100f);
        }
    }
}
