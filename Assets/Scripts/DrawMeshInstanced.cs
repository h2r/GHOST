using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using static SpotObserverClient;
using Debug = UnityEngine.Debug;


public class DrawMeshInstanced : MonoBehaviour
{
    public DepthManager depthManager;
    public SpotCamera SpotObserverCameraIndex;
    public int SpotObserverStreamIdx;

    public Transform mainCameraRot;

    public float range;

    public Texture2D color_image;

    public int imageScriptIndex;

    public Material material;

    public SpotObserverClient spotObserverClient;
    public PoseConsistentVideoDepth CVD;

    public ComputeShader compute;

    // Renderer-owned float3 point/depth output consumed by the point cloud compute shader.
    private ComputeBuffer depth_ar_buffer;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer depthBuffer;
    private ComputeBuffer sparseBuffer;
    private ComputeBuffer edge_buffer;

    private ComputeBuffer optical_flow_buffer;

    public Transform target;
    public Transform auxTarget; // In case someone changes the offset rotation

    private Mesh mesh;
    private Bounds bounds;

    private uint population;
    public uint downsample;
    public uint height;
    public uint width;
    public float t;
    public float size_scale;

    public bool use_saved_meshes = false;
    private float[] depth_ar_saved;

    private bool freezeCloud = false; // boolean that freezes this point cloud
    private float[] depth_ar;

    // Arm motion temporarily pauses completion so old frames are not fused into a moving cloud.
    private bool start_completion = true;
    private bool ready_to_freeze = true;
    private bool freeze_lock = false;

    private int kernel;
    private int edge_kernel;
    private int frameWidth;
    private int frameHeight;
    private int frameSize;
    private ulong lastFrameSequence;
    private bool hasFrameData;
    private bool savedMeshUploaded;
    private bool ownsColorImage;
    private Texture2D frozenColorImage;

    public bool enableDebugLogging = false;

    // Keep this layout in sync with MeshProperties in PointcloudComputeShader.compute.
    private struct MeshProperties
    {
        public Vector4 pos;

        public static int Size()
        {
            return
                sizeof(float) * 4; // position;
        }
    }

    private Vector4 GetIntrinsicsVector()
    {
        if (CVD != null)
            return new Vector4(CVD.CX, CVD.CY, CVD.FX, CVD.FY);

        float cx = frameWidth * 0.5f;
        float cy = frameHeight * 0.5f;
        float fallbackFocalLength = Mathf.Max(frameWidth, frameHeight);
        return new Vector4(cx, cy, fallbackFocalLength, fallbackFocalLength);
    }

    private Vector4 GetScreenDataVector()
    {
        float focalY = CVD != null ? CVD.FY : Mathf.Max(frameWidth, frameHeight);
        return new Vector4((float)frameWidth, (float)frameHeight, 1.0f / frameWidth, focalY);
    }

    private void ApplyFrameShaderConstants()
    {
        // Keep C# dispatch sizes and shader-side indexing in one place.
        compute.SetInt("frameWidth", frameWidth);
        compute.SetInt("frameHeight", frameHeight);
        compute.SetInt("pixelCount", frameSize);
        compute.SetInt("pointCount", (int)population);
    }

    private void Setup()
    {
        kernel = compute.FindKernel("CSMain");
        edge_kernel = compute.FindKernel("EdgeDetector");

        if (width == 0)
            width = 640;
        if (height == 0)
            height = 480;
        if (downsample == 0)
        {
            Debug.LogWarning(name + " had downsample set to 0. Using 1 instead.");
            downsample = 1;
        }

        frameWidth = (int)width;
        frameHeight = (int)height;
        frameSize = frameWidth * frameHeight;
        uint totalPopulation = height * width;
        population = totalPopulation / downsample;
        if (population == 0)
            population = 1;
        lastFrameSequence = ulong.MaxValue;
        hasFrameData = false;
        savedMeshUploaded = false;

        if (CVD != null)
        {
            CVD.Width = frameWidth;
            CVD.Height = frameHeight;
        }

        mesh = CreateQuad(size_scale, size_scale);

        if (use_saved_meshes)
        {
            using (var stream = File.Open("Assets/PointClouds/mesh_array_" + imageScriptIndex, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    int length = reader.ReadInt32();
                    depth_ar_saved = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        depth_ar_saved[i] = reader.ReadSingle();
                    }
                }
            }

            byte[] bytes;
            using (var stream = File.Open("Assets/PointClouds/Color_" + imageScriptIndex + ".png", FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    bytes = reader.ReadBytes(int.MaxValue);
                }
            }
            color_image = new Texture2D(1, 1);
            color_image.LoadImage(bytes);
            ownsColorImage = true;
        }

