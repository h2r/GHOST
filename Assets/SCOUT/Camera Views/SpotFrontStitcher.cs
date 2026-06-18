using UnityEngine;

public class SpotFrontStitcher : MonoBehaviour
{
    private const string StitchShaderName = "SCOUT/Spot Front Stitch";

    public enum UvRotation
    {
        None,
        Left90,
        Right90,
        UpsideDown180
    }

    [System.Serializable]
    public class CameraProjection
    {
        [Tooltip("Source image resolution in pixels.")]
        public Vector2 resolution = new Vector2(640f, 480f);
        [Tooltip("Camera focal length in pixels: (fx, fy).")]
        public Vector2 focalLength = new Vector2(256f, 256f);
        [Tooltip("Camera principal point in pixels: (cx, cy).")]
        public Vector2 principalPoint = new Vector2(320f, 240f);

        [Tooltip("Physical camera position in the virtual stitch-camera frame, metres. +X right, +Y down, +Z forward.")]
        public Vector3 positionMeters;
        [Tooltip("Physical camera rotation in the virtual stitch-camera frame, degrees.")]
        public Vector3 rotationEulerDegrees;

        [Header("Texture Orientation")]
        public UvRotation uvRotation = UvRotation.None;
        public bool flipHorizontal = false;
        public bool flipVertical = false;

        public Matrix4x4 BuildMvp()
        {
            float width = Mathf.Max(1f, resolution.x);
            float height = Mathf.Max(1f, resolution.y);

            Matrix4x4 cameraProjection = Matrix4x4.identity;
            cameraProjection.m00 = focalLength.x / width;
            cameraProjection.m02 = principalPoint.x / width;
            cameraProjection.m11 = focalLength.y / height;
            cameraProjection.m12 = principalPoint.y / height;

            Matrix4x4 virtualFromSensor = BuildTrsVirtualFromSensorMatrix();

            return cameraProjection * virtualFromSensor.inverse;
        }

        public Matrix4x4 BuildTrsVirtualFromSensorMatrix()
        {
            return Matrix4x4.TRS(
                positionMeters,
                Quaternion.Euler(rotationEulerDegrees),
                Vector3.one);
        }

        public Vector4 BuildUvTransform()
        {
            float scaleX = flipHorizontal ? -1f : 1f;
            float scaleY = flipVertical ? -1f : 1f;
            return new Vector4(scaleX, scaleY, flipHorizontal ? 1f : 0f, flipVertical ? 1f : 0f);
        }
    }

    [Header("Source")]
    public SpotObserverClient spotObserverClient;
    public int streamIndex = 0;
    public SpotObserverClient.SpotCamera frontRightCamera = SpotObserverClient.SpotCamera.FRONTRIGHT;
    public SpotObserverClient.SpotCamera frontLeftCamera = SpotObserverClient.SpotCamera.FRONTLEFT;

    [Header("Output")]
    public MeshRenderer targetRenderer;
    public Shader stitchShader;
    public Color backgroundColor = Color.black;

    [Tooltip("Distance from the virtual stitch camera to the projection plane, metres.")]
    public float planeDistanceMeters = 2f;
    [Tooltip("Virtual projection-plane size covered by this output quad, metres.")]
    public Vector2 planeSizeMeters = new Vector2(7.6f, 5.7f);

    [Header("Blend")]
    [Min(0.0001f)]
    public float blendingPower = 10f;
    [Range(0f, 1f)]
    public float blendingMinimum = 0.001f;

    [Header("Manual Calibration")]
    public CameraProjection frontRight = new CameraProjection();
    public CameraProjection frontLeft = new CameraProjection();

