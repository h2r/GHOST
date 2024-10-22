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
using Unity.Sentis;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;

public class DrawMeshInstanced : MonoBehaviour
{
    public float min_estimation_dis;
    public float max_inpainting_dis;
    public uint size_of_new_points;

    public bool freeze_without_action;
    public int latency_frames;
    bool ready_to_freeze = false;

    public float y_min;
    public float z_max;

    public float range;

    public Texture2D color_image;
    public Texture2D depth_image;

    public int imageScriptIndex;

    public Material material;

    public RawImageSubscriber depthSubscriber;  // ROS subscriber that holds the depth array
    public JPEGImageSubscriber colorSubscriber; // ROS subscriber holding the color image

    public ComputeShader compute;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    //private ComputeBuffer depthBuffer;

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

    public float size_scale; //hack to current pointcloud viewing

    public bool use_saved_meshes = false; // boolean that determines whether to use saved meshes or read in new scene data from ROS
    private bool freezeCloud = false; // boolean that freezes this point cloud
    private float[] depth_ar = new float[480 * 640];
    private float[] depth_ar_saved = new float[480 * 640];

    private MeshProperties[] globalProps;

    public float delta_x;
    public float delta_y;
    public float delta_z;

    bool freeze_lock = false;

    public int camera_index;
    public DepthManager depthManager;
    bool start_completion = true;

    ComputeBuffer depth_ar_buffer;
    ComputeBuffer sparse_buffer;

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties
    {
        public Vector4 pos;

        public static int Size()
        {
            return sizeof(float) * 4; // position;
        }
    }

