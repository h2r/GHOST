using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Ghost.SpectatorStream
{
    /// <summary>
    /// Renders a camera offscreen at a fixed rate and pipes raw RGBA frames
    /// into an ffmpeg child process, which encodes H.264 and pushes RTSP to
    /// MediaMTX (config and scripts in stream/ at the repo root). Operator
    /// consoles then pull the stream over WebRTC.
    ///
    /// The encoder invocation here is mirrored by stream/tools/test_stream.sh,
    /// which fakes this component with an ffmpeg test pattern — keep the two
    /// in sync, and use the script to isolate which side of the pipe is
    /// misbehaving.
    /// </summary>
    public class SpectatorStreamer : MonoBehaviour
    {
        public enum H264Encoder { Nvenc, X264, OpenH264 }

        [Header("Capture")]
        public Camera captureCamera;
        public int width = 1280;
        public int height = 720;
        public int framerate = 30;
        [Tooltip("GPU readback row order differs per graphics API; toggle if the stream is upside down.")]
        public bool flipVertically = true;

        [Header("Encoding")]
        public string ffmpegPath = "ffmpeg";
        public H264Encoder encoder = H264Encoder.Nvenc;
        public string bitrate = "6M";
        public string rtspUrl = "rtsp://127.0.0.1:8554/scene";

        // Frames wait here between the readback callback (main thread) and
        // the writer thread; small and lossy by design — when encoding falls
        // behind we drop frames rather than grow latency.
        private BlockingCollection<byte[]> _pending;
        private ConcurrentBag<byte[]> _bufferPool;
        private RenderTexture _renderTexture;
        private Process _ffmpeg;
        private Thread _writerThread;
        private float _nextCaptureTime;
        private int _droppedFrames;
        private int _sentFrames;
        private bool _pipeBroken;

        private void Start()
        {
            if (captureCamera == null)
            {
                Debug.LogError("[Spectator] no capture camera assigned");
                enabled = false;
                return;
            }

            _renderTexture = new RenderTexture(width, height, 24);
            captureCamera.targetTexture = _renderTexture;
            captureCamera.enabled = false; // rendered manually at stream rate

            _pending = new BlockingCollection<byte[]>(3);
            _bufferPool = new ConcurrentBag<byte[]>();

            if (!StartFfmpeg())
            {
                enabled = false;
                return;
            }

            _writerThread = new Thread(WriterLoop) { IsBackground = true, Name = "SpectatorStreamWriter" };
            _writerThread.Start();
            _nextCaptureTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (_pipeBroken)
            {
                Debug.LogError("[Spectator] ffmpeg pipe closed — stopping stream");
                enabled = false;
                return;
            }

            if (Time.unscaledTime < _nextCaptureTime) return;
            _nextCaptureTime += 1f / framerate;
            // If we fell far behind (hitch, breakpoint), resynchronize
            // instead of bursting captures.
            if (Time.unscaledTime > _nextCaptureTime + 1f)
            {
                _nextCaptureTime = Time.unscaledTime;
            }

            captureCamera.Render();
            AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGBA32, OnReadback);
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            if (request.hasError || _pending == null || _pending.IsAddingCompleted) return;

            var data = request.GetData<byte>();
            if (!_bufferPool.TryTake(out var buffer) || buffer.Length != data.Length)
            {
                buffer = new byte[data.Length];
            }
            data.CopyTo(buffer);

            if (!_pending.TryAdd(buffer))
            {
                _droppedFrames++;
                _bufferPool.Add(buffer);
            }
            else
            {
                _sentFrames++;
                if (_sentFrames % (framerate * 30) == 0 && _droppedFrames > 0)
                {
                    Debug.LogWarning($"[Spectator] {_droppedFrames} frames dropped so far (encoder falling behind)");
                }
            }
        }

        private void WriterLoop()
        {
            try
            {
                Stream stdin = _ffmpeg.StandardInput.BaseStream;
                foreach (var frame in _pending.GetConsumingEnumerable())
                {
                    stdin.Write(frame, 0, frame.Length);
                    _bufferPool.Add(frame);
                }
                stdin.Flush();
            }
            catch (IOException)
            {
                _pipeBroken = true;
            }
            catch (System.ObjectDisposedException)
            {
                // shutdown race; nothing to do
            }
        }

        private bool StartFfmpeg()
        {
            string codec = encoder switch
            {
                // Keep in sync with stream/tools/test_stream.sh.
                H264Encoder.Nvenc => "-c:v h264_nvenc -preset p1 -tune ull -bf 0 -delay 0",
                H264Encoder.X264 => "-c:v libx264 -preset ultrafast -tune zerolatency -bf 0",
                H264Encoder.OpenH264 => "-c:v libopenh264 -allow_skip_frames 1",
                _ => "-c:v libx264 -preset ultrafast -tune zerolatency -bf 0",
            };
            string filter = flipVertically ? "-vf vflip " : "";
            string args =
                $"-hide_banner -f rawvideo -pix_fmt rgba -video_size {width}x{height} " +
                $"-framerate {framerate} -i - {filter}{codec} -g {framerate * 2} " +
                $"-b:v {bitrate} -pix_fmt yuv420p -f rtsp -rtsp_transport tcp {rtspUrl}";

            try
            {
                _ffmpeg = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                    },
                };
                _ffmpeg.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Debug.Log($"[ffmpeg] {e.Data}");
                };
                _ffmpeg.Start();
                _ffmpeg.BeginErrorReadLine();
                Debug.Log($"[Spectator] ffmpeg started: {ffmpegPath} {args}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Spectator] failed to start ffmpeg ('{ffmpegPath}'): {e.Message}");
                return false;
            }
        }

        private void OnDestroy()
        {
            _pending?.CompleteAdding();
            _writerThread?.Join(2000);
            try
            {
                if (_ffmpeg != null && !_ffmpeg.HasExited)
                {
                    _ffmpeg.StandardInput.Close();
                    if (!_ffmpeg.WaitForExit(2000)) _ffmpeg.Kill();
                }
            }
            catch (System.Exception)
            {
                // process already gone
            }
            if (_renderTexture != null)
            {
                if (captureCamera != null) captureCamera.targetTexture = null;
                _renderTexture.Release();
            }
        }
    }
}
