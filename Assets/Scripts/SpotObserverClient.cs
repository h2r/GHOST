using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.RclInterfaces;
using RosSharp.Urdf;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.InferenceEngine;

using UnityEngine;
using UnityEngine.EventSystems;


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
    private static extern int SOb_CreateCameraStream(
        int robot_id,
        uint camera_mask
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_DestroyCameraStream(
        int robot_id,
        int stream_id
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_RegisterUnityReadbackBuffers(
        int robot_id,
        int stream_id,
        uint cam_bit,
        IntPtr rgb_resource,
        IntPtr depth_resource,
        int img_buffer_size, // In bytes
        int depth_buffer_size // In bytes
    );

    [DllImport("SpotObserverLib")]
    private static extern void SOb_ClearUnityReadbackBuffers(int robot_id);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_PushNextImageSetToUnityBuffers(
        int robot_id,
        int stream_id
    );

    [DllImport("SpotObserverLib", CharSet = CharSet.Ansi)]
    private static extern IntPtr SOb_LoadModel(string modelPath, string backend);

    [DllImport("SpotObserverLib")]
    private static extern void SOb_UnloadModel(IntPtr model);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_LaunchVisionPipeline(int robot_id, int stream_id, IntPtr model);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_StopVisionPipeline(int robot_id, int stream_id);

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_PushNextVisionPipelineImageSetToUnityBuffers(
        int robot_id,
        int stream_id
    );

    [DllImport("SpotObserverLib")]
    private static extern bool SOb_SetUnityLogCallback(LogCallback callback);
    [DllImport("SpotObserverLib")]
    private static extern void SOb_ToggleLogging(bool enable_logging);

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

    public string SpotPrefix;
    public string RobotIP = "";
    public string username;
    public string password;
    private bool start_called = false;

    public GameObject rosConnector;

    public bool[] useVisionPipeline = new bool[2] { false, false };
    public string depthCompletionModelFile;

    public bool enableLogging = false;
    private bool lastLoggingState = false;
    public bool enableDebugDumps = false;

    // Private

    private int robot_id = -1;
    private bool isConnected = false;

    private int[] stream_ids = new int[2] { -1, -1 };
    private bool[] isStreaming = new bool[2] { false, false };
    private bool[] isVisionPipelineRunning = new bool[2] { false, false };
    private IntPtr model;

    private Texture2D[][] rgb_textures;

    //private Tensor<byte>[][] rgb_tensors;
    private Tensor<float>[][] depth_tensors;

    private IntPtr[][] rgb_resources;
    private IntPtr[][] depth_resources;

    private NativeHashMap<int, int>[] SpotCamToIdx;

    private uint[][] cams_per_stream = {
        new uint[] {
            (uint)SpotCamera.FRONTLEFT,
            (uint)SpotCamera.FRONTRIGHT
        },
        new uint[] {
            (uint)SpotCamera.HAND
        }
    };


    private void launch_vision_pipeline(int stream_idx)
    {
        if (model == IntPtr.Zero)
        {
            Debug.LogError("No model loaded for vision pipeline.");
            return;
        }
        int stream_id = stream_ids[stream_idx];
        if (stream_id < 0)
        {
            Debug.LogError("No valid stream ID for stream index " + stream_idx);
            return;
        }
        bool ret = SOb_LaunchVisionPipeline(robot_id, stream_id, model);
        if (!ret)
        {
            Debug.LogError("Failed to launch vision pipeline on Spot robot.");
            return;
        }
        isVisionPipelineRunning[stream_idx] = true;
    }

    private void stop_vision_pipeline(int stream_idx)
    {
        int stream_id = stream_ids[stream_idx];
        if (stream_id < 0)
        {
            Debug.LogError("No valid stream ID for stream index " + stream_idx);
            return;
        }
        bool ret = SOb_StopVisionPipeline(robot_id, stream_id);
        if (!ret)
        {
            Debug.LogError("Failed to stop vision pipeline on Spot robot.");
            return;
        }
        isVisionPipelineRunning[stream_idx] = false;
    }

    private void UpdateHostName()
    {
        string service_topic = SpotPrefix + "/spot_ros2/get_parameters";
        string[] names = new string[] { "hostname" };
        GetParametersRequest request_params = new GetParametersRequest(names);
        rosConnector.GetComponent<RosConnector>().RosSocket.CallService<GetParametersRequest, GetParametersResponse>(
            service_topic,
            response =>
            {
                RobotIP = response.values[0].string_value;
                Debug.Log("Got " + SpotPrefix + " Hostname: " + RobotIP);
            },
            request_params
        );
    }

    private void _start()
    {
        start_called = true;

        SOb_SetUnityLogCallback(PluginLogCallback);

        if (enableLogging)
        {
            SOb_ToggleLogging(true);
        }
        else
        {
            SOb_ToggleLogging(false);
        }
        lastLoggingState = enableLogging;
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

        // Load the vision pipeline model        
        model = SOb_LoadModel(depthCompletionModelFile, "cuda");
        if (model == IntPtr.Zero)
        {
            Debug.LogError("Failed to load vision pipeline model.");
            return;
        }


        // Initialize camera resources
        // TODO: Unhardcode shapes
        TensorShape rgb_shape = new TensorShape(1, 3, 480, 640); // RGB shape
        TensorShape depth_shape = new TensorShape(1, 1, 480, 640); // Depth shape

        int num_streams = cams_per_stream.Length;

        // Initialize textures and tensors for each camera
        rgb_textures = new Texture2D[num_streams][];
        rgb_resources = new IntPtr[num_streams][];
        depth_resources = new IntPtr[num_streams][];
        depth_tensors = new Tensor<float>[num_streams][];

        // Create camera streams
        SpotCamToIdx = new NativeHashMap<int, int>[num_streams];
        for (int stream = 0; stream < num_streams; stream++)
        {
            int num_cams = cams_per_stream[stream].Length;
            uint[] cams = cams_per_stream[stream];

            // Initialize textures and tensors for each camera
            rgb_textures[stream] = new Texture2D[num_cams];
            rgb_resources[stream] = new IntPtr[num_cams];
            depth_resources[stream] = new IntPtr[num_cams];
            depth_tensors[stream] = new Tensor<float>[num_cams];

            SpotCamToIdx[stream] = new NativeHashMap<int, int>(cams.Length, Allocator.Persistent);

            uint all_cams = 0;
            for (int i = 0; i < cams.Length; i++)
            {
                SpotCamToIdx[stream].Add((int)cams[i], i);
                all_cams |= cams[i];
            }

            // Kick off reading camera feeds
            stream_ids[stream] = SOb_CreateCameraStream(robot_id, all_cams);
            int stream_id = stream_ids[stream];
            if (stream_id < 0)
            {
                Debug.LogError("Failed to read camera feeds from Spot robot.");
            }
            Debug.Log("Created camera stream with ID " + stream_id + " for cameras mask " + all_cams);

            isStreaming[stream] = true;

            for (var i = 0; i < num_cams; i++)
            {
                rgb_textures[stream][i] = new Texture2D(640, 480, TextureFormat.RGB24, false);
                rgb_resources[stream][i] = rgb_textures[stream][i].GetNativeTexturePtr();

                depth_tensors[stream][i] = new Tensor<float>(depth_shape);
                depth_resources[stream][i] = ComputeTensorData.Pin(depth_tensors[stream][i]).buffer.GetNativeBufferPtr();

                // Register textures with the Spot observer
                // Don't trip: RGB24 is 4 bytes per pixel despite what you'd think
                if (!SOb_RegisterUnityReadbackBuffers(robot_id, stream_id, cams[i], rgb_resources[stream][i], depth_resources[stream][i], 640 * 480 * 4, 640 * 480 * 4))
                {
                    Debug.LogError("Failed to register textures for camera " + cams[i]);
                }

            }

            Debug.Log("Successfully started streaming camera feeds from Spot robot.");

            if (!isConnected)
            {
                SOb_DisconnectFromSpot(robot_id);
                robot_id = -1;
                return;
            }

            if (useVisionPipeline[stream])
            {
                launch_vision_pipeline(stream);
            }

            //if (!SOb_PushNextImageSetToUnityBuffers(robot_id))
            //{
            //    //Debug.LogError("Failed to read back image set to Unity buffers.");
            //    return;
            //}
            Debug.Log("Successfully registered and read camera feeds from Spot robot.");

        }
    }

    void Start()
    {
        // If RobotIP is empty, we need to fetch it from ROS
        if (RobotIP == "")
        {
            UpdateHostName();
            if (RobotIP == "")
            {
                Debug.LogError("Could not retrieve RobotIP from ROS. Will retry later.");
                return;
            }
        }
        _start();
    }

    void OnDestroy()
    {
        Debug.Log("Disconnecting from Spot robot " + robot_id);
        bool ret = true;

        for (int stream = 0; stream < stream_ids.Length; stream++)
        {
            if (isVisionPipelineRunning[stream])
            {
                stop_vision_pipeline(stream);
            }

            if (isStreaming[stream] && stream_ids[stream] >= 0)
            {
                ret = SOb_DestroyCameraStream(robot_id, stream_ids[stream]);
                if (!ret)
                {
                    Debug.LogError("Failed to destroy camera stream " + stream_ids[stream] + " for Spot robot " + robot_id + ".");
                }
                else
                {
                    Debug.Log("Successfully destroyed camera stream " + stream_ids[stream] + " for Spot robot " + robot_id + ".");
                }
                isStreaming[stream] = false;
            }
        }

        if (robot_id >= 0)
        {
            SOb_ClearUnityReadbackBuffers(robot_id);
        }
        if (isConnected)
        {
            ret = SOb_DisconnectFromSpot(robot_id);
            if (!ret)
            {
                Debug.LogError("Failed to disconnect from Spot robot " + robot_id + ".");
            }
            else
            {
                Debug.Log("Successfully disconnected from Spot robot " + robot_id + ".");
            }
        }
        robot_id = -1;
        isConnected = false;

        // TODO: Destroy the model
        for (int stream = 0; stream < SpotCamToIdx.Length; stream++)
        {
            if (SpotCamToIdx[stream].IsCreated)
            {
                SpotCamToIdx[stream].Dispose();
            }
            if (depth_tensors[stream] != null)
            {
                foreach (var tensor in depth_tensors[stream])
                {
                    tensor.Dispose();
                }
            }
            if (rgb_textures[stream] != null)
            {
                foreach (var texture in rgb_textures[stream])
                {
                    if (texture != null)
                    {
                        Destroy(texture);
                    }
                }
            }
        }

    
        start_called = false;
    }

    void Update()
    {
        Debug.Log("SpotObserverClient Update() called. Robot ID: " + robot_id + ", start_called: " + start_called + ", RobotIP: " + RobotIP);
        if (!start_called)
        {
            if (RobotIP != "")
            {
                _start();
            }
            else
            {
                Debug.LogError("SpotObserverClient not initialized and RobotIP is empty. Cannot start.");
                return;
            }

        }
        
        if (lastLoggingState != enableLogging)
        {
            if (enableLogging)
            {
                SOb_ToggleLogging(true);
            }
            else
            {
                SOb_ToggleLogging(false);
            }
            lastLoggingState = enableLogging;
        }

        for (int stream = 0; stream < stream_ids.Length; stream++)
        {
            if (robot_id < 0 || !isStreaming[stream])
            {
                continue;
            }

            if (useVisionPipeline[stream])
            {
                if (!isVisionPipelineRunning[stream])
                {
                    launch_vision_pipeline(stream);
                    if (!isVisionPipelineRunning[stream])
                    {
                        // Failed to launch vision pipeline
                        SOb_PushNextImageSetToUnityBuffers(robot_id, stream_ids[stream]);
                        continue;
                    }
                }
                else
                {
                    SOb_PushNextVisionPipelineImageSetToUnityBuffers(robot_id, stream_ids[stream]);
                }
            }
            else
            {
                if (isVisionPipelineRunning[stream])
                {
                    stop_vision_pipeline(stream);
                }
                SOb_PushNextImageSetToUnityBuffers(robot_id, stream_ids[stream]);
            }
        }
    }

    public (Texture2D, Tensor) GetCameraFeeds(int stream_idx, SpotCamera id)
    {
        if (stream_idx < 0 || stream_idx >= stream_ids.Length)
        {
            Debug.LogError("Invalid stream index: " + stream_idx);
            return (null, null);
        }

        if (!isConnected)
        {
            Debug.LogError("Not connected to Spot robot. Cannot get camera feeds. (Robot ID " + robot_id + ", stream " + stream_idx + ", camera " + (int)id + ")");
            return (null, null);
        }
        if (!isStreaming[stream_idx]) {
            Debug.LogError("Not streaming camera feeds for robot " + robot_id + ", stream " + stream_idx + ". Cannot get camera feeds.");
            return (null, null);
        }

        if (!SpotCamToIdx[stream_idx].TryGetValue((int)id, out int idx))
        {
            Debug.LogError("Invalid camera ID: " + (int)id);
            return (null, null);
        }

        return (rgb_textures[stream_idx][idx], depth_tensors[stream_idx][idx]);
    }
}
