using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public static class Kabsch
{
    // Estimate the rigid transformation (rotation and translation) that
    // aligns sourcePoints to targetPoints. Points correspond by index.
    public static (Matrix4x4 rotation, Vector3 translation) Solve(
        IReadOnlyList<Vector3> sourcePoints, IReadOnlyList<Vector3> targetPoints)
    {
        int n = sourcePoints.Count;

        // compute centroids of both point sets
        Vector3 p = Vector3.zero, q = Vector3.zero;
        for (int i = 0; i < n; i++) { p += sourcePoints[i]; q += targetPoints[i]; }
        p /= n;
        q /= n;

        // compute the covariance matrix: source_centered^T @ target_centered
        var covariance = Matrix<double>.Build.Dense(3, 3);
        for (int i = 0; i < n; i++)
        {
            Vector3 sc = sourcePoints[i] - p, tc = targetPoints[i] - q;
            double[] sv = { sc.x, sc.y, sc.z };
            double[] tv = { tc.x, tc.y, tc.z };
            for (int a = 0; a < 3; a++)
                for (int b = 0; b < 3; b++)
                    covariance[a, b] += sv[a] * tv[b];
        }

        var svd = covariance.Svd(true);
        Matrix<double> u = svd.U;
        Matrix<double> vT = svd.VT;

        // rotation_matrix
        Matrix<double> rotation = vT.Transpose() * u.Transpose();

        // ensure a proper rotation
        if (rotation.Determinant() < 0)
        {
            vT.SetRow(2, -vT.Row(2));
            rotation = vT.Transpose() * u.Transpose();
        }

        Matrix4x4 rotationMatrix = ToMatrix4x4(rotation);

        // translation_vector
        Vector3 translation = q - rotationMatrix.MultiplyVector(p);

        return (rotationMatrix, translation);
    }

    private static Matrix4x4 ToMatrix4x4(Matrix<double> m)
    {

        var result = Matrix4x4.identity;
        
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                result[r, c] = (float)m[r, c];

        return result;

    }
}
