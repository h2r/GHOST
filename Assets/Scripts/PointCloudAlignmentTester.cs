using System.Collections.Generic;
using UnityEngine;

// Live test harness: grabs one frame from each of two SpotObserverClient
// cameras, backprojects both to point clouds via DepthBackprojection, and
// runs FourPointCongruentSets.Solve between them. This is the first place in
// the C# port that touches real captured data instead of synthetic clouds --
// see FourPointCongruentSetsSelfTest for the synthetic version used to
// validate the port itself.
//
// Only wires up a single camera per side for now. If a side needs more
// coverage (e.g. front-left + front-right, like DrawMeshInstanced fuses via
// CVD), call DepthBackprojection.Backproject() once per camera and
// concatenate the resulting lists before handing them to
// FourPointCongruentSets.Solve -- it doesn't care how many cameras a cloud
// came from, only that it's a flat list of points in a consistent frame.
//
// No ICP refinement here yet (not ported) -- this only reports the 4PCS
// initial alignment, which is expected to be in the right ballpark but not
// tightly converged, same as during the Python validation.
public class PointCloudAlignmentTester : MonoBehaviour
{
    [Header("Source cloud (aligned onto Target)")]
    public SpotObserverClient sourceClient;
    public int sourceStreamIndex;
    public SpotObserverClient.SpotCamera sourceCamera;
    // Optional: gives real intrinsics (CX/CY/FX/FY). Without one, falls back
    // to a frame-center/max-dimension guess, same fallback DrawMeshInstanced
    // uses when it has no CVD reference.
    public PoseConsistentVideoDepth sourceCVD;

    [Header("Target cloud")]
    public SpotObserverClient targetClient;
    public int targetStreamIndex;
    public SpotObserverClient.SpotCamera targetCamera;
    public PoseConsistentVideoDepth targetCVD;

    [Header("4PCS parameters")]
    public int iterations = 300;
    public float maxDistance = 0.1f;
    public float minSpread = 0.3f;
    public float maxSpread = 1.2f;
    public int seed = 7;

    [Header("Dominant plane (floor) rejection")]
    public bool useDominantPlaneRejection = true;
    public int planeFitIterations = 300;
    public float planeFitThreshold = 0.04f;

    [ContextMenu("Run Alignment On Live Clouds")]
    public void RunAlignment()
    {
        if (!TryBackprojectFrame(sourceClient, sourceStreamIndex, sourceCamera, sourceCVD, "source", out var source))
            return;
        if (!TryBackprojectFrame(targetClient, targetStreamIndex, targetCamera, targetCVD, "target", out var target))
            return;

        Debug.Log($"[PointCloudAlignmentTester] backprojected source={source.Count} pts, target={target.Count} pts");

        Vector3? planeNormal = null;
        float planeOffset = 0f;
        if (useDominantPlaneRejection)
        {
            var plane = FourPointCongruentSets.FitDominantPlane(source, planeFitIterations, planeFitThreshold, seed);
            if (plane.Valid)
            {
                planeNormal = plane.Normal;
                planeOffset = plane.Offset;
                Debug.Log($"[PointCloudAlignmentTester] dominant plane found: normal={plane.Normal}, offset={plane.Offset:F3}");
            }
            else
            {
                Debug.LogWarning("[PointCloudAlignmentTester] no dominant plane found in source cloud -- proceeding without plane rejection.");
            }
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = FourPointCongruentSets.Solve(
            source, target,
            iterations: iterations, maxDistance: maxDistance,
            minSpread: minSpread, maxSpread: maxSpread,
            seed: seed,
            dominantPlaneNormal: planeNormal, dominantPlaneOffset: planeOffset);
        stopwatch.Stop();

        Vector3 t = result.Translation;
        Debug.Log($"[PointCloudAlignmentTester] 4PCS done in {stopwatch.ElapsedMilliseconds}ms. "
                  + $"score={result.Score}/{source.Count}, translation=({t.x:F3}, {t.y:F3}, {t.z:F3})\n"
                  + $"rotation=\n{result.Rotation}");
    }

    private bool TryBackprojectFrame(SpotObserverClient client, int streamIndex, SpotObserverClient.SpotCamera camera,
                                      PoseConsistentVideoDepth cvd, string label, out List<Vector3> points)
    {
        points = null;

        if (client == null)
        {
            Debug.LogError($"[PointCloudAlignmentTester] {label} client is not assigned.");
            return false;
        }

        if (!client.TryGetCameraFrame(streamIndex, camera, out var frame))
        {
            Debug.LogError($"[PointCloudAlignmentTester] no fresh frame available for {label} "
                            + $"(stream {streamIndex}, camera {camera}). Is the client connected and streaming?");
            return false;
        }

        var (cx, cy, fx, fy) = GetIntrinsics(cvd, frame.Width, frame.Height);
        points = DepthBackprojection.Backproject(frame, cx, cy, fx, fy);

        if (points.Count == 0)
        {
            Debug.LogError($"[PointCloudAlignmentTester] {label} backprojected to 0 points -- depth frame may be empty.");
            return false;
        }

        return true;
    }

    // Mirrors DrawMeshInstanced.GetIntrinsicsVector()'s fallback: without a
    // CVD reference, guess frame-center principal point and max-dimension
    // focal length.
    private static (float cx, float cy, float fx, float fy) GetIntrinsics(PoseConsistentVideoDepth cvd, int width, int height)
    {
        if (cvd != null)
            return (cvd.CX, cvd.CY, cvd.FX, cvd.FY);

        float cx = width * 0.5f;
        float cy = height * 0.5f;
        float fallbackFocalLength = Mathf.Max(width, height);
        return (cx, cy, fallbackFocalLength, fallbackFocalLength);
    }
}
