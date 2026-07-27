using System.Collections.Generic;
using UnityEngine;

// Self-check for FourPointCongruentSets.Solve(), the same spirit as
// KabschSelfTest but for the coarser algorithm one level up. Builds a
// synthetic "room corner" cloud (a floor patch + a wall patch, two
// differently-oriented planes so the transform is fully constrained -- a
// single plane alone can't constrain rotation about its own normal, see the
// REPRODUCIBILITY NOTE in FourPointCongruentSets.cs and the floor-degeneracy
// discussion behind icp_align.py's floor_mask), applies a known transform,
// and checks Solve recovers something close to it.
//
// Tolerances here are deliberately loose compared to KabschSelfTest: 4PCS is
// a coarse initial-alignment step meant to be handed to ICP for refinement
// (not yet ported), not a millimeter-precise fit. This mainly exists to
// catch port bugs (spatial hash grid, coplanar base selection, diagonal
// pairing) by confirming the algorithm converges to a sane answer on
// structured synthetic data, matching how the Python version was validated.
//
// Like KabschSelfTest, this is a plain MonoBehaviour rather than an NUnit
// test for the same reason: Assembly-CSharp has no .asmdef, so a Tests
// assembly can't reference FourPointCongruentSets.cs without restructuring.
public class FourPointCongruentSetsSelfTest : MonoBehaviour
{
    [ContextMenu("Run FourPointCongruentSets Self-Test")]
    public void RunTest()
    {
        bool pass = TestRecoversKnownTransformOnCorner();

        if (pass)
            Debug.Log("[FourPointCongruentSetsSelfTest] ALL TESTS PASSED");
        else
            Debug.LogError("[FourPointCongruentSetsSelfTest] ONE OR MORE TESTS FAILED -- see above");
    }

    private void Start()
    {
        RunTest();
    }

    // Builds a floor patch (y = 0 plane) and a wall patch (x = -1.5 plane)
    // meeting at a corner, similar to what a real Spot capture of a room sees.
    private static List<Vector3> BuildCornerCloud()
    {
        var points = new List<Vector3>();

        // floor: y = 0, x/z in [-1.4, 1.4]
        for (float x = -1.4f; x <= 1.4f; x += 0.2f)
            for (float z = -1.4f; z <= 1.4f; z += 0.2f)
                points.Add(new Vector3(x, 0f, z));

        // wall: x = -1.5, y/z in [0.1, 1.5]
        for (float y = 0.1f; y <= 1.5f; y += 0.2f)
            for (float z = -1.4f; z <= 1.4f; z += 0.2f)
                points.Add(new Vector3(-1.5f, y, z));

        return points;
    }

    // Relative rotation angle between two rotation matrices, via the trace
    // identity: angle = acos((trace(a * b^T) - 1) / 2). Both matrices are
    // expected to be pure rotations padded to Matrix4x4 identity (as
    // Kabsch.Solve and FourPointCongruentSets.Solve both produce).
    private static float RotationAngleErrorDegrees(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 rel = a * b.transpose;
        float trace = rel[0, 0] + rel[1, 1] + rel[2, 2];
        float cosAngle = Mathf.Clamp((trace - 1f) * 0.5f, -1f, 1f);
        return Mathf.Acos(cosAngle) * Mathf.Rad2Deg;
    }

    private bool TestRecoversKnownTransformOnCorner()
    {
        List<Vector3> source = BuildCornerCloud();

        // known transform: 12 degrees of yaw (about Y) plus a modest translation --
        // similar magnitude to what showed up on real captures during validation
        // (12-26 degrees, ~1.1-1.5m translation)
        float angleRad = 12f * Mathf.Deg2Rad;
        Matrix4x4 trueRotation = Matrix4x4.identity;
        trueRotation[0, 0] = Mathf.Cos(angleRad); trueRotation[0, 2] = Mathf.Sin(angleRad);
        trueRotation[2, 0] = -Mathf.Sin(angleRad); trueRotation[2, 2] = Mathf.Cos(angleRad);
        Vector3 trueTranslation = new Vector3(0.4f, 0.05f, -0.2f);

        var target = new List<Vector3>(source.Count);
        foreach (var p in source)
            target.Add(trueRotation.MultiplyVector(p) + trueTranslation);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = FourPointCongruentSets.Solve(
            source, target,
            iterations: 500, maxDistance: 0.05f,
            minSpread: 0.3f, maxSpread: 2.0f,
            coplanarTol: 0.02f, distanceTol: 0.02f, eTol: 0.03f,
            seed: 7);
        stopwatch.Stop();

        float rotError = RotationAngleErrorDegrees(result.Rotation, trueRotation);
        float translationError = Vector3.Distance(result.Translation, trueTranslation);

        // loose thresholds -- see class comment. A real failure (bad port) shows up as
        // score <= 0 (no congruent set found at all) or errors of tens of degrees/meters,
        // not a few degrees/centimeters of slack.
        bool pass = result.Score > 0 && rotError < 5f && translationError < 0.1f;

        Debug.Log($"[FourPointCongruentSetsSelfTest] RecoversKnownTransformOnCorner: "
                  + $"{stopwatch.ElapsedMilliseconds}ms, score={result.Score}/{source.Count}, "
                  + $"rotError={rotError:F2}deg, translationError={translationError:F3}m "
                  + $"-> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }
}
