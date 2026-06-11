using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Raymarch
{
    /// <summary>
    /// Route A, Step 2 driver: ray-casts this camera's view against ALL configured depth cameras
    /// at once (gather). Composites into the rendered image via OnRenderImage, like
    /// <see cref="RaymarchCompositor"/> (which remains the single-camera reference path).
    ///
    /// Pipeline each frame:
    ///   1) For every active source, DepthToUpright.PackArray packs its native depth (+color) into
    ///      one slice of a Texture2DArray, through that source's own NativePixelTransform.
    ///   2) Camera models are uploaded as a CameraGPU StructuredBuffer (matrices as explicit
    ///      columns — KEEP IN LOCKSTEP with CameraGPU in MultiDepthRaymarch.compute).
    ///   3) MultiDepthRaymarch.MarchMulti marches this camera's rays against all slices; nearest
    ///      refined crossing wins; hits are shaded by averaging the cameras that agree (Step 3
    ///      replaces this with view-dependent weights).
    ///
    /// Slices are sized to the LARGEST upright frame; smaller cameras zero-pad (depth 0 = invalid)
    /// and carry their true resolution in the camera buffer. Mono matrices (same VR caveat as
    /// RaymarchCompositor): validate with XR off / a free test camera first.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MultiCameraRaymarch : MonoBehaviour
    {
        /// <summary>One depth camera feed + its pixel-mapping settings (see NativePixelTransform).</summary>
        [System.Serializable]
        public class Source
        {
            public DrawMeshInstanced renderer;
            [Tooltip("Spot body cameras (front/left/right/back) = Transpose. Hand camera = Upright.")]
            public ImageOrientation orientation = ImageOrientation.Transpose;
            [Tooltip("Labeling flips — relabel texture coords without changing geometry (see R1).")]
            public bool flipU = false;
            public bool flipV = true;
            [Tooltip("Native CVD buffers store pixels 180°-rotated w.r.t. their intrinsics; leave ON.")]
            public bool mirrorNativeBuffer = true;
            [Tooltip("Color texture origin compensation (validated flipV=true for body cameras).")]
            public bool colorFlipU = false;
            public bool colorFlipV = true;

            public NativePixelTransform PixelTransform =>
                new NativePixelTransform(orientation, flipU, flipV, mirrorNativeBuffer);

            public bool Ready =>
                renderer != null && renderer.HasFrameData && renderer.PointDepthBuffer != null
                && renderer.FrameWidth > 0 && renderer.FrameHeight > 0;
        }

        /// <summary>Keep in lockstep with MAX_CAMERAS in MultiDepthRaymarch.compute.</summary>
        public const int MaxCameras = 8;

        [Header("Source depth cameras")]
        public List<Source> sources = new List<Source>();

        [Header("Compute shaders")]
        public ComputeShader packShader;   // DepthToUpright.compute
        public ComputeShader marchShader;  // MultiDepthRaymarch.compute

        [Header("March params")]
        public float near = 0.1f;
        public float far = 10f;
        [Range(8, 1024)] public int steps = 256;
        public float depthEps = 0.05f;
        [Tooltip("Tolerance for the per-camera color visibility/agreement test (occlusion). Kept " +
                 "separate from (and never tighter than) depthEps: tighten depthEps to clean up " +
                 "geometry without starving color into magenta. Raise if you see magenta on " +
                 "surfaces a camera clearly sees.")]
        public float colorAgreeEps = 0.1f;
        public float minValidDepth = 0.05f;
        [Tooltip("Reject crossings where stored depth jumps more than this (m) — silhouette edges.")]
        public float discontinuityThreshold = 0.15f;

        [Header("Color")]
        public bool useColor = true;

        [Header("Composite")]
        [Tooltip("Composite the reconstruction over the camera image. Off = show it on black.")]
        public bool composite = true;
        [Tooltip("When compositing, reject raymarch hits behind already-rendered Unity geometry.")]
        public bool respectSceneDepth = true;
        [Tooltip("Depth-test tolerance in metres; prevents equal-depth validation overlays from flickering out.")]
        public float sceneDepthBias = 0.02f;
        [Tooltip("Uniform vertical flip of the composited output (escape hatch; normally off).")]
        public bool flipOutputV = false;

        [Header("Validation")]
        [Range(0f, 1f)]
        [Tooltip("Opacity of the reconstruction over the scene; ~0.5 to overlay-check vs the cloud.")]
        public float marchBlend = 1f;
        [Tooltip("Which source 'Snap this camera to a source pose' uses.")]
        public int snapSourceIndex = 0;

        [Header("Debug")]
        [Tooltip("Logs whether OnRenderImage fires, active camera count, and pass/composite state.")]
        public bool enableDebugLog = true;

        // KEEP IN LOCKSTEP with CameraGPU in MultiDepthRaymarch.compute. Matrices as explicit
        // columns so the GPU layout is unambiguous.
        [StructLayout(LayoutKind.Sequential)]
        private struct CameraGPU
        {
            public Vector4 intrinsics;   // (cx, cy, fx, fy)
            public Vector4 resolution;   // (width, height, hasColor ? 1 : 0, unused)
            public Vector4 camToWorldC0;
            public Vector4 camToWorldC1;
            public Vector4 camToWorldC2;
            public Vector4 camToWorldC3;
            public Vector4 worldToCamC0;
            public Vector4 worldToCamC1;
            public Vector4 worldToCamC2;
            public Vector4 worldToCamC3;
        }
        private const int CameraGPUStride = 10 * 16; // ten float4s

        private Camera cam;
        private RenderTexture uprightDepthArr;
        private RenderTexture uprightColorArr;
        private RenderTexture composited;
        private ComputeBuffer cameraBuffer;
        private readonly CameraGPU[] cameraData = new CameraGPU[MaxCameras];
        private readonly List<Source> active = new List<Source>(MaxCameras);
        private readonly List<DepthCameraModel> activeModels = new List<DepthCameraModel>(MaxCameras);
        private int packKernel = -1;
        private int marchKernel = -1;
        private string lastLogState;

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            // OnRenderImage supplies color only. Scene-depth rejection needs Unity's
            // camera depth texture generated alongside the color pass.
            cam.depthTextureMode |= DepthTextureMode.Depth;
            if (packShader != null) packKernel = packShader.FindKernel("PackArray");
            if (marchShader != null) marchKernel = marchShader.FindKernel("MarchMulti");
            if (enableDebugLog)
                Debug.Log($"[MultiCameraRaymarch] Enabled on '{name}'. " +
                          $"packKernel={packKernel}, marchKernel={marchKernel}, sources={sources.Count}.");
        }

        private void OnDisable()
        {
            ReleaseRT(ref uprightDepthArr);
            ReleaseRT(ref uprightColorArr);
            ReleaseRT(ref composited);
            cameraBuffer?.Release();
            cameraBuffer = null;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            CollectActive();
            bool ready = cam != null && packShader != null && marchShader != null
                         && packKernel >= 0 && marchKernel >= 0 && active.Count > 0;

            if (enableDebugLog)
            {
                string state = ready
                    ? $"COMPOSITE ({active.Count}/{sources.Count} cameras)"
                    : "PASSTHROUGH ("
                        + $"packShader={(packShader != null)}, marchShader={(marchShader != null)}, "
                        + $"packKernel={packKernel}, marchKernel={marchKernel}, "
                        + $"activeSources={active.Count}/{sources.Count})";
                if (state != lastLogState || Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[MultiCameraRaymarch] OnRenderImage on '{name}' [{src.width}x{src.height}] " +
                              $"frame {Time.frameCount}: {state}");
                    lastLogState = state;
                }
            }

            if (!ready)
            {
                Graphics.Blit(src, dst);
                return;
            }
            // Preserve depth output even if another component rewrites depthTextureMode.
            cam.depthTextureMode |= DepthTextureMode.Depth;

            // Build the models and find the slice (max) dimensions.
            activeModels.Clear();
            int maxW = 0;
            int maxH = 0;
            foreach (Source s in active)
            {
                DepthCameraModel model = DepthCameraModelBuilder.BuildFor(s.renderer, s.PixelTransform);
                activeModels.Add(model);
                maxW = Mathf.Max(maxW, model.width);
                maxH = Mathf.Max(maxH, model.height);
            }
            if (maxW <= 0 || maxH <= 0)
            {
                Graphics.Blit(src, dst);
                return;
            }

            EnsureArray(ref uprightDepthArr, maxW, maxH, active.Count, RenderTextureFormat.RFloat,
                RenderTextureReadWrite.Default);
            // Linear RT: we UAV-store already-linear color; a default (sRGB) RT would decode again
            // on Load and darken everything in a Linear project (same rationale as Step 1).
            EnsureArray(ref uprightColorArr, maxW, maxH, active.Count, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            EnsureRT(ref composited, src.width, src.height, RenderTextureFormat.ARGBFloat);

            // 1) Pack every active camera into its slice + fill the camera buffer entry.
            for (int i = 0; i < active.Count; i++)
            {
                Source s = active[i];
                DepthCameraModel model = activeModels[i];
                bool hasColor = useColor && s.renderer.colorImage != null;

                packShader.SetInt("_NativeWidth", s.renderer.FrameWidth);
                packShader.SetInt("_NativeHeight", s.renderer.FrameHeight);
                packShader.SetInt("_UprightWidth", model.width);
                packShader.SetInt("_UprightHeight", model.height);
                packShader.SetInt("_SliceWidth", maxW);
                packShader.SetInt("_SliceHeight", maxH);
                packShader.SetInt("_Slice", i);
                s.PixelTransform.ApplyTo(packShader); // same transform as the model build (R1)
                packShader.SetInt("_HasColor", hasColor ? 1 : 0);
                packShader.SetInt("_ColorFlipU", s.colorFlipU ? 1 : 0);
                packShader.SetInt("_ColorFlipV", s.colorFlipV ? 1 : 0);
                packShader.SetBuffer(packKernel, "_PointDepth", s.renderer.PointDepthBuffer);
                packShader.SetTexture(packKernel, "_NativeColor",
                    s.renderer.colorImage != null ? (Texture)s.renderer.colorImage : Texture2D.whiteTexture);
                packShader.SetTexture(packKernel, "_UprightDepthArr", uprightDepthArr);
                packShader.SetTexture(packKernel, "_UprightColorArr", uprightColorArr);
                packShader.Dispatch(packKernel, Mathf.CeilToInt(maxW / 8f), Mathf.CeilToInt(maxH / 8f), 1);

                cameraData[i] = new CameraGPU
                {
                    intrinsics = new Vector4(model.cx, model.cy, model.fx, model.fy),
                    resolution = new Vector4(model.width, model.height, hasColor ? 1f : 0f, 0f),
                    camToWorldC0 = model.cameraToWorld.GetColumn(0),
                    camToWorldC1 = model.cameraToWorld.GetColumn(1),
                    camToWorldC2 = model.cameraToWorld.GetColumn(2),
                    camToWorldC3 = model.cameraToWorld.GetColumn(3),
                    worldToCamC0 = model.worldToCamera.GetColumn(0),
                    worldToCamC1 = model.worldToCamera.GetColumn(1),
                    worldToCamC2 = model.worldToCamera.GetColumn(2),
                    worldToCamC3 = model.worldToCamera.GetColumn(3),
                };
            }

            if (cameraBuffer == null)
                cameraBuffer = new ComputeBuffer(MaxCameras, CameraGPUStride);
            cameraBuffer.SetData(cameraData, 0, 0, active.Count);

            // 2) March this camera's rays against all slices and composite over the scene.
            Matrix4x4 invVP = (cam.projectionMatrix * cam.worldToCameraMatrix).inverse;
            Vector3 eye = cam.cameraToWorldMatrix.GetColumn(3); // true eye origin (R2-correct path)
            bool useSceneDepth = composite && respectSceneDepth;
            marchShader.SetBuffer(marchKernel, "_Cameras", cameraBuffer);
            marchShader.SetInt("_CameraCount", active.Count);
            marchShader.SetVector("_ViewEyePos", eye);
            marchShader.SetMatrix("_InvViewProj", invVP);
            marchShader.SetMatrix("_ViewWorldToCamera", cam.worldToCameraMatrix);
            marchShader.SetVector("_ZBufferParams", Shader.GetGlobalVector("_ZBufferParams"));
            marchShader.SetInt("_OutWidth", src.width);
            marchShader.SetInt("_OutHeight", src.height);
            marchShader.SetFloat("_Near", near);
            marchShader.SetFloat("_Far", far);
            marchShader.SetInt("_Steps", steps);
            marchShader.SetFloat("_DepthEps", depthEps);
            // Color agreement is never stricter than crossing detection (else hits go magenta).
            marchShader.SetFloat("_ColorAgreeEps", Mathf.Max(depthEps, colorAgreeEps));
            marchShader.SetFloat("_MinValidDepth", minValidDepth);
            marchShader.SetFloat("_DiscontinuityThreshold", discontinuityThreshold);
            marchShader.SetInt("_FlipV", flipOutputV ? 1 : 0);
            marchShader.SetInt("_UseColor", useColor ? 1 : 0);
            marchShader.SetInt("_Composite", composite ? 1 : 0);
            marchShader.SetFloat("_HitBlend", marchBlend);
            marchShader.SetInt("_UseSceneDepth", useSceneDepth ? 1 : 0);
            marchShader.SetFloat("_SceneDepthBias", Mathf.Max(0f, sceneDepthBias));
            marchShader.SetTexture(marchKernel, "_UprightDepthArr", uprightDepthArr);
            marchShader.SetTexture(marchKernel, "_UprightColorArr", uprightColorArr);
            marchShader.SetTexture(marchKernel, "_SceneColor", src);
            // Built-in pipeline exposes the camera depth texture globally; the compute
            // shader uses it only to reject occluded raymarch hits.
            if (useSceneDepth)
                marchShader.SetTextureFromGlobal(marchKernel, "_SceneDepth", "_CameraDepthTexture");
            else
                marchShader.SetTexture(marchKernel, "_SceneDepth", Texture2D.blackTexture);
            marchShader.SetTexture(marchKernel, "_Output", composited);
            marchShader.Dispatch(marchKernel,
                Mathf.CeilToInt(src.width / 8f), Mathf.CeilToInt(src.height / 8f), 1);

            Graphics.Blit(composited, dst);
        }

        private void CollectActive()
        {
            active.Clear();
            foreach (Source s in sources)
            {
                if (s != null && s.Ready)
                {
                    active.Add(s);
                    if (active.Count >= MaxCameras)
                        break;
                }
            }
        }

        // Identity test against ONE source: move this camera to that source's pose and match its
        // vertical FOV; the march should reproduce that camera's own view (plus the others' data).
        [ContextMenu("Snap this camera to a source pose (snapSourceIndex)")]
        public void SnapToSourcePose()
        {
            Camera c = GetComponent<Camera>();
            if (c == null || snapSourceIndex < 0 || snapSourceIndex >= sources.Count
                || sources[snapSourceIndex] == null || sources[snapSourceIndex].renderer == null)
            {
                Debug.LogWarning("[MultiCameraRaymarch] Need a Camera and a valid snapSourceIndex.");
                return;
            }

            Source s = sources[snapSourceIndex];
            DepthCameraModel model = DepthCameraModelBuilder.BuildFor(s.renderer, s.PixelTransform);
            Matrix4x4 c2w = model.cameraToWorld;
            c.transform.SetPositionAndRotation(c2w.GetColumn(3), c2w.rotation);
            float vfov = 2f * Mathf.Atan2(model.height * 0.5f, Mathf.Abs(model.fy)) * Mathf.Rad2Deg;
            c.fieldOfView = Mathf.Clamp(vfov, 1f, 179f);
            Debug.Log($"[MultiCameraRaymarch] Snapped to source {snapSourceIndex} " +
                      $"('{s.renderer.name}'); vFOV set to {vfov:F1} deg.");
        }

        private static void EnsureArray(ref RenderTexture rt, int w, int h, int slices,
            RenderTextureFormat fmt, RenderTextureReadWrite rw)
        {
            if (rt != null && rt.width == w && rt.height == h && rt.volumeDepth == slices
                && rt.format == fmt && rt.sRGB == DesiredSRGB(fmt, rw))
                return;
            ReleaseRT(ref rt);
            rt = new RenderTexture(w, h, 0, fmt, rw)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = slices,
                enableRandomWrite = true,
            };
            rt.Create();
        }

        private static void EnsureRT(ref RenderTexture rt, int w, int h, RenderTextureFormat fmt,
            RenderTextureReadWrite rw = RenderTextureReadWrite.Default)
        {
            if (rt != null && rt.width == w && rt.height == h && rt.format == fmt
                && rt.sRGB == DesiredSRGB(fmt, rw))
                return;
            ReleaseRT(ref rt);
            rt = new RenderTexture(w, h, 0, fmt, rw) { enableRandomWrite = true };
            rt.Create();
        }

        private static bool DesiredSRGB(RenderTextureFormat fmt, RenderTextureReadWrite rw)
        {
            if (rw == RenderTextureReadWrite.Linear)
                return false;
            if (rw == RenderTextureReadWrite.sRGB)
                return true;
            return QualitySettings.activeColorSpace == ColorSpace.Linear
                && (fmt == RenderTextureFormat.ARGB32 || fmt == RenderTextureFormat.Default);
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
