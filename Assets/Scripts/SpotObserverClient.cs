using RosSharp.RosBridgeClient.MessageTypes.Std;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.InferenceEngine;

using UnityEngine;


public class SpotObserverClient : MonoBehaviour
{

    [DllImport("SpotObserverLib", CharSet = CharSet.Ansi)]
    private static extern int SOb_ConnectToSpot(
        string robot_ip,
        string username,
        string password
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_DisconnectFromSpot(int robot_id);

    [DllImport("SpotObserverLib", CharSet = CharSet.Ansi)]
    private static extern bool SOb_ReadCameraFeeds(
        int robot_id,
        uint camera_mask
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_RegisterUnityReadbackBuffers(
        int robot_id,
        uint cam_bit,
        IntPtr rgb_resource,
        IntPtr depth_resource,
        int img_buffer_size, // In bytes
        int depth_buffer_size // In bytes
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_PushNextImageSetToUnityBuffers(
        int robot_id
    );

    [DllImport("SpotObserverLib", CharSet = CharSet.Ansi)]
    private static extern IntPtr SOb_LoadModel(string modelPath, string backend);

    [DllImport("SpotObserverLib")]
    private static extern void SOb_UnloadModel(IntPtr model);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_LaunchVisionPipeline(int robot_id, IntPtr model);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_StopVisionPipeline(int robot_id);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_PushNextVisionPipelineImageSetToUnityBuffers(
        int robot_id
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_SetUnityLogCallback(
        LogCallback callback
    );

    [DllImport("SpotObserverLib", CharSet = CharSet.Ansi)]
    private static extern void SOb_ToggleDebugDumps(
        string dump_path
    );


    // Make sure the enum matches the C++ enum in spot-observer.h
    public enum SpotCamera
    {
        BACK = 0x1,
        FRONTLEFT = 0x2,
        FRONTRIGHT = 0x4,
        LEFT = 0x8,
        RIGHT = 0x10,
        HAND = 0x20,
        NUM_CAMERAS = 0x40,
    };

    private delegate void LogCallback(string message);

    private static void PluginLogCallback(string message)
    {
        Debug.Log("[SpotObserverLib] " + message);
    }

    public string RobotIP;
    public string username;
    public string password;

    public bool useVisionPipeline = true;
    public string depthCompletionModelFile;

    public bool enableDebugDumps = false;

    private int robot_id = -1;
    private bool isConnected = false;
    private bool isStreaming = false;
    private bool isVisionPipelineRunning = false;
    private IntPtr model;

    private Texture2D[] rgb_textures;

    //private Tensor<byte>[] rgb_tensors;
    private Tensor<float>[] depth_tensors;
    
    private IntPtr[] rgb_resources;
    private IntPtr[] depth_resources;

    private NativeHashMap<int, int> SpotCamToIdx;

    private uint[] cams = {
        (uint)SpotCamera.FRONTLEFT,
        (uint)SpotCamera.FRONTRIGHT
    };


    private void launch_vision_pipeline()
    {
        if (model == IntPtr.Zero)
        {
            Debug.LogError("No model loaded for vision pipeline.");
            return;
        }
        bool ret = SOb_LaunchVisionPipeline(robot_id, model);
        if (!ret)
        {
            Debug.LogError("Failed to launch vision pipeline on Spot robot.");
            return;
        }
        isVisionPipelineRunning = true;
    }

    private void stop_vision_pipeline()
    {
        bool ret = SOb_StopVisionPipeline(robot_id);
        if (!ret)
        {
            Debug.LogError("Failed to stop vision pipeline on Spot robot.");
            return;
        }
        isVisionPipelineRunning = false;
    }

    void Start()
    {
        SOb_SetUnityLogCallback(PluginLogCallback);
        if (enableDebugDumps)
        {
            SOb_ToggleDebugDumps("SPOT_OBSERVER_DUMPS");
        }

        RobotIP = RobotIP.Trim();
        robot_id = SOb_ConnectToSpot(RobotIP, username, password);
        if (robot_id < 0)
        {
            Debug.LogError("Failed to connect to Spot robot at " + RobotIP);
            isConnected = false;
            return;
        }
        Debug.Log("Connected to Spot robot at " + RobotIP);
        isConnected = true;

        // Initialize camera resources
        // TODO: Unhardcode shapes
        TensorShape rgb_shape = new TensorShape(1, 3, 480, 640); // RGB shape
        TensorShape depth_shape = new TensorShape(1, 1, 480, 640); // Depth shape

        int num_cams = cams.Length;

        // Initialize textures and tensors for each camera
        rgb_textures    = new Texture2D[num_cams];
        rgb_resources   = new IntPtr[num_cams];
        depth_resources = new IntPtr[num_cams];
        depth_tensors   = new Tensor<float>[num_cams];

        SpotCamToIdx = new NativeHashMap<int, int>(cams.Length, Allocator.Persistent);
        for (int i = 0; i < cams.Length; i++)
        {
            SpotCamToIdx[(int)cams[i]] = i;
        }

        uint all_cams = 0;
        for (var i = 0; i < num_cams; i++)
        {
            rgb_textures[i]    = new Texture2D(640, 480, TextureFormat.RGB24, false);
            rgb_resources[i]   = rgb_textures[i].GetNativeTexturePtr();

            depth_tensors[i]   = new Tensor<float>(depth_shape);
            depth_resources[i] = ComputeTensorData.Pin(depth_tensors[i]).buffer.GetNativeBufferPtr();

            // Register textures with the Spot observer
            if (!SOb_RegisterUnityReadbackBuffers(robot_id, cams[i], rgb_resources[i], depth_resources[i], 640 * 480 * 4, 640 * 480 * 4))
            {
                Debug.LogError("Failed to register textures for camera " + cams[i]);
            }

            all_cams |= cams[i];
        }

        // Kick off reading camera feeds
        if (!SOb_ReadCameraFeeds(robot_id, all_cams))
        {
            Debug.LogError("Failed to read camera feeds from Spot robot.");
            return;
        }
        isStreaming = true;
        Debug.Log("Successfully started streaming camera feeds from Spot robot.");

        model = SOb_LoadModel(depthCompletionModelFile, "cuda");
        if (model == IntPtr.Zero)
        {
            Debug.LogError("Failed to load vision pipeline model.");
            return;
        }

        if (!isConnected)
        {
            SOb_DisconnectFromSpot(robot_id);
            robot_id = -1;
            return;
        }

        launch_vision_pipeline();


        //if (!SOb_PushNextImageSetToUnityBuffers(robot_id))
        //{
        //    //Debug.LogError("Failed to read back image set to Unity buffers.");
        //    return;
        //}
        Debug.Log("Successfully registered and read camera feeds from Spot robot.");
    }

    void OnDestroy()
    {
        Debug.Log("Disconnecting from Spot robot " + robot_id + ". isConnected = " + isConnected);
        
        stop_vision_pipeline();

        if (isConnected)
        {
            SOb_DisconnectFromSpot(robot_id);
            Debug.Log("Disconnected from Spot robot.");
        }

        if (depth_tensors != null) {
            foreach (var tensor in depth_tensors)
            {
                tensor.Dispose();
            }
        }
        if (rgb_textures != null) {
            foreach (var texture in rgb_textures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        // TODO: Destroy the model
        
        SpotCamToIdx.Clear();
    }

    void Update()
    {
        if (robot_id < 0 || !isStreaming)
        {
            return;
        }

        if (useVisionPipeline)
        {
            if (!isVisionPipelineRunning)
            {
                launch_vision_pipeline();
            }
            SOb_PushNextVisionPipelineImageSetToUnityBuffers(robot_id);
        } else {
            if (isVisionPipelineRunning)
            {
                stop_vision_pipeline();
            }
            SOb_PushNextImageSetToUnityBuffers(robot_id);
        }
    }

    public (Texture2D, Tensor) GetCameraFeeds(SpotCamera id)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to Spot robot. Cannot get camera feeds. (Robot ID " + robot_id + ")");
            return (null, null);
        }
        if (!isStreaming) {
            Debug.LogError("Not streaming camera feeds for robot " + robot_id);
            return (null, null);
        }

        if (!SpotCamToIdx.TryGetValue((int)id, out int idx))
        {
            Debug.LogError("Invalid camera ID: " + (int)id);
            return (null, null);
        }

        return (rgb_textures[idx], depth_tensors[idx]);
    }
}
