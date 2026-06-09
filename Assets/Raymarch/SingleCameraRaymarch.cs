using UnityEngine;
using UnityEngine.UI;

namespace Raymarch
{
    /// <summary>
    /// Route A, Step 1 driver: ray-casts the view camera against ONE depth camera's upright depth
    /// image and shows the result on a debug RawImage. Geometry-only (depth-shaded) so we can
    /// validate the march against the splat cloud before adding color/blending.
    ///
    /// Two-pass each frame:
    ///   1) DepthToUpright.compute packs the source camera's native depth into an upright RFloat RT.
    ///   2) MultiDepthRaymarch.compute marches the view camera's rays against it.
    ///
    /// Sanity check: place the view camera at roughly the source camera's pose — the output should
    /// resemble that camera's own depth map. Then move the view camera to confirm 3D consistency.
    /// </summary>
    public class SingleCameraRaymarch : MonoBehaviour
    {
        [Header("Source depth camera")]
        public DrawMeshInstanced sourceRenderer;
        [Tooltip("Spot body cameras (front/left/right/back) = Transpose + Flip V. Hand camera = Upright.")]
        public ImageOrientation orientation = ImageOrientation.Transpose;
        public bool flipU = false;
        public bool flipV = true;

        public enum RayMode
        {
            SourceCameraImage, // rays from the depth camera model: reproduces its own image (validation)
            ViewCamera,        // rays from the view camera (the real Route A use case)
        }

        [Header("Ray source")]
        public RayMode rayMode = RayMode.SourceCameraImage;
        [Tooltip("Extra global vertical flip for platform/UI quirks. With the per-mode handling in " +
                 "the shader, OFF should be upright for BOTH ray modes; only toggle if your platform differs.")]
        public bool flipOutputV = false;

        [Header("View camera (used only in ViewCamera mode)")]
        public Camera viewCamera;

        [Header("Compute shaders")]
        public ComputeShader packShader;   // DepthToUpright.compute
        public ComputeShader marchShader;  // MultiDepthRaymarch.compute

        [Header("Output")]
        public int outputWidth = 640;
        public int outputHeight = 480;
        public RawImage display;

        [Header("March params")]
        public float near = 0.1f;
        public float far = 10f;
        [Range(8, 1024)] public int steps = 256;
        public float depthEps = 0.05f;
        public float minValidDepth = 0.05f;
        [Tooltip("Reject surface crossings where stored depth jumps more than this (m) — silhouette " +
                 "edges. Leaves holes instead of rubber-sheet smears. 0 disables.")]
        public float discontinuityThreshold = 0.15f;
        public bool runContinuously = true;

        [Header("Color")]
        public bool useColor = true;
        [Tooltip("Toggle these if the color comes out mirrored/upside-down relative to the depth shape.")]
        public bool colorFlipU = false;
        public bool colorFlipV = true;

        private RenderTexture uprightDepth;
        private RenderTexture uprightColor;
        private RenderTexture output;
        private int packKernel = -1;
        private int marchKernel = -1;

        private void OnEnable()
        {
            if (packShader != null) packKernel = packShader.FindKernel("Pack");
            if (marchShader != null) marchKernel = marchShader.FindKernel("March");
        }

        private void OnDisable()
        {
            ReleaseRT(ref uprightDepth);
            ReleaseRT(ref uprightColor);
            ReleaseRT(ref output);
        }

        private void Update()
        {
            if (runContinuously)
                Render();
        }

        [ContextMenu("Render Once")]
        public void Render()
        {
            if (sourceRenderer == null || packShader == null || marchShader == null)
                return;
            if (packKernel < 0 || marchKernel < 0)
                return;
            if (!sourceRenderer.HasFrameData || sourceRenderer.PointDepthBuffer == null)
                return;

            bool fromModel = rayMode == RayMode.SourceCameraImage;

            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (!fromModel && cam == null)
                return;

            DepthCameraModel model = DepthCameraModelBuilder.BuildFor(sourceRenderer, orientation, flipU, flipV);
            int nativeW = sourceRenderer.FrameWidth;
            int nativeH = sourceRenderer.FrameHeight;
            int uW = model.width;
            int uH = model.height;
            if (uW <= 0 || uH <= 0)
                return;

            // In validation mode the output matches the camera's own (portrait) image so it fills the frame.
            int outW = fromModel ? uW : outputWidth;
            int outH = fromModel ? uH : outputHeight;

            EnsureRT(ref uprightDepth, uW, uH, RenderTextureFormat.RFloat);
            // Linear RT: we store the already-decoded (linear) color via UAV; a default (sRGB) RT
            // would make .Load() decode a second time and darken everything in a Linear project.
            EnsureRT(ref uprightColor, uW, uH, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            EnsureRT(ref output, outW, outH, RenderTextureFormat.ARGBFloat);

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

            // 2) March. In ViewCamera mode, build the projection with the OUTPUT aspect (not the
            // camera's screen aspect) so there is no horizontal stretch/margin from an aspect mismatch.
            Matrix4x4 invVP = Matrix4x4.identity;
            if (!fromModel)
            {
                float aspect = (float)outW / outH;
                Matrix4x4 proj = Matrix4x4.Perspective(
                    cam.fieldOfView, aspect, Mathf.Max(0.01f, near), Mathf.Max(near + 0.01f, far));
                invVP = (proj * cam.worldToCameraMatrix).inverse;
            }

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
            marchShader.SetInt("_RayFromModel", fromModel ? 1 : 0);
            marchShader.SetInt("_UseColor", hasColor ? 1 : 0);
            marchShader.SetInt("_Composite", 0); // debug driver never composites the scene
            marchShader.SetFloat("_HitBlend", 1f);
            marchShader.SetTexture(marchKernel, "_UprightDepth", uprightDepth);
            marchShader.SetTexture(marchKernel, "_UprightColor", uprightColor);
            marchShader.SetTexture(marchKernel, "_SceneColor", Texture2D.blackTexture);
            marchShader.SetTexture(marchKernel, "_Output", output);
            marchShader.Dispatch(marchKernel, Mathf.CeilToInt(outW / 8f), Mathf.CeilToInt(outH / 8f), 1);

            if (display != null)
                display.texture = output;
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
