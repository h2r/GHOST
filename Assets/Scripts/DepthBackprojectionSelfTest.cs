using System.Collections.Generic;
using UnityEngine;

// Self-check for DepthBackprojection.Backproject(), same spirit and style as
// KabschSelfTest / FourPointCongruentSetsSelfTest. Uses a synthetic depth
// buffer with hand-computable expected output (no live camera needed), since
// the pinhole formula is simple enough that exact values are known ahead of
// time -- unlike the other two self-tests, this one should pass to within
// floating-point precision, not a loose tolerance.
public class DepthBackprojectionSelfTest : MonoBehaviour
{
    [ContextMenu("Run DepthBackprojection Self-Test")]
    public void RunTests()
    {
        bool allPassed = true;
        allPassed &= TestKnownPixelsRecoverExpectedXYZ();
        allPassed &= TestZeroDepthPixelsAreExcluded();

        if (allPassed)
            Debug.Log("[DepthBackprojectionSelfTest] ALL TESTS PASSED");
        else
            Debug.LogError("[DepthBackprojectionSelfTest] ONE OR MORE TESTS FAILED -- see above");
    }

    private void Start()
    {
        RunTests();
    }

    // A 4x4 depth image, all pixels at a known constant depth, with known
    // intrinsics. For a pixel exactly at (cx, cy), backprojection should give
    // (0, 0, z). For an offset pixel, x/y should scale linearly with offset
    // and depth, matching (u - cx) * z / fx and (v - cy) * z / fy by hand.
    private bool TestKnownPixelsRecoverExpectedXYZ()
    {
        const int width = 4, height = 4;
        const float cx = 2f, cy = 2f, fx = 100f, fy = 100f, z = 2f;

        float[] depth = new float[width * height];
        for (int i = 0; i < depth.Length; i++) depth[i] = z;

        List<Vector3> points = DepthBackprojection.Backproject(depth, width, height, cx, cy, fx, fy);

        bool pass = points.Count == width * height;

        // pixel (u=2, v=2) sits exactly on the principal point -> (0, 0, z)
        int centerIndex = 2 * width + 2;
        Vector3 centerPoint = points[centerIndex];
        Vector3 expectedCenter = new Vector3(0f, 0f, z);
        float centerError = Vector3.Distance(centerPoint, expectedCenter);
        pass &= centerError < 1e-5f;

        // pixel (u=0, v=0): x = (0-2)*2/100 = -0.04, y = (0-2)*2/100 = -0.04
        Vector3 cornerPoint = points[0];
        Vector3 expectedCorner = new Vector3(-0.04f, -0.04f, z);
        float cornerError = Vector3.Distance(cornerPoint, expectedCorner);
        pass &= cornerError < 1e-5f;

        Debug.Log($"[DepthBackprojectionSelfTest] KnownPixelsRecoverExpectedXYZ: "
                  + $"count={points.Count}/{width * height}, centerError={centerError:E2}, "
                  + $"cornerError={cornerError:E2} -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }

    // Pixels with depth <= 0 (no return / invalid) must be dropped, not
    // turned into a bogus point at the origin or negative depth.
    private bool TestZeroDepthPixelsAreExcluded()
    {
        const int width = 3, height = 3;
        float[] depth = { 1f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f }; // checkerboard of valid/invalid

        List<Vector3> points = DepthBackprojection.Backproject(depth, width, height, 1f, 1f, 50f, 50f);

        int expectedValid = 5; // 5 nonzero entries above
        bool pass = points.Count == expectedValid;
        foreach (var p in points)
            pass &= p.z > 0f;

        Debug.Log($"[DepthBackprojectionSelfTest] ZeroDepthPixelsAreExcluded: "
                  + $"got {points.Count} points, expected {expectedValid} -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }
}
