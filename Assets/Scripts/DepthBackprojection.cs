using System.Collections.Generic;
using UnityEngine;

// Ported from backproject() in pyspotobserver (test_icp_align.py /
// export_four_pcs_test_data.py): turns a depth image into a set of
// camera-frame 3D points via the standard pinhole model. This is the piece
// that was missing before FourPointCongruentSets could run on anything but
// synthetic test clouds -- it's what actually gets real captured data into
// the shape Solve() expects.
//
// Points come out in CAMERA FRAME (x right, y down, z forward along the
// optical axis, same convention as the Python version), not Unity world
// space. No world/robot-pose transform is applied here -- that's a separate
// step if/when clouds from different camera poses need to be fused before
// registration.
public static class DepthBackprojection
{
    // Backproject a raw depth buffer (row-major, width*height floats, meters,
    // 0 = invalid/no return) into camera-frame points. Mirrors backproject()
    // in pyspotobserver exactly: x = (u - cx) * z / fx, y = (v - cy) * z / fy.
    public static List<Vector3> Backproject(IReadOnlyList<float> depth, int width, int height,
                                             float cx, float cy, float fx, float fy)
    {
        var points = new List<Vector3>(width * height);

        for (int v = 0; v < height; v++)
        {
            for (int u = 0; u < width; u++)
            {
                float z = depth[v * width + u];
                if (z <= 0f) continue; // invalid/no-return pixel, same as Python's `depth > 0` mask

                float x = (u - cx) * z / fx;
                float y = (v - cy) * z / fy;
                points.Add(new Vector3(x, y, z));
            }
        }

        return points;
    }

    // Same, but pulls the depth values straight off a live SpotObserverClient
    // frame's GPU buffer first. ComputeBuffer.GetData() is a synchronous
    // readback (stalls until the GPU data is available) -- acceptable for the
    // "run once after movement stops" cadence this is meant for, not for a
    // per-frame render path like DrawMeshInstanced's compute shader.
    public static List<Vector3> Backproject(SpotObserverClient.CameraDepthFrame frame,
                                             float cx, float cy, float fx, float fy)
    {
        if (!frame.IsValid)
        {
            Debug.LogError("DepthBackprojection: camera frame is not valid.");
            return new List<Vector3>();
        }

        float[] depth = new float[frame.Width * frame.Height];
        frame.DepthBuffer.GetData(depth);
        return Backproject(depth, frame.Width, frame.Height, cx, cy, fx, fy);
    }
}
