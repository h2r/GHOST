using JetBrains.Annotations;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Unity.InferenceEngine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static RosSharp.Urdf.Link.Visual.Material;
using static SpotObserverClient;
using Debug = UnityEngine.Debug;
//using System;


public class DrawMeshInstanced : MonoBehaviour
{
    public DepthAveraging averager;

    public ICPLauncher icp_launcher;
    float3[] current_icp_res;
    private Matrix4x4 icp_trans;

    public DepthManager depthManager;
    public SpotCamera SpotObserverCameraIndex;
    public int SpotObserverStreamIdx;

    public Transform mainCameraRot;

    public float range;

    public Texture2D color_image;
    public Texture2D depth_image;
    public Tensor depth_tensor;

    public int imageScriptIndex;

    public Material material;

    public SpotObserverClient spotObserverClient;
    public PoseConsistentVideoDepth CVD;

    public bool savePointCloud;                 // allow user to save point cloud

    public ComputeShader compute;

    // Renderer-owned float3 point/depth output consumed by the point cloud compute shader.
    private ComputeBuffer depth_ar_buffer;
    // Raw scalar depth input. Borrowed from SpotObserverClient in the live path;
    // points at renderer-owned depthBuffer when rendering saved meshes.
    private ComputeBuffer currentRawDepthBuffer;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer depthBuffer;
    private ComputeBuffer sparseBuffer;
    private ComputeBuffer edge_buffer;
    private ComputeBuffer icp_res_buffer;

    private ComputeBuffer optical_flow_buffer;

    public Transform target;
    public Transform auxTarget; // In case someone changes the offset rotation

    private Mesh mesh;
    private Bounds bounds;

    public float noise_range;

    private uint total_population;
    private uint population;
    public uint downsample;
    public uint height;
    public uint width;
    public float facedAngle;
    public float t;
    public float pS;    // point scalar
    //public uint counter;
    //private uint numUpdates;
    public float size_scale; //hack to current pointcloud viewing

    public bool use_saved_meshes = false; // boolean that determines whether to use saved meshes or read in new scene data from ROS
    private float[] depth_ar_saved;

    private bool freezeCloud = false; // boolean that freezes this point cloud
    private float[] depth_ar;

    private MeshProperties[] globalProps;

    bool start_completion = true;
    bool ready_to_freeze = true;
    bool freeze_lock = false;

    //private MeshProperties[] generalUseProps;

    int kernel;
    int edge_kernel;
    private int frameWidth;
    private int frameHeight;
    private int frameSize;
    private ulong lastFrameSequence;
    private bool hasFrameData;
    private bool savedMeshUploaded;
    private bool ownsColorImage;
    private Texture2D frozenColorImage;

    public bool enableDebugLogging = false;

    // Downstream GPU consumers may bind this buffer, but must not release it.
    public ComputeBuffer CurrentDepthBuffer => currentRawDepthBuffer;
    // Renderer-owned float3 output for consumers that need point-space depth.
    public ComputeBuffer CurrentPointDepthBuffer => depth_ar_buffer;
    public bool HasCurrentDepthBuffer => currentRawDepthBuffer != null && hasFrameData;
    

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience function which returns the stride of the struct.
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
        pS = 1.0f;
        kernel = compute.FindKernel("CSMain");
        edge_kernel = compute.FindKernel("EdgeDetector");

        //size_scale = 0.002f;
        //width = 640;
        //height = 480;
        //width = 424;
        //height = 240;
        //counter = 0;
        //numUpdates = 0;
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
        total_population = height * width;
        population = total_population / downsample;
        if (population == 0)
            population = 1;
        lastFrameSequence = ulong.MaxValue;
        hasFrameData = false;
        savedMeshUploaded = false;
        currentRawDepthBuffer = null;

        if (CVD != null)
        {
            CVD.Width = frameWidth;
            CVD.Height = frameHeight;
        }

        Mesh mesh = CreateQuad(size_scale, size_scale);
        this.mesh = mesh;

        //generalUseProps = new MeshProperties[population];

        // Use saved meshes
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

