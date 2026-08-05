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

    // kind: 0 = single-shot (PromptDA-style), 1 = streaming (KV-cache model).
    // The family cannot be inferred from the file -- both are .onnx.
    [DllImport("SpotObserverLib", CharSet = CharSet.Ansi)]
    private static extern IntPtr SOb_LoadModelEx(string modelPath, string backend, int kind);

    // Stops the vision pipeline on the stream (if running) and relaunches it on
    // the given preloaded model handle. The camera stream keeps running.
    [DllImport("SpotObserverLib")]
    private static extern bool SOb_SwitchVisionPipelineModel(int robot_id, int stream_id, IntPtr model);

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
    private static extern void SOb_SetDepthAveraging(int robot_id, int stream_id, bool enable_averaging);
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
    public string[] depthCompletionModelFiles;
    // Parallel to depthCompletionModelFiles: 0 = single-shot, 1 = streaming
    // (KV-cache). Missing/short array defaults to single-shot.
    public int[] depthCompletionModelKinds;
    public int currentDepthModelIndex = 0;
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
    // One preloaded handle per depthCompletionModelFiles entry. Loading happens
    // once at startup; cycling models is a handle switch with no load stall.
    private IntPtr[] modelHandles = null;

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
    // Don't trip: RGB24 is 4 bytes per pixel despite what you'd think
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

    private int GetDepthModelKind(int index)
    {
        if (depthCompletionModelKinds != null && index >= 0 && index < depthCompletionModelKinds.Length)
            return depthCompletionModelKinds[index];
        return 0; // single-shot
    }

    private bool EnsureVisionModelLoaded()
    {
        if (model != IntPtr.Zero)
            return true;

        // Preload every selectable model once, up front. Model switching later is
        // a handle swap (SOb_SwitchVisionPipelineModel) with no load in the hot
        // path -- so all the load cost lives here, at startup.
        if (depthCompletionModelFiles != null && depthCompletionModelFiles.Length > 0)
        {
            modelHandles = new IntPtr[depthCompletionModelFiles.Length];
            for (int i = 0; i < depthCompletionModelFiles.Length; i++)
            {
                string path = depthCompletionModelFiles[i] == null ? string.Empty : depthCompletionModelFiles[i].Trim();
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($"{username}: empty depth model path at index {i}");
                    return false;
                }
                modelHandles[i] = SOb_LoadModelEx(path, "cuda", GetDepthModelKind(i));
                if (modelHandles[i] == IntPtr.Zero)
                {
                    Debug.LogError($"{username}: failed to load depth model ({path}, kind {GetDepthModelKind(i)})");
                    return false;
                }
                Debug.Log($"{username}: loaded depth model {i}: {path} (kind {GetDepthModelKind(i)})");
            }

            currentDepthModelIndex = Mathf.Clamp(currentDepthModelIndex, 0, modelHandles.Length - 1);
            model = modelHandles[currentDepthModelIndex];
            return true;
        }

        // Legacy single-model path.
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
                int colorBytes = DefaultCameraWidth * DefaultCameraHeight * ColorRegistrationBytesPerPixel;
                int depthBytes = DefaultCameraWidth * DefaultCameraHeight * DepthBytesPerPixel;
                if (!SOb_RegisterUnityReadbackBuffers(robot_id, stream_id, cams[i], rgb_resources[stream][i], depth_resources[stream][i], colorBytes, depthBytes))
                {
                    Debug.LogError("Failed to register textures for camera " + cams[i]);
                }

            }

            if (!isConnected)
            {
                SOb_DisconnectFromSpot(robot_id);
                robot_id = -1;
                return;
            }
            
            Debug.Log("Successfully started streaming camera feeds from Spot robot " + RobotIP + " for stream index " + stream + ".");

            bool shouldUseVisionPipeline = useVisionPipeline != null && stream < useVisionPipeline.Length && useVisionPipeline[stream];
            if (shouldUseVisionPipeline)
            {
                launch_vision_pipeline(stream);
                Debug.Log("Started the vision pipeline for robot " + RobotIP + " for stream index " + stream + ".");
            }            
            SOb_SetDepthAveraging(robot_id, stream_id, true);

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

        if (modelHandles != null)
        {
            // Only streaming handles are unloaded: each streaming load is a fresh
            // per-client instance, so this is safe. Single-shot handles are shared
            // by path across clients on the native side -- unloading one here
            // would dangle another client's copy of the same pointer. Those stay
            // resident until process teardown.
            for (int i = 0; i < modelHandles.Length; i++)
            {
                if (modelHandles[i] != IntPtr.Zero && GetDepthModelKind(i) == 1)
                {
                    SOb_UnloadModel(modelHandles[i]);
                }
                modelHandles[i] = IntPtr.Zero;
            }
            modelHandles = null;
            model = IntPtr.Zero;
        }
        else if (model != IntPtr.Zero)
        {
            // Legacy single-model path: single-shot and possibly shared; leave
            // resident (matches pre-branch behavior where unload was disabled).
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

    public string GetCurrentDepthModelPath()
    {
        if (depthCompletionModelFiles != null && depthCompletionModelFiles.Length > 0)
        {
            currentDepthModelIndex = Mathf.Clamp(currentDepthModelIndex, 0, depthCompletionModelFiles.Length - 1);
            return depthCompletionModelFiles[currentDepthModelIndex];
        }
        return depthCompletionModelFile;
    }
    
    public string GetCurrentDepthModelName()
    {
        string path = GetCurrentDepthModelPath();
        if (string.IsNullOrEmpty(path))
            return "None";
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }

    public bool CycleDepthModel()
    {
        if (depthCompletionModelFiles == null || depthCompletionModelFiles.Length == 0)
            return false;

        // Clamp before incrementing: a negative inspector value would otherwise
        // survive the modulo and index out of range.
        currentDepthModelIndex = Mathf.Clamp(currentDepthModelIndex, 0, depthCompletionModelFiles.Length - 1);
        currentDepthModelIndex = (currentDepthModelIndex + 1) % depthCompletionModelFiles.Length;
        depthCompletionModelFile = depthCompletionModelFiles[currentDepthModelIndex];

        Debug.Log($"{username}: switched depth model selection to {depthCompletionModelFile}");

        return ReloadDepthModelSelection();
    }

    public bool ReloadDepthModelSelection()
    {
        if (modelHandles == null || modelHandles.Length == 0)
        {
            Debug.LogWarning($"{username}: no depth models preloaded; selection applies at next launch");
            return false;
        }

        currentDepthModelIndex = Mathf.Clamp(currentDepthModelIndex, 0, modelHandles.Length - 1);
        IntPtr next = modelHandles[currentDepthModelIndex];
        model = next; // any future launch uses the new selection

        // Not connected yet: updating the selection is the whole change.
        if (robot_id < 0)
            return true;

        // Live switch on every running vision pipeline: the native call stops the
        // pipeline, relaunches it on the preloaded handle, and leaves the camera
        // stream running. Sub-second, since nothing is loaded here.
        bool allOk = true;
        for (int s = 0; s < stream_ids.Length; s++)
        {
            if (s >= useVisionPipeline.Length || !useVisionPipeline[s]) continue;
            if (stream_ids[s] < 0 || !isVisionPipelineRunning[s]) continue;

            bool ok = SOb_SwitchVisionPipelineModel(robot_id, stream_ids[s], next);
            if (!ok)
                Debug.LogError($"{username}: depth model switch failed on stream {s} " +
                               "(model may not support this stream's camera count -- see native log)");
            allOk &= ok;
        }

        if (allOk)
            Debug.Log($"{username}: depth model switched to {GetCurrentDepthModelName()}");
        return allOk;
    }
}
