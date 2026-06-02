using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.RclInterfaces;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.InferenceEngine;

using UnityEngine;


public class SpotObserverClient : MonoBehaviour
{
    // Borrowed view of the latest camera frame. Consumers may read these resources
    // during the frame, but ownership stays with SpotObserverClient/the native plugin.
    public struct CameraDepthFrame
    {
        public readonly Texture2D ColorTexture;
        public readonly Tensor<float> DepthTensor;
        public readonly ComputeBuffer DepthBuffer;
        public readonly int Width;
        public readonly int Height;
        // Monotonic per camera while streaming; consumers use this to skip duplicate frames.
        public readonly ulong Sequence;

        public bool IsValid => ColorTexture != null && DepthTensor != null && DepthBuffer != null && Width > 0 && Height > 0;

        public CameraDepthFrame(Texture2D colorTexture, Tensor<float> depthTensor, ComputeBuffer depthBuffer, int width, int height, ulong sequence)
        {
            ColorTexture = colorTexture;
            DepthTensor = depthTensor;
            DepthBuffer = depthBuffer;
            Width = width;
            Height = height;
            Sequence = sequence;
        }
    }

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
    // Keep the delegate rooted while native code may call back into Unity.
    private static readonly LogCallback UnityLogCallback = PluginLogCallback;

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

    public bool[] useVisionPipeline = { false, false };
    public string depthCompletionModelFile;

    public bool enableLogging = false;
    private bool lastLoggingState = false;
    public bool enableDebugDumps = false;
    public bool enableFrameDebugLogging = false;

    // Private

    private int robot_id = -1;
    private bool isConnected = false;

    private int[] stream_ids = { -1, -1 };
    private bool[] isStreaming = { false, false };
    private bool[] isVisionPipelineRunning = { false, false };
    private IntPtr model = IntPtr.Zero;

    private Texture2D[][] rgb_textures;

    // Depth tensors own the pinned GPU buffers; renderers borrow depth_buffers via CameraDepthFrame.
    private Tensor<float>[][] depth_tensors;
    private ComputeBuffer[][] depth_buffers;
    // Sequence increments only after a successful native push, letting consumers skip duplicate frames.
    private ulong[][] frame_sequences;
    private bool[][] frame_valid;

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

    private const int DefaultCameraWidth = 640;
    private const int DefaultCameraHeight = 480;
    private const int ColorRegistrationBytesPerPixel = 4;
    private const int DepthBytesPerPixel = 4;

    private bool RequiresVisionPipeline()
    {
        if (useVisionPipeline == null)
            return false;

        for (int i = 0; i < useVisionPipeline.Length; i++)
        {
            if (useVisionPipeline[i])
                return true;
        }

        return false;
    }

    private bool EnsureVisionModelLoaded()
    {
        if (model != IntPtr.Zero)
            return true;

        string modelPath = depthCompletionModelFile == null ? string.Empty : depthCompletionModelFile.Trim();
        if (string.IsNullOrEmpty(modelPath))
        {
            Debug.LogError("No model path configured for the Spot vision pipeline.");
            return false;
        }

        model = SOb_LoadModel(modelPath, "cuda");
        if (model == IntPtr.Zero)
        {
            Debug.LogError("Failed to load vision pipeline model from: " + modelPath);
            return false;
        }

        return true;
    }

    private void MarkStreamFramePushed(int stream)
    {
        if (frame_sequences == null || frame_valid == null ||
            stream < 0 || stream >= frame_sequences.Length || stream >= frame_valid.Length ||
            frame_sequences[stream] == null || frame_valid[stream] == null)
        {
            return;
        }

        for (int i = 0; i < frame_sequences[stream].Length; i++)
        {
            frame_sequences[stream][i]++;
            frame_valid[stream][i] = true;
        }
    }

    private bool TryPushStreamFrame(int stream, bool usePipeline)
    {
        bool pushed;
        if (usePipeline)
        {
            pushed = SOb_PushNextVisionPipelineImageSetToUnityBuffers(robot_id, stream_ids[stream]);
        }
        else
        {
            pushed = SOb_PushNextImageSetToUnityBuffers(robot_id, stream_ids[stream]);
        }

        if (pushed)
        {
            MarkStreamFramePushed(stream);
        }
        else if (enableFrameDebugLogging)
        {
            Debug.LogWarning("No camera frame pushed for robot " + robot_id + ", stream " + stream + ".");
        }

        return pushed;
    }


