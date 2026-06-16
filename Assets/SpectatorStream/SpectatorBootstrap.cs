using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Ghost.SpectatorStream
{
    /// <summary>
    /// Spectator launch mode: when the player starts with -spectator (or the
    /// GHOST_SPECTATOR=1 env var, usable in the editor), XR is shut down and
    /// a fixed camera is spawned that streams the scene over RTSP via
    /// <see cref="SpectatorStreamer"/>. Requires no scene changes; without
    /// the flag the normal VR path is completely untouched.
    ///
    /// Optional command-line overrides:
    ///   -spectator-pose x,y,z,pitch,yaw    camera pose (default 0,2.5,-3.5,25,0)
    ///   -spectator-rtsp rtsp://host:8554/scene
    ///   -spectator-encoder nvenc|x264|openh264
    ///   -spectator-size 1280x720
    ///   -spectator-fps 30
    ///   -spectator-ffmpeg C:\path\to\ffmpeg.exe
    /// </summary>
    public static class SpectatorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!IsEnabled()) return;
            Debug.Log("[Spectator] spectator mode — shutting down XR and starting scene stream");
            ShutDownXr();
            CreateRig();
        }

        private static bool IsEnabled()
        {
            if (Environment.GetEnvironmentVariable("GHOST_SPECTATOR") == "1") return true;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-spectator") return true;
            }
            return false;
        }

        private static void ShutDownXr()
        {
            try
            {
                var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
                if (manager != null && manager.activeLoader != null)
                {
                    manager.StopSubsystems();
                    manager.DeinitializeLoader();
                    Debug.Log("[Spectator] XR loader deinitialized");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Spectator] XR shutdown failed (continuing): {e.Message}");
            }
        }

        private static void CreateRig()
        {
            var root = new GameObject("SpectatorStream");
            UnityEngine.Object.DontDestroyOnLoad(root);

            var cameraObject = new GameObject("SpectatorCamera");
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.stereoTargetEye = StereoTargetEyeMask.None;

            // The rig tracks the robots (above + behind, looking down/forward)
            // so the view stays on them wherever localization places them.
            // -spectator-pose seeds the fallback pose used before robots exist.
            float[] pose = ParsePose(GetArg("-spectator-pose"), new float[] { 0f, 3f, -4.5f, 28f, 0f });
            cameraObject.transform.position = new Vector3(pose[0], pose[1], pose[2]);
            cameraObject.transform.rotation = Quaternion.Euler(pose[3], pose[4], 0f);
            var rig = cameraObject.AddComponent<SpectatorCameraRig>();
            rig.fallbackPosition = new Vector3(pose[0], pose[1], pose[2]);
            rig.fallbackEuler = new Vector3(pose[3], pose[4], 0f);

            var streamer = root.AddComponent<SpectatorStreamer>();
            streamer.captureCamera = camera;

            // RTSP push target: -spectator-rtsp arg, else GHOST_SPECTATOR_RTSP
            // env var (usable from the editor), else the streamer default. Point
            // this at the server in the server-centric setup, e.g.
            // rtsp://128.148.138.132:8554/scene.
            string rtsp = GetArg("-spectator-rtsp")
                ?? Environment.GetEnvironmentVariable("GHOST_SPECTATOR_RTSP");
            if (!string.IsNullOrEmpty(rtsp)) streamer.rtspUrl = rtsp;

            string ffmpeg = GetArg("-spectator-ffmpeg");
            if (ffmpeg != null) streamer.ffmpegPath = ffmpeg;

            switch (GetArg("-spectator-encoder"))
            {
                case "nvenc": streamer.encoder = SpectatorStreamer.H264Encoder.Nvenc; break;
                case "x264": streamer.encoder = SpectatorStreamer.H264Encoder.X264; break;
                case "openh264": streamer.encoder = SpectatorStreamer.H264Encoder.OpenH264; break;
            }

            string size = GetArg("-spectator-size");
            if (size != null)
            {
                string[] parts = size.Split('x');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out int w)
                    && int.TryParse(parts[1], out int h))
                {
                    streamer.width = w;
                    streamer.height = h;
                }
            }

            string fps = GetArg("-spectator-fps");
            if (fps != null && int.TryParse(fps, out int f)) streamer.framerate = f;
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }

        private static float[] ParsePose(string value, float[] fallback)
        {
            if (value == null) return fallback;
            string[] parts = value.Split(',');
            if (parts.Length != fallback.Length) return fallback;
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                {
                    return fallback;
                }
            }
            return result;
        }
    }
}
