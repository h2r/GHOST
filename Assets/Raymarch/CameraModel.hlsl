#ifndef RAYMARCH_CAMERA_MODEL_INCLUDED
#define RAYMARCH_CAMERA_MODEL_INCLUDED

// GPU mirror of DepthCameraModel.cs. KEEP THE TWO IN LOCKSTEP.
//
// Conventions:
//   pixel u = column (right+), v = row (down+); (cx, cy, fx, fy) in pixels.
//   camera space: +X right, +Y down, +Z forward (into the scene); depth = Z_cam (metres).
//   cameraToWorld maps camera space -> Unity world space.
//   intrinsics packed as (cx, cy, fx, fy).
//
// Extrinsics are rigid, so mul(matrix, float4(p, 1)).xyz is the affine transform
// (equivalent to Matrix4x4.MultiplyPoint3x4 on the C# side).

struct CameraModel
{
    float4   intrinsics;    // (cx, cy, fx, fy)
    float2   resolution;    // (width, height)
    float4x4 cameraToWorld;
    float4x4 worldToCamera;
};

// Pixel (uv) + metric depth -> camera-space point (+Z forward).
float3 CameraModel_UnprojectToCamera(CameraModel c, float2 uv, float depth)
{
    float x = (uv.x - c.intrinsics.x) * depth / c.intrinsics.z;
    float y = (uv.y - c.intrinsics.y) * depth / c.intrinsics.w;
    return float3(x, y, depth);
}

// Pixel (uv) + metric depth -> world-space point.
float3 CameraModel_UnprojectToWorld(CameraModel c, float2 uv, float depth)
{
    return mul(c.cameraToWorld, float4(CameraModel_UnprojectToCamera(c, uv, depth), 1.0)).xyz;
}

// Camera-space point -> (u, v, depth). Returned .z is Z_cam (<= 0 means behind the camera).
// Inverse of CameraModel_UnprojectToCamera.
float3 CameraModel_ProjectFromCamera(CameraModel c, float3 cam)
{
    float u = c.intrinsics.z * cam.x / cam.z + c.intrinsics.x;
    float v = c.intrinsics.w * cam.y / cam.z + c.intrinsics.y;
    return float3(u, v, cam.z);
}

// World-space point -> (u, v, depth). Inverse of CameraModel_UnprojectToWorld.
float3 CameraModel_ProjectFromWorld(CameraModel c, float3 world)
{
    float3 cam = mul(c.worldToCamera, float4(world, 1.0)).xyz;
    return CameraModel_ProjectFromCamera(c, cam);
}

bool CameraModel_InFront(float3 cam)
{
    return cam.z > 0.0;
}

bool CameraModel_InFrame(CameraModel c, float2 uv)
{
    return uv.x >= 0.0 && uv.x < c.resolution.x && uv.y >= 0.0 && uv.y < c.resolution.y;
}

#endif // RAYMARCH_CAMERA_MODEL_INCLUDED
