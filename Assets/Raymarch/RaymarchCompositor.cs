using UnityEngine;

namespace Raymarch
{
    /// <summary>
    /// Composites the single-camera ray-march into the camera's rendered image (built-in render
    /// pipeline) via OnRenderImage. Attach to the camera you want to look through (e.g. the main
    /// camera). Mono-first: uses the camera's center projection/view matrices; per-eye stereo is a
    /// later step (in VR multi-pass this runs per eye with the eye's src, but the matrices used here
    /// are the mono ones, so validate with XR off / a non-XR camera first).
    ///
    /// Pipeline each frame: pack the source camera's native depth (+color) into upright textures,
    /// march this camera's rays against them, and on a miss show the scene so the reconstruction
    /// composites over the rendered frame.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RaymarchCompositor : MonoBehaviour
    {
        [Header("Source depth camera")]
        public DrawMeshInstanced sourceRenderer;
        [Tooltip("Spot body cameras = Transpose + Flip V. Hand camera = Upright.")]
        public ImageOrientation orientation = ImageOrientation.Transpose;
        public bool flipU = false;
        public bool flipV = true;

        [Header("Compute shaders")]
        public ComputeShader packShader;   // DepthToUpright.compute
        public ComputeShader marchShader;  // MultiDepthRaymarch.compute

        [Header("March params")]
        public float near = 0.1f;
        public float far = 10f;
        [Range(8, 1024)] public int steps = 256;
        public float depthEps = 0.05f;
        public float minValidDepth = 0.05f;
        public float discontinuityThreshold = 0.15f;

        [Header("Color")]
        public bool useColor = true;
        public bool colorFlipU = false;
        public bool colorFlipV = true;

        [Header("Composite")]
        [Tooltip("Composite the reconstruction over the camera image. Off = show it on black.")]
        public bool composite = true;
        [Tooltip("Uniform vertical flip of the composited output (escape hatch; normally off).")]
        public bool flipOutputV = false;

        [Header("Validation")]
        [Range(0f, 1f)]
        [Tooltip("Opacity of the reconstruction over the scene. Set ~0.5 to see the splat cloud " +
                 "through it and check the ray-march lands exactly on the cloud from every angle.")]
        public float marchBlend = 1f;

        [Header("Debug")]
        [Tooltip("Logs whether OnRenderImage fires and why it composites or passes through.")]
        public bool enableDebugLog = true;

        private Camera cam;
        private RenderTexture uprightDepth;
        private RenderTexture uprightColor;
        private RenderTexture composited;
        private int packKernel = -1;
        private int marchKernel = -1;
        private string lastLogState;

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            if (packShader != null) packKernel = packShader.FindKernel("Pack");
            if (marchShader != null) marchKernel = marchShader.FindKernel("March");
            if (enableDebugLog)
                Debug.Log($"[RaymarchCompositor] Enabled on '{name}'. " +
                          $"packKernel={packKernel}, marchKernel={marchKernel}.");
        }

        private void OnDisable()
        {
            ReleaseRT(ref uprightDepth);
            ReleaseRT(ref uprightColor);
            ReleaseRT(ref composited);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            bool ready = Ready();

            if (enableDebugLog)
            {
                string state = ready
                    ? "COMPOSITE"
                    : "PASSTHROUGH ("
                        + $"sourceRenderer={(sourceRenderer != null)}, "
                        + $"packShader={(packShader != null)}, marchShader={(marchShader != null)}, "
                        + $"packKernel={packKernel}, marchKernel={marchKernel}, "
                        + $"hasFrameData={(sourceRenderer != null && sourceRenderer.HasFrameData)}, "
                        + $"depthBuffer={(sourceRenderer != null && sourceRenderer.PointDepthBuffer != null)})";
                // Log on state change, plus a heartbeat every ~300 frames so steady state is visible
                // (silence then means OnRenderImage is NOT firing -> the camera isn't rendering the view).
                if (state != lastLogState || Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[RaymarchCompositor] OnRenderImage on '{name}' [{src.width}x{src.height}] " +
                              $"frame {Time.frameCount}: {state}");
                    lastLogState = state;
                }
            }

            if (!ready)
            {
                Graphics.Blit(src, dst); // passthrough so the view still renders
                return;
            }

            DepthCameraModel model = DepthCameraModelBuilder.BuildFor(sourceRenderer, orientation, flipU, flipV);
            int nativeW = sourceRenderer.FrameWidth;
            int nativeH = sourceRenderer.FrameHeight;
            int uW = model.width;
            int uH = model.height;
            if (uW <= 0 || uH <= 0)
            {
                Graphics.Blit(src, dst);
                return;
            }

            int outW = src.width;
            int outH = src.height;