            depth_ar = new float[frameSize];
            Array.Copy(depth_ar_saved, depth_ar, Mathf.Min(depth_ar_saved.Length, depth_ar.Length));

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

            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);
        }
        else
        {
            Destroy(depth_image);
            depth_ar = new float[frameSize];
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            ownsColorImage = false;
        }

        globalProps = GetProperties();

        icp_trans = Matrix4x4.identity;

        //inp_stm.Close();

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(transform.position, Vector3.one * (range + 1));

        // Pass 1 / width and 1 / height to material shader
        // [2023-10-30][JHT] Why do we need to do this? That data is (almost) all in 'screenData' that gets passed in later.
        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        //material.SetFloat("a",target.rotation.y * 0);
        //Debug.Log(auxTarget.eulerAngles);
        material.SetFloat("a", get_target_rota());
        material.SetFloat("pS", pS);

        Vector4 intr = GetIntrinsicsVector();
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = GetScreenDataVector();
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);
        ApplyFrameShaderConstants();

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);

        InitializeBuffers();

        meshPropertiesBuffer.SetData(globalProps);

        // Hack: Initialize sparsebuffer to 0
        for (int i = 0; i < frameSize; i++)
        {
            depth_ar[i] = 0.0f;
        }
        sparseBuffer.SetData(depth_ar);
    }

    private bool process_depth(ComputeBuffer in_depth)
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

    private float get_target_rota()
    {
        //Debug.Log(convert_angle(target.eulerAngles.y).ToString() + "     " + convert_angle(auxTarget.eulerAngles.y).ToString());
        if (target == null)
        {
            return 0.0f;
        }
        if (auxTarget == null) { return convert_angle(target.eulerAngles.y) * 2; }
        else
        {
            return convert_angle(target.eulerAngles.y) + convert_angle(auxTarget.eulerAngles.y);
        }
    }

    private float convert_angle(float a) // Unity is giving me the sin of an angle when I just want the angle
    {
        //a = (a + 180) % 360 - 180;
        return a * (float)0.00872;
    }

    private MeshProperties[] GetProperties()
    {
        MeshProperties[] properties = new MeshProperties[population];

        if (width == 0 || height == 0 || depth_ar == null || depth_ar.Length == 0)
        {
            return properties;
        }

        Vector4 initialValue = new Vector4( 0, 0, 0, 1 );

        for (uint pop_i = 0; pop_i < population; pop_i++)
        {
            // TODO: Handle downsampling correctly
            properties[pop_i].pos = initialValue;

        }

        return properties;
    }

    private void InitializeBuffers()
    {

        depth_ar_buffer = new ComputeBuffer(frameSize, sizeof(float) * 3);
        icp_res_buffer = new ComputeBuffer(160 * 120, sizeof(float) * 3);

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
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
        compute.SetMatrix("_GOPose", pose);
        compute.SetMatrix("_ICPTrans", icp_trans);
        material.SetMatrix("_GOPose", pose);
        bounds.center = transform.position;
        SetBillboardBasis();
        // compute.SetMatrix("_GOPose", Matrix4x4.TRS(Vector3.zero, transform.rotation, new Vector3(1, 1, 1)));
    }

    private void SetBillboardBasis()
    {
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
        //material.SetFloat("a", get_target_rota());
        //material.SetFloat("pS", pS);
        //depthBuffer.SetData(depth_ar);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);

        compute.SetBuffer(kernel, "_Depth", depth_ar_buffer);
        compute.SetBuffer(edge_kernel, "_Depth", depth_ar_buffer);
        compute.SetBuffer(edge_kernel, "_Edge", edge_buffer);
        compute.SetBuffer(kernel, "_Edge", edge_buffer);

        bool showSampling = depthManager != null && depthManager.show_sampling_res;
        compute.SetBool("show_sampling_res", showSampling);
        if (showSampling && icp_launcher != null)
        {
            current_icp_res = icp_launcher.get_current_float3(imageScriptIndex);
            if (current_icp_res != null)
            {
                if (enableDebugLogging)
                    Debug.Log(current_icp_res.Length);
                icp_res_buffer.SetData(current_icp_res);
            }
        }
        compute.SetBuffer(kernel, "_ICP_Res", icp_res_buffer);

        compute.SetBuffer(kernel, "_Sparse", sparseBuffer);


        if (color_image != null)
            material.SetTexture("_colorMap", color_image);
    }

    private Texture2D copy_texture(Texture2D input_texture)
    {
        if (input_texture == null)
            return null;

        Texture2D copy = new Texture2D(input_texture.width, input_texture.height, input_texture.format, input_texture.mipmapCount > 1);
        Graphics.CopyTexture(input_texture, copy);

        return copy;
    }

    public float[] get_depth()
    {
        return depth_ar;
    }

    private bool UpdateFrameData()
    {
        if (freezeCloud && hasFrameData)
            return false;

        compute.SetFloat("t", t);

        Vector4 intr = GetIntrinsicsVector();
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

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

            currentRawDepthBuffer = depthBuffer;
            if (!process_depth(currentRawDepthBuffer))
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

        // Borrow texture/tensor/buffer from SpotObserverClient. Do not destroy or release them here.
        color_image = frame.ColorTexture;
        depth_tensor = frame.DepthTensor;
        currentRawDepthBuffer = frame.DepthBuffer;
        lastFrameSequence = frame.Sequence;

        if (!process_depth(currentRawDepthBuffer))
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
            compute.SetInt("maxNeighbourNum", depthManager != null ? depthManager.maxNeighbourNum : 0);
            compute.SetBool("activate_edge_detection", depthManager != null && depthManager.activate_edge_detection);

            compute.Dispatch(edge_kernel, Mathf.CeilToInt(frameWidth / 16f), Mathf.CeilToInt(frameHeight / 16f), 1);
            compute.Dispatch(kernel, Mathf.CeilToInt(population / 256f), 1, 1);
        }

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private Vector4 pixel_to_vision_frame(uint i, uint j, float depth)
    {
        //int CX = 320;
        //int CY = 240;

        //float FX = (float)552.029101;
        //float FY = (float)552.029101;

        Vector4 intrinsics = GetIntrinsicsVector();
        float x = (j - intrinsics.x) * depth / intrinsics.z;
        float y = (i - intrinsics.y) * depth / intrinsics.w;

        Vector4 ret = new Vector4(x, y, depth, 1f);
        return (ret);

    }

    public Matrix4x4 get_current_pose()
    {
        return Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1));
    }

    public Vector4 get_screenData()
    {
        return GetScreenDataVector();
    }

    public Vector4 get_intrinsics()
    {
        return GetIntrinsicsVector();
    }

    public bool get_ready_to_freeze()
    {
        return start_completion;
    }

    private IEnumerator ToggleReadyToFreezeAfterDelay(float waitTime)
    {
        freeze_lock = true;

        yield return new WaitForSecondsRealtime(waitTime);
        ready_to_freeze = true;
        start_completion = true;
        if (enableDebugLogging)
            Debug.LogWarning("DEPTH IS READY AGAIN");
        averager?.ClearBuffer();
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
        if (ready_to_freeze & !freeze_lock)
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
        //Debug.LogWarning(res.ToString());
        return res.normalized;
    }

    //private Mesh CreateQuad(float width = 1f, float height = 1f, float depth = 1f)
    //{
    //    // Create a quad mesh.
    //    var mesh = new Mesh();

    //    float w = width * .5f;
    //    float h = height * .5f;
    //    float d = depth * .5f;

    //    var vertices = new Vector3[8] {
    //        new Vector3(-w, -h, -d),
    //        new Vector3(w, -h, -d),
    //        new Vector3(w, h, -d),
    //        new Vector3(-w, h, -d),
    //        new Vector3(-w, -h, d),
    //        new Vector3(w, -h, d),
    //        new Vector3(w, h, d),
    //        new Vector3(-w, h, d)
    //    };

    //    var tris = new int[3 * 2 * 6] {
    //        0, 3, 1,
    //        3, 2, 1,

    //        0,4,5,
    //        0,5,1,

    //        1,5,2,
    //        2,5,6,

    //        7,3,6,
    //        3,6,2,

    //        0,4,3,
    //        4,7,3,

    //        4,7,5,
    //        7,5,6
    //    };

    //    var normals = new Vector3[8] {
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,
    //        -Vector3.forward,

    //    };

    //    var uv = new Vector2[8] {
    //        new Vector2(0, 0),
    //        new Vector2(1, 0),
    //        new Vector2(1, 1),
    //        new Vector2(0, 1),
    //        new Vector2(0, 0),
    //        new Vector2(1, 0),
    //        new Vector2(1, 1),
    //        new Vector2(0, 1),
    //    };

    //    mesh.vertices = vertices;
    //    mesh.triangles = tris;
    //    mesh.normals = normals;
    //    mesh.uv = uv;

    //    return mesh;
    //}

    // Actually a cube, not a quad
    private Mesh CreateQuad(float width = 1f, float height = 1f)
    {
        // Create a quad mesh.
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
            // lower left tri.
            0, 2, 1,
            // lower right tri
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
            frozenColorImage = copy_texture(color_image);
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

    private void Start()
    {
        // See OnEnable
    }


    private void OnDisable()
    {
        // Release gracefully.
        DestroyFrozenColorImage();

        if (mesh != null)
        {
            Destroy(mesh);
            mesh = null;
        }

        if (depth_image != null)
        {
            Destroy(depth_image);
            depth_image = null;
        }

        if (ownsColorImage && color_image != null)
        {
            Destroy(color_image);
        }
        color_image = null;
        ownsColorImage = false;
        currentRawDepthBuffer = null;
        hasFrameData = false;

        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;

        if (depth_ar_buffer != null)
            depth_ar_buffer.Release();

        depth_ar_buffer = null;

        if (depthBuffer != null)
        {
            depthBuffer.Release();
        }
        depthBuffer = null;

        if (sparseBuffer != null)
        {
            sparseBuffer.Release();
        }
        sparseBuffer = null;

        if (edge_buffer != null)
        {
            edge_buffer.Release();
        }
        edge_buffer = null;

        if (icp_res_buffer != null)
        {
            icp_res_buffer.Release();
        }
        icp_res_buffer = null;

        if (optical_flow_buffer != null)
        {
            optical_flow_buffer.Release();
        }
        optical_flow_buffer = null;
    }

    private void OnEnable()
    {
        Setup();
    }
}
