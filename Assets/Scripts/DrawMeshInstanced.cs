using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using System.Text;
using static RosSharp.Urdf.Link.Visual.Material;
using System;
using JetBrains.Annotations;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;
using Unity.VisualScripting;
//using System;

public class DrawMeshInstanced : MonoBehaviour
{
    public DepthAveraging averager;

    bool new_depth_to_render = false;
    public ICPLauncher icp_launcher;
    float3[] current_icp_res;
    private Matrix4x4 icp_trans;

    public DepthManager depthManager;
    public int camera_index;

    public Transform mainCameraRot;

    public float range;

    public Texture2D color_image;
    public Texture2D depth_image;

    public int imageScriptIndex;

    public Material material;

    public RawImageSubscriber depthSubscriber;  // ROS subscriber that holds the depth array
    public JPEGImageSubscriber colorSubscriber; // ROS subscriber holding the color image
    public bool savePointCloud;                 // allow user to save point cloud

    public ComputeShader compute;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer depthBuffer;
    private ComputeBuffer sparseBuffer;
    private ComputeBuffer edge_buffer;
    private ComputeBuffer icp_res_buffer;

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
    public int CX;
    public int CY;
    public float FX;
    public float FY;
    public float facedAngle;
    public float t;
    public float pS;    // point scalar
    //public uint counter;
    //private uint numUpdates;
    private bool calculate_icp = true;

    public float size_scale; //hack to current pointcloud viewing

    public bool use_saved_meshes = false; // boolean that determines whether to use saved meshes or read in new scene data from ROS
    private float[] depth_ar_saved;

    private bool freezeCloud = false; // boolean that freezes this point cloud
    private float[] depth_ar;
    private float[] avged_sparse = new float[640 * 480]; // averaged depth array

    private MeshProperties[] globalProps;

    bool start_completion = true;
    bool ready_to_freeze = true;
    bool freeze_lock = false;

    float[,] depth_avg_buffer = new float[30, 640 * 480];
    float[] res_depth = new float[640 * 480];

    //private MeshProperties[] generalUseProps;

    ComputeBuffer depth_ar_buffer;

    int buffer_pos = 0;