    private Material runtimeMaterial;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int FrontRightTexId = Shader.PropertyToID("_FrontRightTex");
    private static readonly int FrontLeftTexId = Shader.PropertyToID("_FrontLeftTex");
    private static readonly int FrontRightMvpId = Shader.PropertyToID("_FrontRightMVP");
    private static readonly int FrontLeftMvpId = Shader.PropertyToID("_FrontLeftMVP");
    private static readonly int FrontRightUvTransformId = Shader.PropertyToID("_FrontRightUvTransform");
    private static readonly int FrontLeftUvTransformId = Shader.PropertyToID("_FrontLeftUvTransform");
    private static readonly int FrontRightUvRotationId = Shader.PropertyToID("_FrontRightUvRotation");
    private static readonly int FrontLeftUvRotationId = Shader.PropertyToID("_FrontLeftUvRotation");
    private static readonly int PlaneDistanceId = Shader.PropertyToID("_PlaneDistance");
    private static readonly int PlaneSizeId = Shader.PropertyToID("_PlaneSize");
    private static readonly int BackColorId = Shader.PropertyToID("_BackColor");
    private static readonly int BlendingPowerId = Shader.PropertyToID("_BlendingPower");
    private static readonly int BlendingMinimumId = Shader.PropertyToID("_BlendingMinimum");

    private void Reset()
    {
        targetRenderer = GetComponent<MeshRenderer>();
        stitchShader = Shader.Find(StitchShaderName);
        ApplyApproximateSpotFrontDefaults();
    }

    private void Awake()
    {
        EnsureRendererAndMaterial();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    private void Update()
    {
        if (!EnsureRendererAndMaterial() || spotObserverClient == null)
            return;

        if (!spotObserverClient.TryGetCameraFrame(streamIndex, frontRightCamera, out SpotObserverClient.CameraDepthFrame rightFrame) ||
            !spotObserverClient.TryGetCameraFrame(streamIndex, frontLeftCamera, out SpotObserverClient.CameraDepthFrame leftFrame))
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetTexture(FrontRightTexId, rightFrame.ColorTexture);
        propertyBlock.SetTexture(FrontLeftTexId, leftFrame.ColorTexture);
        propertyBlock.SetMatrix(FrontRightMvpId, frontRight.BuildMvp());
        propertyBlock.SetMatrix(FrontLeftMvpId, frontLeft.BuildMvp());
        propertyBlock.SetVector(FrontRightUvTransformId, frontRight.BuildUvTransform());
        propertyBlock.SetVector(FrontLeftUvTransformId, frontLeft.BuildUvTransform());
        propertyBlock.SetFloat(FrontRightUvRotationId, (float)frontRight.uvRotation);
        propertyBlock.SetFloat(FrontLeftUvRotationId, (float)frontLeft.uvRotation);
        propertyBlock.SetFloat(PlaneDistanceId, Mathf.Max(0.01f, planeDistanceMeters));
        propertyBlock.SetVector(PlaneSizeId, planeSizeMeters);
        propertyBlock.SetColor(BackColorId, backgroundColor);
        propertyBlock.SetFloat(BlendingPowerId, blendingPower);
        propertyBlock.SetFloat(BlendingMinimumId, blendingMinimum);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private bool EnsureRendererAndMaterial()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<MeshRenderer>();

        if (targetRenderer == null)
            return false;

        if (stitchShader == null)
            stitchShader = Shader.Find(StitchShaderName);

        if (stitchShader == null)
        {
            Debug.LogError("Could not find shader '" + StitchShaderName + "'.", this);
            return false;
        }

        if (runtimeMaterial == null || runtimeMaterial.shader != stitchShader)
        {
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);

            runtimeMaterial = new Material(stitchShader)
            {
                name = "Runtime Spot Front Stitch"
            };
            targetRenderer.sharedMaterial = runtimeMaterial;
        }

        return true;
    }

    [ContextMenu("Apply Approximate Spot Front Defaults")]
    private void ApplyApproximateSpotFrontDefaults()
    {
        frontRight.resolution = new Vector2(640f, 480f);
        frontRight.focalLength = new Vector2(255.8604f, 255.5598f);
        frontRight.principalPoint = new Vector2(311f, 246f);
        frontRight.positionMeters = new Vector3(0.035f, 0f, 0f);
        frontRight.rotationEulerDegrees = new Vector3(0f, -18f, 0f);
        frontRight.flipVertical = true;

        frontLeft.resolution = new Vector2(640f, 480f);
        frontLeft.focalLength = new Vector2(256.4678f, 256.1428f);
        frontLeft.principalPoint = new Vector2(306f, 235f);
        frontLeft.positionMeters = new Vector3(-0.035f, 0f, 0f);
        frontLeft.rotationEulerDegrees = new Vector3(0f, 18f, 0f);
        frontLeft.flipVertical = true;
    }
}
