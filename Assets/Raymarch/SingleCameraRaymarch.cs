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

        [Header("View camera (defines the rays)")]
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
        public bool runContinuously = true;

        private RenderTexture uprightDepth;
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

            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
                return;

            DepthCameraModel model = DepthCameraModelBuilder.BuildFor(sourceRenderer, orientation, flipU, flipV);
            int nativeW = sourceRenderer.FrameWidth;
            int nativeH = sourceRenderer.FrameHeight;
            int uW = model.width;
            int uH = model.height;
            if (uW <= 0 || uH <= 0)
                return;

            EnsureRT(ref uprightDepth, uW, uH, RenderTextureFormat.RFloat);
            EnsureRT(ref output, outputWidth, outputHeight, RenderTextureFormat.ARGBFloat);

            // 1) Pack native depth -> upright depth texture.
            packShader.SetInt("_NativeWidth", nativeW);
            packShader.SetInt("_NativeHeight", nativeH);
            packShader.SetInt("_UprightWidth", uW);
            packShader.SetInt("_UprightHeight", uH);
            packShader.SetInt("_BodyCamera", orientation == ImageOrientation.Transpose ? 1 : 0);
            packShader.SetBuffer(packKernel, "_PointDepth", sourceRenderer.PointDepthBuffer);
            packShader.SetTexture(packKernel, "_UprightDepth", uprightDepth);
            packShader.Dispatch(packKernel, Mathf.CeilToInt(uW / 8f), Mathf.CeilToInt(uH / 8f), 1);

            // 2) March the view camera's rays against it.
            Matrix4x4 invVP = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;

            marchShader.SetVector("_CamIntrinsics", new Vector4(model.cx, model.cy, model.fx, model.fy));
            marchShader.SetVector("_CamResolution", new Vector2(model.width, model.height));
            marchShader.SetMatrix("_CamToWorld", model.cameraToWorld);
            marchShader.SetMatrix("_WorldToCam", model.worldToCamera);
            marchShader.SetMatrix("_InvViewProj", invVP);
            marchShader.SetInt("_OutWidth", outputWidth);
            marchShader.SetInt("_OutHeight", outputHeight);
            marchShader.SetFloat("_Near", near);
            marchShader.SetFloat("_Far", far);
            marchShader.SetInt("_Steps", steps);
            marchShader.SetFloat("_DepthEps", depthEps);
            marchShader.SetFloat("_MinValidDepth", minValidDepth);
            marchShader.SetTexture(marchKernel, "_UprightDepth", uprightDepth);
            marchShader.SetTexture(marchKernel, "_Output", output);
            marchShader.Dispatch(marchKernel, Mathf.CeilToInt(outputWidth / 8f), Mathf.CeilToInt(outputHeight / 8f), 1);

            if (display != null)
                display.texture = output;
        }

        private static void EnsureRT(ref RenderTexture rt, int w, int h, RenderTextureFormat fmt)
        {
            if (rt != null && rt.width == w && rt.height == h && rt.format == fmt)
                return;
            ReleaseRT(ref rt);
            rt = new RenderTexture(w, h, 0, fmt) { enableRandomWrite = true };
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