    private void launch_vision_pipeline(int stream_idx)
    {
        if (!EnsureVisionModelLoaded())
            return;

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

        SOb_SetUnityLogCallback(UnityLogCallback);

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

        if (RequiresVisionPipeline() && !EnsureVisionModelLoaded())
        {
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
        depth_buffers = new ComputeBuffer[num_streams][];
        frame_sequences = new ulong[num_streams][];
        frame_valid = new bool[num_streams][];

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
            depth_buffers[stream] = new ComputeBuffer[num_cams];
            frame_sequences[stream] = new ulong[num_cams];
            frame_valid[stream] = new bool[num_cams];

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
                isStreaming[stream] = false;
                continue;
            }
            Debug.Log("Created camera stream with ID " + stream_id + " for cameras mask " + all_cams);

            isStreaming[stream] = true;

            for (var i = 0; i < num_cams; i++)
            {
                rgb_textures[stream][i] = new Texture2D(DefaultCameraWidth, DefaultCameraHeight, TextureFormat.RGB24, false);
                rgb_resources[stream][i] = rgb_textures[stream][i].GetNativeTexturePtr();

                depth_tensors[stream][i] = new Tensor<float>(depth_shape);
                depth_buffers[stream][i] = ComputeTensorData.Pin(depth_tensors[stream][i]).buffer;
                depth_resources[stream][i] = depth_buffers[stream][i].GetNativeBufferPtr();

                // Register textures with the Spot observer
                // Don't trip: RGB24 still needs 4 bytes per pixel for this native readback path.
                int colorBytes = DefaultCameraWidth * DefaultCameraHeight * ColorRegistrationBytesPerPixel;
                int depthBytes = DefaultCameraWidth * DefaultCameraHeight * DepthBytesPerPixel;
                if (!SOb_RegisterUnityReadbackBuffers(robot_id, stream_id, cams[i], rgb_resources[stream][i], depth_resources[stream][i], colorBytes, depthBytes))
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

            bool shouldUseVisionPipeline = useVisionPipeline != null && stream < useVisionPipeline.Length && useVisionPipeline[stream];
            if (shouldUseVisionPipeline)
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

        if (model != IntPtr.Zero)
        {
            SOb_UnloadModel(model);
            model = IntPtr.Zero;
        }

        if (SpotCamToIdx != null)
        {
            for (int stream = 0; stream < SpotCamToIdx.Length; stream++)
            {
                if (SpotCamToIdx[stream].IsCreated)
                {
                    SpotCamToIdx[stream].Dispose();
                }
            }
        }

        if (depth_tensors != null)
        {
            for (int stream = 0; stream < depth_tensors.Length; stream++)
            {
                if (depth_tensors[stream] == null)
                    continue;

                foreach (var tensor in depth_tensors[stream])
                {
                    tensor?.Dispose();
                }
            }
        }

        if (rgb_textures != null)
        {
            for (int stream = 0; stream < rgb_textures.Length; stream++)
            {
                if (rgb_textures[stream] == null)
                    continue;

                foreach (var texture in rgb_textures[stream])
                {
                    if (texture != null)
                    {
                        Destroy(texture);
                    }
                }
            }
        }

        rgb_textures = null;
        depth_tensors = null;
        depth_buffers = null;
        rgb_resources = null;
        depth_resources = null;
        frame_sequences = null;
        frame_valid = null;
        start_called = false;
    }

    void Update()
    {
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

            bool shouldUseVisionPipeline = useVisionPipeline != null && stream < useVisionPipeline.Length && useVisionPipeline[stream];
            if (shouldUseVisionPipeline)
            {
                if (!isVisionPipelineRunning[stream])
                {
                    launch_vision_pipeline(stream);
                    if (!isVisionPipelineRunning[stream])
                    {
                        // Failed to launch vision pipeline
                        TryPushStreamFrame(stream, false);
                        continue;
                    }
                }
                else
                {
                    TryPushStreamFrame(stream, true);
                }
            }
            else
            {
                if (isVisionPipelineRunning[stream])
                {
                    stop_vision_pipeline(stream);
                }
                TryPushStreamFrame(stream, false);
            }
        }
    }

    // Returns a borrowed frame for the requested camera. False means no fresh native frame
    // has been pushed yet, the stream is unavailable, or startup/teardown is incomplete.
    public bool TryGetCameraFrame(int stream_idx, SpotCamera id, out CameraDepthFrame frame)
    {
        frame = default;

        if (stream_idx < 0 || stream_idx >= stream_ids.Length)
        {
            Debug.LogError("Invalid stream index: " + stream_idx);
            return false;
        }

        if (!isConnected)
        {
            if (enableFrameDebugLogging)
                Debug.LogWarning("Not connected to Spot robot. Cannot get camera feeds. (Robot ID " + robot_id + ", stream " + stream_idx + ", camera " + (int)id + ")");
            return false;
        }
        if (!isStreaming[stream_idx]) {
            if (enableFrameDebugLogging)
                Debug.LogWarning("Not streaming camera feeds for robot " + robot_id + ", stream " + stream_idx + ". Cannot get camera feeds.");
            return false;
        }

        if (SpotCamToIdx == null || stream_idx >= SpotCamToIdx.Length || !SpotCamToIdx[stream_idx].IsCreated || !SpotCamToIdx[stream_idx].TryGetValue((int)id, out int idx))
        {
            Debug.LogError("Invalid camera ID: " + (int)id);
            return false;
        }

        if (rgb_textures == null || depth_tensors == null || depth_buffers == null || frame_sequences == null || frame_valid == null ||
            stream_idx >= rgb_textures.Length || stream_idx >= depth_tensors.Length || stream_idx >= depth_buffers.Length ||
            stream_idx >= frame_sequences.Length || stream_idx >= frame_valid.Length ||
            rgb_textures[stream_idx] == null || depth_tensors[stream_idx] == null || depth_buffers[stream_idx] == null ||
            frame_sequences[stream_idx] == null || frame_valid[stream_idx] == null ||
            idx < 0 || idx >= rgb_textures[stream_idx].Length || idx >= depth_tensors[stream_idx].Length ||
            idx >= depth_buffers[stream_idx].Length || idx >= frame_sequences[stream_idx].Length || idx >= frame_valid[stream_idx].Length)
        {
            return false;
        }

        if (!frame_valid[stream_idx][idx])
        {
            return false;
        }

        frame = new CameraDepthFrame(
            rgb_textures[stream_idx][idx],
            depth_tensors[stream_idx][idx],
            depth_buffers[stream_idx][idx],
            DefaultCameraWidth,
            DefaultCameraHeight,
            frame_sequences[stream_idx][idx]);

        return frame.IsValid;
    }

    public (Texture2D, Tensor) GetCameraFeeds(int stream_idx, SpotCamera id)
    {
        if (TryGetCameraFrame(stream_idx, id, out CameraDepthFrame frame))
            return (frame.ColorTexture, frame.DepthTensor);

        return (null, null);
    }
}
