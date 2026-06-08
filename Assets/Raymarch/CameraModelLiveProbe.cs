using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Raymarch
{
    /// <summary>
    /// Step 0 reconciliation tool. Bridges a live <see cref="DrawMeshInstanced"/> camera to the
    /// canonical <see cref="DepthCameraModel"/>, measures the pixel-convention correction, and
    /// visualizes the result with persistent gizmos in the Scene view.
    ///
    /// Two visualizations (drawn as gizmo spheres so there are no timing/visibility pitfalls):
    ///   - Per-sample crosses: gray = the exact world point the legacy renderer draws; green = the
    ///     baked clean-model reconstruction (should overlay gray when orientation is correct).
    ///   - Frustum-corner test: color-coded markers for each image corner at a fixed depth, so the
    ///     camera's orientation/handedness is unambiguous (blue = image top-left, red = top-right,
    ///     yellow = bottom-left, magenta = bottom-right, white = center).
    ///
    /// View in the SCENE tab during Play (gizmos on by default). For the Game/VR view, enable the
    /// "Gizmos" toggle. This tool only READS GPU state; it never mutates the renderer.
    /// Run via the "Sample &amp; Compare Now" context menu, or enable <see cref="continuous"/>.
    /// </summary>
    public class CameraModelLiveProbe : MonoBehaviour
    {
        public enum Candidate
        {
            LegacyOnly,
            Identity,        // canonical fed (u=col, v=row), depth sampled at (col, row)
            Transpose,       // canonical fed (u=row, v=col), depth sampled at (col, row)
            CleanTransposed, // the BAKED model from DepthCameraModelBuilder; should overlay red
        }

        [Header("Source")]
        public DrawMeshInstanced sourceRenderer;

        [Header("Sampling")]
        [Tooltip("Interior grid of sample pixels per axis (e.g. 3 -> a 3x3 grid at 1/4, 1/2, 3/4).")]
        public int gridPerAxis = 3;
        public bool continuous = false;
        [Tooltip("When continuous, sample every N frames to limit GPU readback stalls.")]
        public int sampleIntervalFrames = 30;
        [Tooltip("Depth below this (metres) is treated as invalid/empty and skipped.")]
        public float minValidDepth = 0.01f;

        [Header("Model")]
        [Tooltip("Orientation used by the CleanTransposed candidate and the frustum test. " +
                 "Body cameras (front/left/right/back) = Transpose; the hand camera = Upright.")]
        public ImageOrientation orientation = ImageOrientation.Transpose;

        [Header("Per-sample crosses")]
        public Candidate candidate = Candidate.CleanTransposed;
        public float gizmoRadius = 0.04f;
        public bool drawRayFromCamera = false;

        [Header("Frustum-corner orientation test")]
        [Tooltip("Draws color-coded markers at the image corners to reveal handedness/orientation.")]
        public bool frustumTest = true;
        [Tooltip("Fixed depth (m) at which the corner markers are placed, regardless of scene depth.")]
        public float frustumTestDepth = 1.5f;
        [Range(0f, 0.4f)]
        [Tooltip("Inset from the image edge (fraction) for the corner markers.")]
        public float frustumInset = 0.08f;
        [Tooltip("Flip the de-rotated image horizontally (left<->right) for the frustum / shipped model.")]
        public bool flipU = false;
        [Tooltip("Flip the de-rotated image vertically (top<->bottom). Set this per the corner test.")]
        public bool flipV = false;

        [Header("Debug visibility")]
        [Tooltip("Always draw anchor wire spheres (cyan = this object, orange = source camera origin) " +
                 "so you can confirm gizmos render at all and find where to look.")]
        public bool alwaysOnAnchor = true;
        public float anchorRadius = 0.25f;

        private struct Mark
        {
            public Vector3 a;
            public Vector3 b;
            public Color color;
            public bool isLine;
            public float radius;
        }

        private readonly List<Mark> marks = new List<Mark>();
        private Vector3[] depthData;
        private int cachedCount = -1;
        private bool warned;

        private void Update()
        {
            if (continuous && sampleIntervalFrames > 0 && Time.frameCount % sampleIntervalFrames == 0)
                SampleAndCompare();
        }

        // One-click preset so we don't depend on manually setting the Inspector dropdown/checkbox
        // (Unity can leave a pre-existing component's enum at its serialized/zero value).
        [ContextMenu("Preset: Body camera (Transpose + Flip V), then sample")]
        public void PresetBodyCameraAndSample()
        {
            candidate = Candidate.CleanTransposed; // green must come from the baked clean model, not Identity
            orientation = ImageOrientation.Transpose;
            flipU = false;
            flipV = true;
            Debug.Log("[CameraModelLiveProbe] Preset set: candidate=CleanTransposed, " +
                      "orientation=Transpose, flipU=False, flipV=True.");
            SampleAndCompare();
        }

        [ContextMenu("Sample & Compare Now")]
        public void SampleAndCompare()
        {
            if (sourceRenderer == null)
            {
                Debug.LogWarning("[CameraModelLiveProbe] No sourceRenderer assigned.");
                return;
            }

            ComputeBuffer buffer = sourceRenderer.PointDepthBuffer;
            int w = sourceRenderer.FrameWidth;
            int h = sourceRenderer.FrameHeight;

            if (buffer == null || w <= 0 || h <= 0 || !sourceRenderer.HasFrameData)
            {
                if (!warned)
                {
                    Debug.LogWarning("[CameraModelLiveProbe] Renderer has no live depth yet " +
                                     "(buffer null or no frame data). Will retry on next sample.");
                    warned = true;
                }
                return;
            }
            warned = false;

            int count = w * h;
            if (depthData == null || cachedCount != count)
            {
                depthData = new Vector3[count];
                cachedCount = count;
            }

            // Synchronous readback: fine for an on-demand / low-rate debug probe.
            buffer.GetData(depthData);

            Vector4 intr = sourceRenderer.Intrinsics; // (cx, cy, fx, fy)
            Matrix4x4 cameraToWorld = sourceRenderer.GetCurrentPose();
            Vector3 camPos = cameraToWorld.GetColumn(3);
            DepthCameraModel model = DepthCameraModel.FromIntrinsicsVector(intr, w, h, cameraToWorld);
            // The baked clean model (folds away the transpose); used by CleanTransposed + frustum test.
            DepthCameraModel cleanModel = DepthCameraModelBuilder.BuildFor(sourceRenderer, orientation);

            marks.Clear();

            int n = Mathf.Max(1, gridPerAxis);
            var sb = new StringBuilder();
            sb.AppendLine($"[CameraModelLiveProbe] '{sourceRenderer.name}'  {w}x{h}  " +
                          $"intr(cx,cy,fx,fy)=({intr.x:F1},{intr.y:F1},{intr.z:F1},{intr.w:F1})  " +
                          $"candidate={candidate}  orientation={orientation}");
            sb.AppendLine("  px(col,row)  depth   -> canonical needs (u*, v*, d*)   [u*-col, v*-row]");

            for (int gy = 1; gy <= n; gy++)
            {
                for (int gx = 1; gx <= n; gx++)
                {
                    int col = Mathf.Clamp(Mathf.RoundToInt(w * gx / (float)(n + 1)), 0, w - 1);
                    int row = Mathf.Clamp(Mathf.RoundToInt(h * gy / (float)(n + 1)), 0, h - 1);

                    // --- Legacy reproduction (exact match to CSMain) ---
                    int mirrorIndex = (w * (h - row - 1)) + (w - col - 1);
                    float legacyDepth = depthData[mirrorIndex].z;
                    if (legacyDepth <= minValidDepth)
                        continue;

                    Vector3 legacyCamLocal = DepthCameraModel.LegacyGoLocalCenter(col, row, legacyDepth, intr);
                    Vector3 legacyWorld = cameraToWorld.MultiplyPoint3x4(legacyCamLocal);

                    // --- Exact correction measurement ---
                    Vector3 star = model.ProjectFromWorld(legacyWorld); // (u*, v*, d*)
                    sb.AppendLine($"  ({col,4},{row,4})  {legacyDepth,6:F3}  -> " +
                                  $"({star.x,7:F1}, {star.y,7:F1}, {star.z,6:F3})   " +
                                  $"[{star.x - col,6:F1}, {star.y - row,6:F1}]");

                    AddPoint(legacyWorld, Color.gray);
                    if (drawRayFromCamera)
                        AddLine(camPos, legacyWorld, Color.gray * 0.5f);

                    // --- Optional candidate canonical reconstruction (clean convention) ---
                    if (candidate != Candidate.LegacyOnly)
                    {
                        Vector3 candWorld = Vector3.zero;
                        bool haveCand = true;

                        if (candidate == Candidate.CleanTransposed)
                        {
                            // Verify the baked model reproduces the legacy geometry: feed it the SAME
                            // (mirrored) depth as red, so only the projection convention differs.
                            // Green should land on top of red.
                            Vector2 muv = DepthCameraModelBuilder.NativeToModelPixel(col, row, orientation);
                            candWorld = cleanModel.UnprojectToWorld(muv.x, muv.y, legacyDepth);
                        }
                        else
                        {
                            int idxIdentity = col + row * w;
                            float depthIdentity = depthData[idxIdentity].z;
                            if (depthIdentity > minValidDepth)
                                candWorld = candidate == Candidate.Transpose
                                    ? model.UnprojectToWorld(row, col, depthIdentity)
                                    : model.UnprojectToWorld(col, row, depthIdentity);
                            else
                                haveCand = false;
                        }

                        if (haveCand)
                            AddPoint(candWorld, Color.green, gizmoRadius * 0.6f);
                    }
                }
            }

            if (frustumTest)
                AddFrustumCorners(cleanModel, camPos, sb);

            // Diagnostics: confirm marks were created and report WHERE they live in world space,
            // so the Scene view can be framed on them.
            Vector3 centroid = Vector3.zero;
            int pts = 0;
            foreach (Mark mk in marks)
            {
                if (!mk.isLine) { centroid += mk.a; pts++; }
            }
            if (pts > 0) centroid /= pts;
            sb.AppendLine($"  marks={marks.Count} ({pts} spheres)  cameraOrigin(world)={camPos}  " +
                          $"sphereCentroid(world)={centroid}");

            Debug.Log(sb.ToString());
        }

        // Places color-coded markers at the four image corners (+ center) at a fixed depth so the
        // camera frame's orientation in 3D is unambiguous. Uses the clean model + current orientation.
        private void AddFrustumCorners(DepthCameraModel cleanModel, Vector3 camPos, StringBuilder sb)
        {
            int w = (int)cleanModel.width;   // model-space (de-rotated) dimensions
            int h = (int)cleanModel.height;
            float insX = Mathf.Clamp01(frustumInset) * w;
            float insY = Mathf.Clamp01(frustumInset) * h;
            float d = Mathf.Max(0.05f, frustumTestDepth);

            // Model-space (u, v): u = column (right+), v = row (down+).
            AddCorner(cleanModel, camPos, insX, insY, d, Color.blue, "top-left");
            AddCorner(cleanModel, camPos, w - 1 - insX, insY, d, Color.red, "top-right");
            AddCorner(cleanModel, camPos, insX, h - 1 - insY, d, Color.yellow, "bottom-left");
            AddCorner(cleanModel, camPos, w - 1 - insX, h - 1 - insY, d, Color.magenta, "bottom-right");
            AddCorner(cleanModel, camPos, w * 0.5f, h * 0.5f, d, Color.white, "center");


            sb.AppendLine($"  frustum test @ {d:F2}m (flipU={flipU}, flipV={flipV}): " +
                          "blue=top-left  red=top-right  yellow=bottom-left  magenta=bottom-right  white=center");
        }

        private string marks_str()
        {
            var sb = new StringBuilder();
            sb.AppendLine("  marks:");
            for (int i = 0; i < marks.Count; i++)
            {
                Mark m = marks[i];
                if (m.isLine)
                    sb.AppendLine($"    line from {m.a} to {m.b}  color={m.color}");
                else
                    sb.AppendLine($"    point at {m.a}  color={m.color}  radius={m.radius}");
            }
            return sb.ToString();
        }

        // (u, v) are the desired SHIP (final, upright) image corner; flipU/flipV map ship->model
        // pixels before unprojecting, so the colors always label the shipped image's corners.
        private void AddCorner(DepthCameraModel cleanModel, Vector3 camPos, float u, float v, float d, Color c, string label)
        {
            float mu = flipU ? (cleanModel.width - 1 - u) : u;
            float mv = flipV ? (cleanModel.height - 1 - v) : v;
            Vector3 p = cleanModel.UnprojectToWorld(mu, mv, d);
            AddPoint(p, c, gizmoRadius * 1.4f);
            AddLine(camPos, p, c);
        }

        private void AddPoint(Vector3 p, Color c)
        {
            AddPoint(p, c, gizmoRadius);
        }

        private void AddPoint(Vector3 p, Color c, float radius)
        {
            marks.Add(new Mark { a = p, color = c, isLine = false, radius = radius });
        }

        private void AddLine(Vector3 a, Vector3 b, Color c)
        {
            marks.Add(new Mark { a = a, b = b, color = c, isLine = true });
        }

        private void OnDrawGizmos()
        {
            // Always-on anchors, independent of sampling. If you can see these, gizmos render fine
            // and you are looking in the right place; if not, it's a view/gizmo-toggle problem.
            // Cyan = this probe object's transform; orange = the source camera origin.
            if (alwaysOnAnchor)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, anchorRadius);
                if (sourceRenderer != null)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f);
                    Gizmos.DrawWireSphere(sourceRenderer.GetCurrentPose().GetColumn(3), anchorRadius);
                }
            }

            for (int i = 0; i < marks.Count; i++)
            {
                Mark m = marks[i];
                Gizmos.color = m.color;
                if (m.isLine)
                    Gizmos.DrawLine(m.a, m.b);
                else
                    Gizmos.DrawSphere(m.a, m.radius);
            }
        }
    }
}