            EnsureRT(ref uprightDepth, uW, uH, RenderTextureFormat.RFloat);
            // Linear RT: store already-decoded (linear) color via UAV; a default (sRGB) RT would make
            // .Load() decode a second time and darken everything in a Linear project.
            EnsureRT(ref uprightColor, uW, uH, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            EnsureRT(ref composited, outW, outH, RenderTextureFormat.ARGBFloat);

            bool hasColor = useColor && sourceRenderer.colorImage != null;

            // 1) Pack native depth (+ color) -> upright textures.
            packShader.SetInt("_NativeWidth", nativeW);
            packShader.SetInt("_NativeHeight", nativeH);
            packShader.SetInt("_UprightWidth", uW);
            packShader.SetInt("_UprightHeight", uH);
            packShader.SetInt("_BodyCamera", orientation == ImageOrientation.Transpose ? 1 : 0);
            packShader.SetInt("_HasColor", hasColor ? 1 : 0);
            packShader.SetInt("_ColorFlipU", colorFlipU ? 1 : 0);
            packShader.SetInt("_ColorFlipV", colorFlipV ? 1 : 0);
            packShader.SetBuffer(packKernel, "_PointDepth", sourceRenderer.PointDepthBuffer);
            packShader.SetTexture(packKernel, "_UprightDepth", uprightDepth);
            packShader.SetTexture(packKernel, "_NativeColor",
                sourceRenderer.colorImage != null ? (Texture)sourceRenderer.colorImage : Texture2D.whiteTexture);
            packShader.SetTexture(packKernel, "_UprightColor", uprightColor);
            packShader.Dispatch(packKernel, Mathf.CeilToInt(uW / 8f), Mathf.CeilToInt(uH / 8f), 1);

            // 2) March this camera's rays and composite over the scene.
            Matrix4x4 invVP = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
            marchShader.SetVector("_CamIntrinsics", new Vector4(model.cx, model.cy, model.fx, model.fy));
            marchShader.SetVector("_CamResolution", new Vector2(model.width, model.height));
            marchShader.SetMatrix("_CamToWorld", model.cameraToWorld);
            marchShader.SetMatrix("_WorldToCam", model.worldToCamera);
            marchShader.SetMatrix("_InvViewProj", invVP);
            marchShader.SetInt("_OutWidth", outW);
            marchShader.SetInt("_OutHeight", outH);
            marchShader.SetFloat("_Near", near);
            marchShader.SetFloat("_Far", far);
            marchShader.SetInt("_Steps", steps);
            marchShader.SetFloat("_DepthEps", depthEps);
            marchShader.SetFloat("_MinValidDepth", minValidDepth);
            marchShader.SetFloat("_DiscontinuityThreshold", discontinuityThreshold);
            marchShader.SetInt("_FlipV", flipOutputV ? 1 : 0);
            marchShader.SetInt("_RayFromModel", 0);
            marchShader.SetInt("_UseColor", hasColor ? 1 : 0);
            marchShader.SetInt("_Composite", composite ? 1 : 0);
            marchShader.SetFloat("_HitBlend", marchBlend);
            marchShader.SetTexture(marchKernel, "_UprightDepth", uprightDepth);
            marchShader.SetTexture(marchKernel, "_UprightColor", uprightColor);
            marchShader.SetTexture(marchKernel, "_SceneColor", src);
            marchShader.SetTexture(marchKernel, "_Output", composited);
            marchShader.Dispatch(marchKernel, Mathf.CeilToInt(outW / 8f), Mathf.CeilToInt(outH / 8f), 1);

            Graphics.Blit(composited, dst);
        }

        // Identity test: move THIS camera to the source camera's pose and match its vertical FOV, so
        // the march should reproduce the depth camera's own view undistorted. Only meaningful on a
        // free (non-head-tracked) test camera. Run from the component context menu.
        [ContextMenu("Snap this camera to the source camera pose")]
        public void SnapToSourcePose()
        {
            Camera c = GetComponent<Camera>();
            if (sourceRenderer == null || c == null)
            {
                Debug.LogWarning("[RaymarchCompositor] Need a source renderer and a Camera to snap.");
                return;
            }

            DepthCameraModel model = DepthCameraModelBuilder.BuildFor(sourceRenderer, orientation, flipU, flipV);
            Matrix4x4 c2w = model.cameraToWorld;
            c.transform.SetPositionAndRotation(c2w.GetColumn(3), c2w.rotation);
            // Vertical FOV from the (possibly flip-negated) focal length and the upright image height.
            float vfov = 2f * Mathf.Atan2(model.height * 0.5f, Mathf.Abs(model.fy)) * Mathf.Rad2Deg;
            c.fieldOfView = Mathf.Clamp(vfov, 1f, 179f);
            Debug.Log($"[RaymarchCompositor] Snapped to source pose; vFOV set to {vfov:F1} deg. " +
                      "The march should now reproduce the depth camera's own view.");
        }

        private bool Ready()
        {
            return cam != null
                && sourceRenderer != null && packShader != null && marchShader != null
                && packKernel >= 0 && marchKernel >= 0
                && sourceRenderer.HasFrameData && sourceRenderer.PointDepthBuffer != null;
        }

        private static void EnsureRT(ref RenderTexture rt, int w, int h, RenderTextureFormat fmt,
            RenderTextureReadWrite rw = RenderTextureReadWrite.Default)
        {
            if (rt != null && rt.width == w && rt.height == h && rt.format == fmt)
                return;
            ReleaseRT(ref rt);
            rt = new RenderTexture(w, h, 0, fmt, rw) { enableRandomWrite = true };
            rt.Create();
        }

        private static void ReleaseRT(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                rt = null;
            }
        }
    }
}
