#!/usr/bin/env bash
# Stand-in for the Unity producer: pipes raw RGBA frames (ffmpeg test
# pattern) into the same encoder invocation SpectatorStreamer.cs builds,
# pushing RTSP to MediaMTX. Use it to verify the server + console player
# without Unity, or to isolate which side of the seam is misbehaving.
#
# Usage: ./test_stream.sh [nvenc|x264|openh264] [WxH] [fps] [rtsp-url]
set -euo pipefail

encoder="${1:-openh264}"
size="${2:-1280x720}"
fps="${3:-30}"
url="${4:-rtsp://127.0.0.1:8554/scene}"

# Keep these in sync with SpectatorStreamer.EncoderArgs.
case "$encoder" in
  nvenc) codec="-c:v h264_nvenc -preset p1 -tune ull -bf 0 -delay 0" ;;
  x264) codec="-c:v libx264 -preset ultrafast -tune zerolatency -bf 0" ;;
  openh264) codec="-c:v libopenh264 -allow_skip_frames 1" ;;
  *) echo "unknown encoder: $encoder" >&2; exit 1 ;;
esac

ffmpeg -hide_banner -f lavfi -i "testsrc2=size=${size}:rate=${fps}" \
  -f rawvideo -pix_fmt rgba - |
ffmpeg -hide_banner \
  -f rawvideo -pix_fmt rgba -video_size "$size" -framerate "$fps" -i - \
  $codec -g $((fps * 2)) -b:v 6M -pix_fmt yuv420p \
  -f rtsp -rtsp_transport tcp "$url"
