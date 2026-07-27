using System.Collections.Generic;
using UnityEngine;

// Self-check for Kabsch.Solve(), mirroring test_icp_align.py's
// test_kabsch_recovers_known_transform / test_kabsch_rejects_reflection /
// test_kabsch_minimum_points. This is the one thing in the C# port that
// couldn't be validated before landing (no C# compiler was available in the
// environment that wrote it) -- specifically, whether MathNet.Numerics'
// Svd() returns U/S/VT in the same convention Kabsch.cs assumes (numpy-style:
// VT is V-transpose, not V). If that assumption is wrong, results below will
// be visibly wrong (a transposed/incorrect rotation), not silently almost-right.
//
// This is a plain MonoBehaviour rather than a formal Unity Test Framework
// (NUnit) test on purpose: this project has no Tests assembly/.asmdef set up
// yet, and Kabsch.cs/FourPointCongruentSets.cs currently live in the default
// Assembly-CSharp (no asmdef of their own) -- a custom Tests.asmdef can't
// reference Assembly-CSharp directly, so wiring up a proper NUnit test would
// mean restructuring those files into their own assembly first. That's a
// reasonable thing to do later, but is more invasive than this check
// warrants right now. This runs with zero project reconfiguration: attach to
// any GameObject and press Play, or right-click the component and use the
// context menu to run it without entering Play mode.
public class KabschSelfTest : MonoBehaviour
{
    [ContextMenu("Run Kabsch Self-Test")]
    public void RunTests()
    {
        bool allPassed = true;
        allPassed &= TestRecoversKnownTransform();
        allPassed &= TestRejectsReflection();
        allPassed &= TestMinimumPoints();

        if (allPassed)
            Debug.Log("[KabschSelfTest] ALL TESTS PASSED");
        else
            Debug.LogError("[KabschSelfTest] ONE OR MORE TESTS FAILED -- see above");
    }

    private void Start()
    {
        RunTests();
    }

    // Applies a known rotation + translation to a synthetic point cloud and
    // checks Kabsch.Solve recovers it closely. Mirrors
    // test_kabsch_recovers_known_transform in test_icp_align.py.
    private bool TestRecoversKnownTransform()
    {
        var source = new List<Vector3>();
        var prng = new System.Random(42);
        for (int i = 0; i < 200; i++)
        {
            source.Add(new Vector3(
                (float)(prng.NextDouble() * 4 - 2),
                (float)(prng.NextDouble() * 4 - 2),
                (float)(prng.NextDouble() * 4 - 2)));
        }

        // known rotation: 10 degrees about Z, plus a translation -- small,
        // plausible values, same spirit as the Python test's [3,-5,10] degree case
        float angleRad = 10f * Mathf.Deg2Rad;
        Matrix4x4 trueRotation = Matrix4x4.identity;
        trueRotation[0, 0] = Mathf.Cos(angleRad); trueRotation[0, 1] = -Mathf.Sin(angleRad);
        trueRotation[1, 0] = Mathf.Sin(angleRad); trueRotation[1, 1] = Mathf.Cos(angleRad);
        Vector3 trueTranslation = new Vector3(0.5f, -0.2f, 0.1f);

        var target = new List<Vector3>(source.Count);
        foreach (var p in source)
            target.Add(trueRotation.MultiplyVector(p) + trueTranslation);

        var (estRotation, estTranslation) = Kabsch.Solve(source, target);

        float maxRotError = 0f;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                maxRotError = Mathf.Max(maxRotError, Mathf.Abs(estRotation[r, c] - trueRotation[r, c]));
        float translationError = Vector3.Distance(estTranslation, trueTranslation);

        bool pass = maxRotError < 1e-4f && translationError < 1e-4f;
        Debug.Log($"[KabschSelfTest] RecoversKnownTransform: maxRotError={maxRotError:E2}, "
                  + $"translationError={translationError:E2} -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }

    // A mirrored point cloud is a reflection, not a rotation -- Kabsch must
    // still return a proper rotation (determinant +1, not -1). Mirrors
    // test_kabsch_rejects_reflection in test_icp_align.py.
    private bool TestRejectsReflection()
    {
        var source = new List<Vector3>();
        var prng = new System.Random(7);
        for (int i = 0; i < 100; i++)
        {
            source.Add(new Vector3(
                (float)(prng.NextDouble() * 4 - 2),
                (float)(prng.NextDouble() * 4 - 2),
                (float)(prng.NextDouble() * 4 - 2)));
        }

        var mirrored = new List<Vector3>(source.Count);
        foreach (var p in source) mirrored.Add(new Vector3(-p.x, p.y, p.z));

        var (estRotation, _) = Kabsch.Solve(source, mirrored);

        float det = estRotation[0, 0] * (estRotation[1, 1] * estRotation[2, 2] - estRotation[1, 2] * estRotation[2, 1])
                  - estRotation[0, 1] * (estRotation[1, 0] * estRotation[2, 2] - estRotation[1, 2] * estRotation[2, 0])
                  + estRotation[0, 2] * (estRotation[1, 0] * estRotation[2, 1] - estRotation[1, 1] * estRotation[2, 0]);

        bool pass = det > 0f && Mathf.Abs(det - 1f) < 1e-4f;
        Debug.Log($"[KabschSelfTest] RejectsReflection: determinant={det:F6} -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }

    // Exact minimum case: 3 points, a clean 90 degree Z rotation, no
    // translation. Mirrors test_kabsch_minimum_points in test_icp_align.py.
    private bool TestMinimumPoints()
    {
        var source = new List<Vector3>
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(-1, -1, 0),
        };

        Matrix4x4 trueRotation = Matrix4x4.identity;
        trueRotation[0, 0] = 0; trueRotation[0, 1] = -1;
        trueRotation[1, 0] = 1; trueRotation[1, 1] = 0;

        var target = new List<Vector3>();
        foreach (var p in source) target.Add(trueRotation.MultiplyVector(p));

        var (estRotation, _) = Kabsch.Solve(source, target);

        float maxErr = 0f;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                maxErr = Mathf.Max(maxErr, Mathf.Abs(estRotation[r, c] - trueRotation[r, c]));

        bool pass = maxErr < 1e-5f;
        Debug.Log($"[KabschSelfTest] MinimumPoints: maxError={maxErr:E2} -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }
}