    int kernel;
    int edge_kernel;

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties
    {
        public Vector4 pos;

        public static int Size()
        {
            return
                sizeof(float) * 4; // position;
        }
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
        total_population = height * width;
        population = (uint)(total_population / downsample);

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
                    depth_ar = new float[length];
                    depth_ar_saved = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        depth_ar_saved[i] = reader.ReadSingle();
                    }
                }
            }

            //if (imageScriptIndex == 0)
            //{
            //    for (int y = 10; y < 10 + 300; y++)
            //    {
            //        for (int x = 10; x < 10 + (int)width - 20; x++)
            //        {
            //            int index = y * (int)width + x;
            //            depth_ar[index] = -0.0f;
            //        }
            //    }
            //}

            //if (camera_index == 1)
            //{
            //    for (int y = (int)height - 300; y < (int)height; y++)
            //    {
            //        for (int x = 10; x < 10 + (int)width - 20; x++)
            //        {
            //            int index = y * (int)width + x;
            //            depth_ar[index] = -0.0f;
            //        }
            //    }
            //}



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

            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);
        }
        else
        {
            Destroy(depth_image);
            depth_ar = new float[height * width];
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
        }

        globalProps = GetProperties();



        //inp_stm.Close();

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        // bounds = new Bounds(transform.position, Vector3.one * (range + 1));
        bounds = new Bounds(Vector3.zero, Vector3.one * (range + 1));

        // Pass 1 / width and 1 / height to material shader
        // [2023-10-30][JHT] Why do we need to do this? That data is (almost) all in 'screenData' that gets passed in later.
        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        //material.SetFloat("a",target.rotation.y * 0);
        //Debug.Log(auxTarget.eulerAngles);
        material.SetFloat("a", get_target_rota());
        material.SetFloat("pS", pS);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);

        InitializeBuffers();

        meshPropertiesBuffer.SetData(globalProps);
    }

    private float get_target_rota()
    {
        //Debug.Log(convert_angle(target.eulerAngles.y).ToString() + "     " + convert_angle(auxTarget.eulerAngles.y).ToString());
        if (auxTarget == null || true) { return convert_angle(target.eulerAngles.y) * 2; }
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

        if (width == 0 || height == 0 || depth_ar == null || depth_ar.Length == 0 || true)
        {
            return properties;
        }

        uint i;

        Vector4 initialValue = new Vector4( 0, 0, 0, 1 );

        for (uint pop_i = 0; pop_i < population; pop_i++)
        {
            // TODO: Handle downsampling correctly
            i = pop_i * downsample;
            properties[pop_i].pos = initialValue;

        }

        return properties;
    }

    private void InitializeBuffers()
    {

        depth_ar_buffer = new ComputeBuffer(480 * 640, sizeof(float) * 3);
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
        MeshProperties[] properties = new MeshProperties[population];


        meshPropertiesBuffer = new ComputeBuffer((int)population, MeshProperties.Size());
        meshPropertiesBuffer.SetData(GetProperties());

        depthBuffer = new ComputeBuffer((int)depth_ar.Length, sizeof(float));
        sparseBuffer = new ComputeBuffer((int)(480 * 640), sizeof(float));
        edge_buffer = new ComputeBuffer((int)(480 * 640), sizeof(float));
        depthBuffer.SetData(depth_ar);

        SetProperties();
        SetGOPosition();
    }

    private void SetGOPosition()
    {
        compute.SetMatrix("_GOPose", Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1)));
        compute.SetMatrix("_ICPTrans", icp_trans);
        // compute.SetMatrix("_GOPose", Matrix4x4.TRS(Vector3.zero, transform.rotation, new Vector3(1, 1, 1)));
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

        compute.SetBool("show_sampling_res", depthManager.show_sampling_res);
        if (depthManager.show_sampling_res)
        {
            current_icp_res = icp_launcher.get_current_float3(imageScriptIndex);
            Debug.Log(current_icp_res.Length);
            icp_res_buffer.SetData(current_icp_res);
        }
        compute.SetBuffer(kernel, "_ICP_Res", icp_res_buffer);

        compute.SetBuffer(kernel, "_Sparse", sparseBuffer);


        material.SetTexture("_colorMap", color_image);

        //compute.SetBuffer(kernel, "_Depth", depthBuffer);


        //Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        //compute.SetVector("intrinsics", intr);
        //material.SetVector("intrinsics", intr);

        //Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        //compute.SetVector("screenData", screenData);
        //material.SetVector("screenData", screenData);

        //compute.SetFloat("samplingSize", downsample);
        //material.SetFloat("samplingSize", downsample);
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

    private void UpdateTexture()
    {
        compute.SetFloat("t", t);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);

        if (use_saved_meshes)
        {
            for (int i = 0; i < 480 * 640; i++)
            {
                depth_ar[i] = depth_ar_saved[i];
            }
            new_depth_to_render = true;
        }
        else
        {
            DestroyImmediate(color_image, true);
            color_image = copy_texture(colorSubscriber.texture2D);
            depth_ar = depthSubscriber.getDepthArr();
            if (depthSubscriber.new_depth == true)
            {
                new_depth_to_render = true;
            }
            depthSubscriber.new_depth = false;
        }

        if (depth_ar.Length < 640 * 480)
        {
            return;
        }
        sparseBuffer.SetData(avged_sparse);

        calculate_icp = true;
        if (imageScriptIndex > 1) { calculate_icp = false; } // depth manager 2

        //if (depthManager.avg_before_completion)
        //{
        //    for (int j = 0; j < depth_ar.Length; j++)
        //    {
        //        depth_avg_buffer[buffer_pos, j] = depth_ar[j];
        //        res_depth[j] = 0.0f;
        //    }

        //    int num_valid = 0;
        //    float total_sum = 0.0f;
        //    for (int j = 0; j < 480 * 640; j++)
        //    {
        //        num_valid = 0;
        //        total_sum = 0.0f;
        //        for (int i = 0; i < 30; i++)
        //        {

        //            if (depth_avg_buffer[i, j] >= 1.0f)
        //            {
        //                num_valid += 1;
        //                total_sum += depth_avg_buffer[i, j];
        //            }
        //        }
        //        if (num_valid != 0)
        //        {
        //            res_depth[j] = total_sum / num_valid;
        //        }
        //    }


        //    buffer_pos += 1;
        //    if (buffer_pos >= 30)
        //    {
        //        buffer_pos = 0;
        //    }
        //}



        //temp_output_left = Averager.averaging(temp_output_left, is_not_moving, mean_averaging, median_averaging, edge_detection, edge_threshold);
        (depth_ar_buffer, icp_trans, avged_sparse) = depthManager.update_depth_from_renderer(color_image, depth_ar, camera_index, calculate_icp, new_depth_to_render, depthManager.avg_before_completion);

        //if (depthManager.avg_before_completion)
        //{
        //    (depth_ar_buffer, icp_trans) = depthManager.update_depth_from_renderer(color_image, res_depth, camera_index, calculate_icp, new_depth_to_render);
        //}
        //else
        //{
        //    (depth_ar_buffer, icp_trans) = depthManager.update_depth_from_renderer(color_image, depth_ar, camera_index, calculate_icp, new_depth_to_render);
        //}

        new_depth_to_render = false;
        if (imageScriptIndex > 1) { icp_trans = Matrix4x4.identity; } // depth manager 2


    }

    private void Update()
    {
        if (!depthManager.show_spot)
        {
            return;
        }


        UpdateTexture();

        //Debug.Log("UPDATE");

        //SetProperties enables point cloud to move when game object moves, but is laggier due to redrawing. Just comment it out for performance improvement;
        //transform.LookAt(target);
        SetProperties();
        SetGOPosition();


        compute.SetFloat("y_max", depthManager.y_max);
        compute.SetFloat("z_max", depthManager.z_max);

        compute.SetFloat("edgeThreshold", depthManager.edgeThreshold);
        compute.SetFloat("meanThreshold", depthManager.meanThreshold);
        compute.SetInt("maxNeighbourNum", depthManager.maxNeighbourNum);
        compute.SetBool("activate_edge_detection", depthManager.activate_edge_detection);

        //update the color image
        //counter += 1;

        //Debug.Log("UPDATE");
        //DateTime localTime = DateTime.Now;
        //float deltaTime = Time.deltaTime;
        //long microseconds = localTime.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
        //Debug.Log("updates per second: " + (counter/Time.realtimeSinceStartup).ToString() + " updates: " + counter.ToString() + " deltaTime: " + Time.realtimeSinceStartup.ToString());

        // We used to just be able to use `population` here, but it looks like a Unity update imposed a thread limit (65535) on my device.
        // This is probably for the best, but we have to do some more calculation.  Divide population by numthreads.x (declared in compute shader).
        compute.Dispatch(edge_kernel, Mathf.CeilToInt(population / 256f), 1, 1);
        compute.Dispatch(kernel, Mathf.CeilToInt(population / 256f), 1, 1);
        // Question:
        //          - where is the point cloud? -> stored at where
        //          - what is the format of the point cloud after calling the compute shader
        //          - How to access the 2 point cloud from 2 spots?
        //          
        OrientQuad(mesh, get_normal());
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        //numUpdates += 1;
    }

    private Vector4 pixel_to_vision_frame(uint i, uint j, float depth)
    {
        //int CX = 320;
        //int CY = 240;

        //float FX = (float)552.029101;
        //float FY = (float)552.029101;

        float x = (j - CX) * depth / FX;
        float y = (i - CY) * depth / FY;

        Vector4 ret = new Vector4(x, y, depth, 1f);
        return (ret);

    }

    public Matrix4x4 get_current_pose()
    {
        return Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1));
    }

    public Vector4 get_screenData()
    {
        return new Vector4((float)width, (float)height, 1 / (float)width, FY);
    }

    public Vector4 get_intrinsics()
    {
        return new Vector4((float)CX, (float)CY, FX, FY);
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
        Debug.LogWarning("DEPTH IS READY AGAIN");
        averager.ClearBuffer();
        freeze_lock = false;
    }

    private IEnumerator ToggleReadyToDepthAfterDelay(float waitTime)
    {
        yield return new WaitForSecondsRealtime(waitTime);
        Debug.LogWarning("DEPTH IS READY");
        start_completion = true;
    }

    public void continue_update()
    {
        start_completion = false;
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

    void OrientQuad(Mesh mesh, Vector3 vec_dir)
    {
        if (vec_dir.sqrMagnitude < 1e-6f) return;          // guard against zero
        Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, vec_dir.normalized);

        float w = size_scale * .5f;
        float h = size_scale * .5f;

        Vector3[] v = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        Vector3[] n = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        for (int i = 0; i < v.Length; ++i)
        {
            v[i] = rot * v[i];      // rotate vertex positions
            n[i] = rot * n[i];      // rotate normals
        }

        mesh.vertices = v;
        mesh.normals = n;
        //mesh.RecalculateBounds();
    }

    private Mesh CreateTri(float width = 1f, float height = 1f)
    {
        // Create a quad mesh.
        var mesh = new Mesh();

        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[3] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0)
        };

        var tris = new int[3] {
            // lower left tri.
            0, 2, 1
        };

        var normals = new Vector3[3] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[3] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    public void toggleFreezeCloud()
    {
        float[] temp_depth;
        Texture2D temp_texture;


        freezeCloud = !freezeCloud;

        // if turning on freeze, deep copy arrays
        if (freezeCloud)
        {
            temp_depth = new float[depth_ar.Length];
            Array.Copy(depth_ar, temp_depth, depth_ar.Length);
            depth_ar = temp_depth;

            temp_texture = new Texture2D(color_image.width, color_image.height);
            temp_texture.SetPixels(color_image.GetPixels());
            temp_texture.Apply();
            color_image = temp_texture;
        }
    }

    public void setCloudFreeze(bool freeze)
    {
        freezeCloud = freeze;
    }

    private void Start()
    {
        // See OnEnable
    }


    private void OnDisable()
    {
        // Release gracefully.
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

        if (depth_ar_buffer != null)
        {
            depth_ar_buffer.Release();
        }
        depth_ar_buffer = null;
    }

    private void OnEnable()
    {
        Setup();
    }
}