    private void Update()
    {
        //float startTime = Time.realtimeSinceStartup;

        UpdateTexture();

        //float t1 = Time.realtimeSinceStartup;

        int kernel = compute.FindKernel("CSMain");
        SetProperties();
        compute.SetMatrix("_GOPose", Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1)));
        compute.Dispatch(kernel, Mathf.CeilToInt(population / 256), 1, 1);


        //float t2 = Time.realtimeSinceStartup;


        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);

        //float t3 = Time.realtimeSinceStartup;

        //float d1 = t1 - startTime;
        //float d2 = t2 - t1;
        //float d3 = t3 - t2;

        //Debug.Log($"Runtime of HeavyComputationFunction: {d1 * 1000} seconds: {d2 * 1000} seconds: {d3 * 1000} seconds");


    }

    private void SetProperties()
    {
        int kernel = compute.FindKernel("CSMain");
        material.SetFloat("a", target.eulerAngles.y * 0.00872f * 2.0f);
        material.SetFloat("pS", pS);
        material.SetTexture("_colorMap", color_image);

        //depthBuffer.SetData(depth_ar);
        meshPropertiesBuffer.SetData(globalProps);

        material.SetBuffer("_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        compute.SetBuffer(kernel, "_Depth", depth_ar_buffer);
        compute.SetBuffer(kernel, "_SparseDepth", sparse_buffer);
        compute.SetFloat("min_estimation_dis", min_estimation_dis);
        compute.SetFloat("max_inpainting_dis", max_inpainting_dis);
        compute.SetFloat("size_of_new_points", (int)size_of_new_points);
    }

    private IEnumerator ToggleReadyToFreezeAfterDelay(float waitTime)
    {
        freeze_lock = true;

        yield return new WaitForSeconds(waitTime);
        ready_to_freeze = true;
        start_completion = true;
        freeze_lock = false;
    }

    private IEnumerator ToggleReadyToDepthAfterDelay(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        start_completion = true;
    }

    public void continue_update()
    {
        start_completion = false;
        if (ready_to_freeze & !freeze_lock)
        {
            StartCoroutine(ToggleReadyToFreezeAfterDelay(1.0f / 30.0f * latency_frames));
            StartCoroutine(ToggleReadyToDepthAfterDelay(1.0f / 30.0f * latency_frames / 2.0f));
            ready_to_freeze = false;
            start_completion = false;
        }
    }

    private void UpdateTexture()
    {
        if (freezeCloud || (ready_to_freeze && freeze_without_action))// || (!first_run && depth_process_lock))
        {
            return;
        }

        if (use_saved_meshes)
        {
            //for (int i = 0; i < 480 * 640; i++)
            //{
            //    depth_ar[i] = depth_ar_saved[i];
            //}

            //float noiseMin = -0.0006f;
            //float noiseMax = 0.0002f;
            //for (int i = 0; i < depth_ar.Length; i++)
            //{
            //    depth_ar[i] += Random.Range(noiseMin, noiseMax);
            //}
            depth_ar = depth_ar_saved;
        }
        else
        {
            DestroyImmediate(color_image, true);
            color_image = copy_texture(colorSubscriber.texture2D);
            depth_ar = depthSubscriber.getDepthArr();
        }

        sparse_buffer.SetData(depth_ar);
        depth_ar_buffer = depthManager.update_depth_from_renderer(color_image, depth_ar, camera_index);
    }

    private Texture2D copy_texture(Texture2D input_texture)
    {
        if (input_texture == null)
            return null;

        Texture2D copy = new Texture2D(input_texture.width, input_texture.height, input_texture.format, input_texture.mipmapCount > 1);
        Graphics.CopyTexture(input_texture, copy);

        return copy;
    }

    public bool get_ready_to_freeze()
    {
        return start_completion;
    }

    private void OnDisable()
    {
        // Release buffers and other assets to avoid memory leaks.
        if (meshPropertiesBuffer != null)
            meshPropertiesBuffer.Release();
        if (argsBuffer != null)
            argsBuffer.Release();
        if (depth_ar_buffer != null)
            depth_ar_buffer.Release();
        if (sparse_buffer != null)
            sparse_buffer.Release();

        // Clear references to ensure garbage collector can reclaim memory.
        meshPropertiesBuffer = null;
        argsBuffer = null;
        depth_ar_buffer = null;
        sparse_buffer = null;
    }

    private void OnDestroy()
    {
        OnDisable();
    }

    private void OnEnable()
    {
        StartCoroutine(ToggleReadyToFreezeAfterDelay(5.0f));

        pS = 1.0f;

        total_population = height * width;
        population = (uint)(total_population / downsample);

        Mesh mesh = CreateQuad(size_scale, size_scale);
        this.mesh = mesh;

        if (use_saved_meshes)
        {
            using (var stream = File.Open("Assets/PointClouds/mesh_array_" + imageScriptIndex, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    int length = reader.ReadInt32();
                    depth_ar = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        depth_ar[i] = reader.ReadSingle();
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

            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
            depth_image.SetPixelData(depth_ar, 0);

            for (int i = 0; i < 480 * 640; i++)
            {
                depth_ar_saved[i] = depth_ar[i];
            }
        }
        else
        {
            Destroy(depth_image);
            depth_ar = new float[height * width];
            depth_image = new Texture2D((int)width, (int)height, TextureFormat.RFloat, false, false);
        }

        globalProps = new MeshProperties[population * (1 + size_of_new_points)];

        bounds = new Bounds(Vector3.zero, Vector3.one * (range + 1));

        InitializeMaterials();
        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        int kernel = compute.FindKernel("CSMain");

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = mesh.GetIndexCount(0);
        args[1] = population * (1 + size_of_new_points);
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[population];

        meshPropertiesBuffer = new ComputeBuffer((int)population * (1 + (int)size_of_new_points), MeshProperties.Size());
        meshPropertiesBuffer.SetData(globalProps);

        depth_ar_buffer = new ComputeBuffer((int)depth_ar.Length, sizeof(float));
        sparse_buffer = new ComputeBuffer((int)depth_ar.Length, sizeof(float));
    }

    private void InitializeMaterials()
    {
        // Pass 1 / width and 1 / height to material shader
        // [2023-10-30][JHT] Why do we need to do this? That data is (almost) all in 'screenData' that gets passed in later.
        material.SetFloat("width", 1.0f / width);
        material.SetFloat("height", 1.0f / height);
        material.SetInt("w", (int)width);
        material.SetFloat("a", target.eulerAngles.y * 0.00872f * 2.0f);
        material.SetFloat("pS", pS);

        Vector4 intr = new Vector4((float)CX, (float)CY, FX, FY);
        compute.SetVector("intrinsics", intr);
        material.SetVector("intrinsics", intr);

        Vector4 screenData = new Vector4((float)width, (float)height, 1 / (float)width, FY);
        compute.SetVector("screenData", screenData);
        material.SetVector("screenData", screenData);

        compute.SetFloat("samplingSize", downsample);
        material.SetFloat("samplingSize", downsample);

        compute.SetFloat("t", t);
        compute.SetFloat("y_min", y_min);
        compute.SetFloat("z_max", z_max);

        compute.SetFloat("dx", delta_x);
        compute.SetFloat("dy", delta_y);
        compute.SetFloat("dz", delta_z);
    }

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

    // =============================================================================== //
    //                                     PUBLIC                                        //
    // =============================================================================== //

    public void toggleFreezeCloud()
    {
        float[] temp_depth;
        Texture2D temp_texture;

        freezeCloud = !freezeCloud;

        //if turning on freeze, deep copy arrays
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
}