        depth_ar = new float[frameSize];
        ownsColorImage = use_saved_meshes;

        // Bounds only need to cover the cloud; the indirect draw is culled against this box.
        bounds = new Bounds(transform.position, Vector3.one * (range + 1));

        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetFloat("a", GetTargetRotationScalar());

        Vector4 intr = GetIntrinsicsVector();
        compute.SetVector("intrinsics", intr);

        Vector4 screenData = GetScreenDataVector();
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);
        ApplyFrameShaderConstants();

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);

        InitializeBuffers();

        // Sparse fallback is currently a zero-depth mask for rejected pixels.
        Array.Clear(depth_ar, 0, depth_ar.Length);
        sparseBuffer.SetData(depth_ar);
    }

    private bool ProcessDepth(ComputeBuffer in_depth)
    {
        if (in_depth == null || CVD == null || depth_ar_buffer == null)
            return false;

        float edgethreshold = depthManager != null ? depthManager.edgeThreshold : 0.0f;
        Matrix4x4 tform = get_current_pose();
        bool activateCVD = depthManager != null && depthManager.activate_CVD && start_completion;
        bool activateDepthCompletion = depthManager != null && depthManager.activate_depth_estimation;
        float cvdWeight = depthManager != null ? depthManager.cvd_weight : 0.0f;

        // CVD writes into our owned output buffer; the input depth buffer remains borrowed.
        return CVD.WriteConsistentDepth(in_depth, depth_ar_buffer, tform, optical_flow_buffer, activateCVD, edgethreshold, activateDepthCompletion, cvdWeight);
    }

    private float GetTargetRotationScalar()
    {
        if (target == null)
        {
            return 0.0f;
        }
        if (auxTarget == null) { return ConvertAngle(target.eulerAngles.y) * 2; }
        else
        {
            return ConvertAngle(target.eulerAngles.y) + ConvertAngle(auxTarget.eulerAngles.y);
        }
    }

    private float ConvertAngle(float a)
    {
        return a * (float)0.00872;
    }

    private MeshProperties[] GetProperties()
    {
        MeshProperties[] properties = new MeshProperties[population];

        Vector4 initialValue = new Vector4( 0, 0, 0, 1 );

        for (uint pop_i = 0; pop_i < population; pop_i++)
        {
            properties[pop_i].pos = initialValue;
        }

        return properties;
    }

    private void InitializeBuffers()
    {
        depth_ar_buffer = new ComputeBuffer(frameSize, sizeof(float) * 3);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = mesh.GetIndexCount(0);
        args[1] = population;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        meshPropertiesBuffer = new ComputeBuffer((int)population, MeshProperties.Size());
        meshPropertiesBuffer.SetData(GetProperties());

        if (enableDebugLogging)
        {
            Debug.Log("mesh.getIndexCount(0): " + mesh.GetIndexCount(0));
            Debug.Log("population: " + population);
            Debug.Log("mesh.GetIndexStart(0): " + mesh.GetIndexStart(0));
            Debug.Log("mesh.GetBaseVertex(0): " + mesh.GetBaseVertex(0));
            Debug.Log("meshPropertiesBuffer.count: " + meshPropertiesBuffer.count);
        }

        depthBuffer = new ComputeBuffer(frameSize, sizeof(float));
        sparseBuffer = new ComputeBuffer(frameSize, sizeof(float));
        edge_buffer = new ComputeBuffer(frameSize, sizeof(float));
        depthBuffer.SetData(depth_ar);

        optical_flow_buffer = new ComputeBuffer(frameSize * 2, sizeof(float));

        SetProperties();
        SetGOPosition();
    }

    private void SetGOPosition()
    {
        Matrix4x4 pose = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        material.SetMatrix("_GOPose", pose);
        bounds.center = transform.position;
        SetBillboardBasis();
    }

    private void SetBillboardBasis()
    {
        // Quad orientation is applied in the material shader, so CPU mesh data stays static.
        Vector3 localNormal = transform.InverseTransformDirection(get_normal()).normalized;
        if (localNormal.sqrMagnitude < 1e-6f)
            localNormal = -Vector3.forward;

        Vector3 localRight = Vector3.Cross(localNormal, Vector3.up);
        if (localRight.sqrMagnitude < 1e-6f)
            localRight = Vector3.right;
        localRight.Normalize();

        Vector3 localUp = Vector3.Cross(localRight, localNormal);
        if (localUp.sqrMagnitude < 1e-6f)
            localUp = Vector3.up;
        localUp.Normalize();

        material.SetVector("_BillboardRight", localRight);
        material.SetVector("_BillboardUp", localUp);
    }

    private void SetProperties()
    {
        material.SetBuffer("_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);

        compute.SetBuffer(kernel, "_Depth", depth_ar_buffer);
        compute.SetBuffer(edge_kernel, "_Depth", depth_ar_buffer);
        compute.SetBuffer(edge_kernel, "_Edge", edge_buffer);
        compute.SetBuffer(kernel, "_Edge", edge_buffer);

        compute.SetBuffer(kernel, "_Sparse", sparseBuffer);

        if (color_image != null)
            material.SetTexture("_colorMap", color_image);
    }

    private Texture2D CopyTexture(Texture2D input_texture)
    {
        if (input_texture == null)
            return null;

        Texture2D copy = new Texture2D(input_texture.width, input_texture.height, input_texture.format, input_texture.mipmapCount > 1);
        Graphics.CopyTexture(input_texture, copy);

        return copy;
    }

    private bool UpdateFrameData()
    {
        if (freezeCloud && hasFrameData)
            return false;

        compute.SetFloat("t", t);

        Vector4 intr = GetIntrinsicsVector();
        compute.SetVector("intrinsics", intr);

        Vector4 screenData = GetScreenDataVector();

        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);
        ApplyFrameShaderConstants();

        if (use_saved_meshes)
        {
            if (savedMeshUploaded)
                return false;

            if (depth_ar_saved == null || depth_ar_saved.Length == 0)
                return false;

            Array.Clear(depth_ar, 0, depth_ar.Length);
            sparseBuffer.SetData(depth_ar);

            int copyCount = Mathf.Min(depth_ar_saved.Length, frameSize);
            if (copyCount == frameSize)
            {
                depthBuffer.SetData(depth_ar_saved);
            }
            else
            {
                Array.Clear(depth_ar, 0, depth_ar.Length);
                Array.Copy(depth_ar_saved, depth_ar, copyCount);
                depthBuffer.SetData(depth_ar);
            }

            if (!ProcessDepth(depthBuffer))
                return false;
            savedMeshUploaded = true;
            hasFrameData = true;
            return true;
        }

        if (spotObserverClient == null || !spotObserverClient.TryGetCameraFrame(SpotObserverStreamIdx, SpotObserverCameraIndex, out SpotObserverClient.CameraDepthFrame frame))
        {
            return false;
        }

        if (hasFrameData && frame.Sequence == lastFrameSequence)
        {
            return false;
        }

        // Borrow texture and depth buffer from SpotObserverClient. Do not destroy or release them here.
        color_image = frame.ColorTexture;
        lastFrameSequence = frame.Sequence;

        if (!ProcessDepth(frame.DepthBuffer))
            return false;
        hasFrameData = true;
        return true;
    }

    private void Update()
    {
        if (depthManager != null && !depthManager.show_spot)
        {
            return;
        }

        bool frameUpdated = UpdateFrameData();
        SetGOPosition();

        if (!hasFrameData)
            return;

        if (frameUpdated)
        {
            SetProperties();
            compute.SetFloat("y_max", depthManager != null ? depthManager.y_max : float.NegativeInfinity);
            compute.SetFloat("z_max", depthManager != null ? depthManager.z_max : float.NegativeInfinity);

            compute.SetFloat("edgeThreshold", depthManager != null ? depthManager.edgeThreshold : 0.0f);
            compute.SetFloat("meanThreshold", depthManager != null ? depthManager.meanThreshold : 0.0f);
            compute.SetBool("activate_edge_detection", depthManager != null && depthManager.activate_edge_detection);

            compute.Dispatch(edge_kernel, Mathf.CeilToInt(frameWidth / 16f), Mathf.CeilToInt(frameHeight / 16f), 1);
            compute.Dispatch(kernel, Mathf.CeilToInt(population / 256f), 1, 1);
        }

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    public Matrix4x4 get_current_pose()
    {
        return Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1));
    }

    private IEnumerator ToggleReadyToFreezeAfterDelay(float waitTime)
    {
        freeze_lock = true;

        yield return new WaitForSecondsRealtime(waitTime);
        ready_to_freeze = true;
        start_completion = true;
        if (enableDebugLogging)
            Debug.LogWarning("DEPTH IS READY AGAIN");
        freeze_lock = false;
    }

    private IEnumerator ToggleReadyToDepthAfterDelay(float waitTime)
    {
        yield return new WaitForSecondsRealtime(waitTime);
        if (enableDebugLogging)
            Debug.LogWarning("DEPTH IS READY");
        start_completion = true;
    }

    public void continue_update()
    {
        start_completion = false;
        if (enableDebugLogging)
            Debug.LogWarning("SET TO FALSE");
        if (ready_to_freeze && !freeze_lock)
        {
            StartCoroutine(ToggleReadyToFreezeAfterDelay(4.0f));
            StartCoroutine(ToggleReadyToDepthAfterDelay(4.0f));
            ready_to_freeze = false;
            start_completion = false;
        }
    }

    public Vector3 get_normal()
    {
        if (mainCameraRot == null)
            return -Vector3.forward;

        var relRot = Quaternion.Euler(0f, mainCameraRot.rotation.eulerAngles.y, 0f);
        var res = relRot * new Vector3(0f, 0f, -1.0f);
        return res.normalized;
    }

    private Mesh CreateQuad(float width = 1f, float height = 1f)
    {
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        var tris = new int[6] {
            0, 2, 1,
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    public void toggleFreezeCloud()
    {
        freezeCloud = !freezeCloud;

        if (use_saved_meshes)
            return;

        if (freezeCloud && color_image != null)
        {
            DestroyFrozenColorImage();
            // Live frame textures are owned by SpotObserverClient; frozen clouds need their own snapshot.
            frozenColorImage = CopyTexture(color_image);
            if (frozenColorImage != null)
            {
                color_image = frozenColorImage;
                material.SetTexture("_colorMap", color_image);
            }
        }
        else if (!freezeCloud)
        {
            DestroyFrozenColorImage();
            lastFrameSequence = ulong.MaxValue;
        }
    }

    public void setCloudFreeze(bool freeze)
    {
        if (freezeCloud == freeze)
            return;

        toggleFreezeCloud();
    }

    private void DestroyFrozenColorImage()
    {
        if (frozenColorImage != null)
        {
            Destroy(frozenColorImage);
            frozenColorImage = null;
        }
    }

    private static void ReleaseBuffer(ref ComputeBuffer buffer)
    {
        if (buffer == null)
            return;

        buffer.Release();
        buffer = null;
    }

    TESTMEPLEASE!
    private void OnDisable()
    {
        DestroyFrozenColorImage();

        if (mesh != null)
        {
            Destroy(mesh);
            mesh = null;
        }

        if (ownsColorImage && color_image != null)
        {
            Destroy(color_image);
        }
        color_image = null;
        ownsColorImage = false;
        hasFrameData = false;

        ReleaseBuffer(ref meshPropertiesBuffer);
        ReleaseBuffer(ref argsBuffer);
        ReleaseBuffer(ref depth_ar_buffer);
        ReleaseBuffer(ref depthBuffer);
        ReleaseBuffer(ref sparseBuffer);
        ReleaseBuffer(ref edge_buffer);
        ReleaseBuffer(ref optical_flow_buffer);
    }

    private void OnEnable()
    {
        Setup();
    }
}
