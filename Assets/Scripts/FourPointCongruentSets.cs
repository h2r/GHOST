using System;
using System.Collections.Generic;
using UnityEngine;

// Ported from pyspotobserver/four_pcs.py. Finds an initial rigid alignment
// between two point clouds with no prior pose information, using
// 4-Points Congruent Sets. Meant to be run once, then handed to an ICP
// refinement step (not yet ported).
//
// REPRODUCIBILITY NOTE: this uses System.Random, which is a different RNG
// from both numpy's default_rng (PCG64, used by the Python version) and
// std::mt19937_64 (used by the C++ port in spotobserver-real). A "matching"
// seed value does NOT reproduce the same random draws across any of these --
// each is only reproducible within itself. Validate this port by checking
// that it converges to a similarly good answer on real data, not by
// expecting an exact number match to a specific Python or C++ run.
public static class FourPointCongruentSets
{
    // Check which points lie within `threshold` of a plane defined by
    // normal . x + offset = 0.
    public static bool PointNearPlane(Vector3 point, Vector3 normal, float offset, float threshold = 0.04f)
    {
        return Mathf.Abs(Vector3.Dot(point, normal) + offset) < threshold;
    }

    public struct PlaneFit
    {
        public bool Valid;
        public Vector3 Normal;
        public float Offset;
        public bool[] InlierMask;
    }

    // Fit the single largest planar surface in a point cloud using RANSAC.
    public static PlaneFit FitDominantPlane(IReadOnlyList<Vector3> points, int iterations = 300,
                                             float threshold = 0.04f, int seed = 0)
    {
        var result = new PlaneFit { Valid = false };
        int n = points.Count;
        if (n < 3) return result;

        var rng = new System.Random(seed);
        long bestCount = -1;

        for (int it = 0; it < iterations; it++)
        {
            int i0 = rng.Next(n), i1 = rng.Next(n), i2 = rng.Next(n);
            if (i0 == i1 || i1 == i2 || i0 == i2) continue;

            Vector3 p0 = points[i0], p1 = points[i1], p2 = points[i2];
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            float normLen = normal.magnitude;
            if (normLen < 1e-8f) continue;

            normal /= normLen;
            float offset = -Vector3.Dot(normal, p0);

            bool[] inliers = new bool[n];
            long count = 0;
            for (int i = 0; i < n; i++)
            {
                inliers[i] = PointNearPlane(points[i], normal, offset, threshold);
                if (inliers[i]) count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                result.Valid = true;
                result.Normal = normal;
                result.Offset = offset;
                result.InlierMask = inliers;
            }
        }
        return result;
    }

    // Select a coplanar base of 4 points from a point cloud.
    private struct CoplanarBase
    {
        public bool Found;
        public int[] Indices;
        public Vector3[] Points;
    }

    private static CoplanarBase SelectCoplanarBase(IReadOnlyList<Vector3> cloud, System.Random rng,
                                                     float minSpread, float maxSpread,
                                                     float coplanarTol, int iterations)
    {
        int n = cloud.Count;

        for (int it = 0; it < iterations; it++)
        {
            int[] idx = new int[4];
            bool duplicate = false;

            for (int k = 0; k < 4; k++)
            {
                idx[k] = rng.Next(n);
                for (int j = 0; j < k; j++) if (idx[j] == idx[k]) duplicate = true;
            }

            if (duplicate) continue;

            Vector3[] pts = new Vector3[4];
            for (int k = 0; k < 4; k++) pts[k] = cloud[idx[k]];

            float minDist = float.PositiveInfinity, maxDist = 0f;
            for (int a = 0; a < 4; a++)
                for (int b = a + 1; b < 4; b++)
                {
                    float d = Vector3.Distance(pts[a], pts[b]);
                    minDist = Mathf.Min(minDist, d);
                    maxDist = Mathf.Max(maxDist, d);
                }

            if (minDist < minSpread || maxDist > maxSpread) continue;

            // compute the normal of the plane defined by the first three points
            Vector3 normal = Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]);
            float normalLen = normal.magnitude;
            if (normalLen < 1e-6f) continue;
            normal /= normalLen;

            // compute the residual for the fourth point to check coplanarity
            float residual = Mathf.Abs(Vector3.Dot(pts[3] - pts[0], normal));
            if (residual > coplanarTol) continue;

            return new CoplanarBase { Found = true, Indices = idx, Points = pts };
        }
        return new CoplanarBase { Found = false };
    }

    // Find the pairing of 4 coplanar points that makes them behave like the
    // two diagonals of a quadrilateral, and compute the affine-invariant
    // ratios where those diagonals cross. See diagonal_pairing_and_ratios()
    // in four_pcs.py.
    private struct DiagonalPairing
    {
        public bool Found;
        public int[] Order; // (a, b, c, d): segment ab and segment cd cross
        public float RatioA, RatioB, DiagA, DiagB;
    }

    // least-squares solve of the 3-equation, 2-unknown system via normal
    private static bool Solve3x2LeastSquares(Vector3 col0, Vector3 col1, Vector3 rhs, out float x0, out float x1)
    {
        float a = Vector3.Dot(col0, col0), b = Vector3.Dot(col0, col1);
        float c = Vector3.Dot(col1, col0), d = Vector3.Dot(col1, col1);
        float e = Vector3.Dot(col0, rhs), g = Vector3.Dot(col1, rhs);
        float det = a * d - b * c;
        if (Mathf.Abs(det) < 1e-10f) { x0 = 0; x1 = 0; return false; }
        x0 = (e * d - b * g) / det;
        x1 = (a * g - e * c) / det;
        return true;
    }

    private static DiagonalPairing DiagonalPairingAndRatios(Vector3[] basePoints)
    {
        // the 3 ways to split 4 points into two pairs -- only one will actually cross
        int[][] orderings = { new[] { 0, 1, 2, 3 }, new[] { 0, 2, 1, 3 }, new[] { 0, 3, 1, 2 } };

        foreach (var ord in orderings)
        {
            int a = ord[0], b = ord[1], c = ord[2], d = ord[3];
            Vector3 pointA = basePoints[a], pointB = basePoints[b], pointC = basePoints[c], pointD = basePoints[d];

            // solve for where segment ab and segment cd cross:
            // point_a + ratio_a*(point_b - point_a) = point_c + ratio_b*(point_d - point_c)
            Vector3 col0 = pointB - pointA;
            Vector3 col1 = -(pointD - pointC);
            Vector3 rhs = pointC - pointA;
            if (!Solve3x2LeastSquares(col0, col1, rhs, out float ratioA, out float ratioB)) continue;

            // the crossing point must actually lie between the two endpoints on each segment
            if (ratioA >= -0.05f && ratioA <= 1.05f && ratioB >= -0.05f && ratioB <= 1.05f)
            {
                Vector3 crossingPoint = pointA + ratioA * (pointB - pointA);
                Vector3 crossingPointCheck = pointC + ratioB * (pointD - pointC);

                if (Vector3.Distance(crossingPoint, crossingPointCheck) < 0.02f)
                {
                    return new DiagonalPairing
                    {
                        Found = true,
                        Order = new[] { a, b, c, d },
                        RatioA = ratioA,
                        RatioB = ratioB,
                        DiagA = Vector3.Distance(pointB, pointA),
                        DiagB = Vector3.Distance(pointD, pointC)
                    };
                }
            }
        }
        return new DiagonalPairing { Found = false };
    }

    // Minimal spatial hash grid over 3D points, standing in for scipy's
    // cKDTree (query_pairs / query_ball_point / query w/ distance_upper_bound)
    // -- there's no KD-tree library in this project. Correct at any query
    // radius relative to cellSize (searches enough neighboring cells to
    // cover it), though most efficient when radius is close to cellSize.
    private class SpatialHashGrid
    {
        private readonly IReadOnlyList<Vector3> _points;
        private readonly float _cellSize;
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

        public SpatialHashGrid(IReadOnlyList<Vector3> points, float cellSize)
        {
            _points = points;
            _cellSize = Mathf.Max(cellSize, 1e-6f);
            for (int i = 0; i < points.Count; i++)
            {
                long key = CellKey(points[i]);
                if (!_cells.TryGetValue(key, out var list)) { list = new List<int>(); _cells[key] = list; }
                list.Add(i);
            }
        }

        // all index pairs (i < j) whose distance falls within
        // [targetDistance - tolerance, targetDistance + tolerance].
        // Mirrors distance_pairs().
        public List<(int, int)> PairsInRange(float targetDistance, float tolerance)
        {
            var result = new List<(int, int)>();
            float maxD = targetDistance + tolerance;
            float minD = Mathf.Max(0f, targetDistance - tolerance);
            int reach = Mathf.CeilToInt(maxD / _cellSize) + 1;

            for (int i = 0; i < _points.Count; i++)
            {
                (int cx, int cy, int cz) = CellCoord(_points[i]);
                for (int dx = -reach; dx <= reach; dx++)
                    for (int dy = -reach; dy <= reach; dy++)
                        for (int dz = -reach; dz <= reach; dz++)
                        {
                            if (!_cells.TryGetValue(EncodeKey(cx + dx, cy + dy, cz + dz), out var list)) continue;
                            foreach (int j in list)
                            {
                                if (j <= i) continue; // undirected, avoid duplicates/self-pairs
                                float d = Vector3.Distance(_points[i], _points[j]);
                                if (d >= minD && d <= maxD) result.Add((i, j));
                            }
                        }
            }
            return result;
        }

        // for each point in `query`, indices into this grid's points within `radius`.
        // Mirrors cKDTree.query_ball_point().
        public List<List<int>> RadiusQuery(IReadOnlyList<Vector3> query, float radius)
        {
            var result = new List<List<int>>(query.Count);
            int reach = Mathf.CeilToInt(radius / _cellSize) + 1;

            for (int q = 0; q < query.Count; q++)
            {
                var matches = new List<int>();
                (int cx, int cy, int cz) = CellCoord(query[q]);
                for (int dx = -reach; dx <= reach; dx++)
                    for (int dy = -reach; dy <= reach; dy++)
                        for (int dz = -reach; dz <= reach; dz++)
                        {
                            if (!_cells.TryGetValue(EncodeKey(cx + dx, cy + dy, cz + dz), out var list)) continue;
                            foreach (int idx in list)
                                if (Vector3.Distance(_points[idx], query[q]) <= radius) matches.Add(idx);
                        }
                result.Add(matches);
            }
            return result;
        }

        // nearest point index within maxDistance, or -1 if none found.
        // Mirrors cKDTree.query(..., distance_upper_bound=max_distance).
        public int NearestWithin(Vector3 query, float maxDistance)
        {
            int reach = Mathf.CeilToInt(maxDistance / _cellSize) + 1;
            (int cx, int cy, int cz) = CellCoord(query);
            int best = -1;
            float bestD = maxDistance;

            for (int dx = -reach; dx <= reach; dx++)
                for (int dy = -reach; dy <= reach; dy++)
                    for (int dz = -reach; dz <= reach; dz++)
                    {
                        if (!_cells.TryGetValue(EncodeKey(cx + dx, cy + dy, cz + dz), out var list)) continue;
                        foreach (int idx in list)
                        {
                            float d = Vector3.Distance(_points[idx], query);
                            if (d < bestD) { bestD = d; best = idx; }
                        }
                    }
            return best;
        }

        private (int, int, int) CellCoord(Vector3 p) =>
            (Mathf.FloorToInt(p.x / _cellSize), Mathf.FloorToInt(p.y / _cellSize), Mathf.FloorToInt(p.z / _cellSize));

        private static long EncodeKey(int x, int y, int z)
        {
            long Enc(int v) => (long)v + (1 << 19);
            return (Enc(x) << 42) | (Enc(y) << 21) | Enc(z);
        }

        private long CellKey(Vector3 p)
        {
            (int x, int y, int z) = CellCoord(p);
            return EncodeKey(x, y, z);
        }
    }

    // Find all point pairs in a cloud whose distance falls within a
    // tolerance band around a target distance, capped at maxPairs. Mirrors
    // distance_pairs().
    //
    // Builds its own grid sized to THIS query's radius rather than reusing a
    // fixed-cell grid tuned for a different (often much smaller) radius --
    // that mismatch is a real perf trap: a small-celled grid queried at a
    // large radius (diagonals here can be several meters) means searching
    // tens of thousands of neighboring cells per point. Rebuilding is cheap
    // since these are the coarse, downsampled clouds (thousands of points).
    private static List<(int, int)> DistancePairs(IReadOnlyList<Vector3> points, float targetDistance,
                                                    float tolerance, System.Random rng, int maxPairs = 2000)
    {
        var grid = new SpatialHashGrid(points, Mathf.Max(targetDistance + tolerance, 1e-3f));
        var pairs = grid.PairsInRange(targetDistance, tolerance);

        if (pairs.Count > maxPairs)
        {
            // partial Fisher-Yates: shuffle just enough to pick maxPairs without replacement
            for (int i = 0; i < maxPairs; i++)
            {
                int j = i + rng.Next(pairs.Count - i);
                (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
            }
            pairs = pairs.GetRange(0, maxPairs);
        }
        return pairs;
    }

    // Search the target cloud for all 4-point subsets that are approximately
    // congruent to a base -- share the same two diagonal lengths AND the
    // same two crossing ratios, within tolerance. Mirrors find_congruent().
    private static List<(int, int, int, int)> FindCongruent(
        Vector3[] orderedBasePoints, float ratioA, float ratioB, float diagA, float diagB,
        IReadOnlyList<Vector3> target, float distanceTolerance, float eTolerance, System.Random rng,
        int maxPairsPerDistance = 2000, int maxCandidates = 250)
    {
        var pairsA = DistancePairs(target, diagA, distanceTolerance, rng, maxPairsPerDistance);
        var pairsB = DistancePairs(target, diagB, distanceTolerance, rng, maxPairsPerDistance);
        if (pairsA.Count == 0 || pairsB.Count == 0) return new List<(int, int, int, int)>();

        // candidate crossing points implied by each pairsA pair + ratioA.
        // try both point orderings per pair, since we don't know which end plays "a" vs "b"
        var e1All = new List<Vector3>();
        var pairsADirections = new List<(int, int)>();
        foreach (var (pi, qi) in pairsA)
        {
            Vector3 P = target[pi], Q = target[qi];
            e1All.Add(P + ratioA * (Q - P));
            pairsADirections.Add((pi, qi));
            e1All.Add(Q + ratioA * (P - Q));
            pairsADirections.Add((qi, pi));
        }

        // candidate crossing points implied by each pairsB pair + ratioB
        var e2All = new List<Vector3>();
        var pairsBDirections = new List<(int, int)>();
        foreach (var (pi, qi) in pairsB)
        {
            Vector3 P = target[pi], Q = target[qi];
            e2All.Add(P + ratioB * (Q - P));
            pairsBDirections.Add((pi, qi));
            e2All.Add(Q + ratioB * (P - Q));
            pairsBDirections.Add((qi, pi));
        }

        // build a grid over e1All and query for nearby points in e2All
        var e1Grid = new SpatialHashGrid(e1All, Mathf.Max(eTolerance, 1e-6f));
        var e2Matches = e1Grid.RadiusQuery(e2All, eTolerance);

        var candidates = new List<(int, int, int, int)>();
        int nChecked = 0;

        for (int e2Index = 0; e2Index < e2Matches.Count; e2Index++)
        {
            foreach (int e1Index in e2Matches[e2Index])
            {
                nChecked++;
                if (nChecked > maxCandidates) return candidates;

                var (P, Q) = pairsADirections[e1Index];
                var (R, S) = pairsBDirections[e2Index];
                candidates.Add((P, Q, R, S));
            }
        }
        return candidates;
    }

    public struct RegistrationResult
    {
        public Matrix4x4 Rotation;
        public Vector3 Translation;
        public long Score;
    }

    // Find an initial rigid alignment between two point clouds with no prior
    // pose information, using 4-Points Congruent Sets. Meant to be run once,
    // then handed to an ICP refinement step for polishing.
    public static RegistrationResult Solve(
        IReadOnlyList<Vector3> source, IReadOnlyList<Vector3> target,
        int iterations = 200, float maxDistance = 0.1f,
        float minSpread = 0.3f, float maxSpread = 1.2f, float coplanarTol = 0.05f,
        float distanceTol = 0.03f, float eTol = 0.05f, int seed = 0,
        Vector3? dominantPlaneNormal = null, float dominantPlaneOffset = 0f,
        float planeRejectThresh = 0.04f, float planeRejectAngleCos = 0.94f)
    {
        var rng = new System.Random(seed);
        var targetGrid = new SpatialHashGrid(target, Mathf.Max(maxDistance, 0.05f));

        bool planeActive = dominantPlaneNormal.HasValue;
        bool[] sourceOffPlane = null;
        if (planeActive)
        {
            sourceOffPlane = new bool[source.Count];
            for (int i = 0; i < source.Count; i++)
                sourceOffPlane[i] = !PointNearPlane(source[i], dominantPlaneNormal.Value, dominantPlaneOffset, planeRejectThresh);
        }

        var best = new RegistrationResult { Rotation = Matrix4x4.identity, Translation = Vector3.zero, Score = -1 };

        for (int it = 0; it < iterations; it++)
        {
            CoplanarBase basePts = SelectCoplanarBase(source, rng, minSpread, maxSpread, coplanarTol, 200);
            if (!basePts.Found) continue;

            if (planeActive)
            {
                Vector3 baseNormal = Vector3.Cross(basePts.Points[1] - basePts.Points[0], basePts.Points[2] - basePts.Points[0]);
                float baseNormalLen = baseNormal.magnitude;

                if (baseNormalLen > 1e-8f)
                {
                    baseNormal /= baseNormalLen;
                    Vector3 baseCentroid = (basePts.Points[0] + basePts.Points[1] + basePts.Points[2] + basePts.Points[3]) * 0.25f;
                    float cosAngle = Mathf.Abs(Vector3.Dot(baseNormal, dominantPlaneNormal.Value));
                    float centroidDist = Mathf.Abs(Vector3.Dot(baseCentroid, dominantPlaneNormal.Value) + dominantPlaneOffset);

                    // this base is itself just a patch of the dominant plane -- skip it,
                    // since any match found from it is exactly the degenerate case above
                    if (cosAngle > planeRejectAngleCos && centroidDist < planeRejectThresh * 3) continue;
                }
            }

            DiagonalPairing pairing = DiagonalPairingAndRatios(basePts.Points);
            if (!pairing.Found) continue;

            Vector3[] orderedBasePoints =
            {
                basePts.Points[pairing.Order[0]], basePts.Points[pairing.Order[1]],
                basePts.Points[pairing.Order[2]], basePts.Points[pairing.Order[3]]
            };

            var candidates = FindCongruent(orderedBasePoints, pairing.RatioA, pairing.RatioB,
                                            pairing.DiagA, pairing.DiagB, target, distanceTol, eTol, rng);

            // verify each candidate with a real rigid fit, scored against the whole cloud --
            // find_congruent() only guarantees affine invariance, not a genuine rigid match
            foreach (var (P, Q, R, S) in candidates)
            {
                var matchedPoints = new[] { target[P], target[Q], target[R], target[S] };
                var (rotationMatrix, translationVector) = Kabsch.Solve(orderedBasePoints, matchedPoints);

                long score = 0;
                for (int i = 0; i < source.Count; i++)
                {
                    Vector3 transformed = rotationMatrix.MultiplyVector(source[i]) + translationVector;
                    bool matched = targetGrid.NearestWithin(transformed, maxDistance) >= 0;
                    // points on the dominant plane don't count toward the score -- otherwise a
                    // transform that just slides the plane onto itself wins by default
                    if (matched && (!planeActive || sourceOffPlane[i])) score++;
                }

                if (score > best.Score)
                {
                    best.Score = score;
                    best.Rotation = rotationMatrix;
                    best.Translation = translationVector;
                }
            }
        }

        return best;
    }
}
