/*
© Siemens AG, 2018-2019
Original Author: Berkay Alp Cakal (berkay_alp.cakal.ct@siemens.com)
Adapted to PointCloud2 2025: Claude Code And Yichen Wei (yichen_wei@brown.edu)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace RosSharp.RosBridgeClient
{
    public class PointCloud2Writer : MonoBehaviour
    {
        private bool isReceived = false;
        private readonly object dataLock = new object();
        // ROS callbacks fill write* buffers; Update publishes ready* while holding the lock
        // so visualizers can copy before the writer reuses either array.
        private Vector3[] writePoints;
        private Color[] writeColors;
        private Vector3[] readyPoints;
        private Color[] readyColors;
        private int readyPointCount;
        private PointCloud2Visualizer[] pointCloud2Visualizers;

        // Reinterprets endian-correct integer bits without allocating byte arrays per field.
        [StructLayout(LayoutKind.Explicit)]
        private struct UIntFloat
        {
            [FieldOffset(0)] public uint UInt;
            [FieldOffset(0)] public float Float;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ULongDouble
        {
            [FieldOffset(0)] public ulong ULong;
            [FieldOffset(0)] public double Double;
        }

        private void Awake()
        {
            pointCloud2Visualizers = GetComponents<PointCloud2Visualizer>();
        }

        private void Update()
        {
            if (pointCloud2Visualizers == null)
                pointCloud2Visualizers = GetComponents<PointCloud2Visualizer>();

            if (pointCloud2Visualizers == null)
                return;

            lock (dataLock)
            {
                if (!isReceived)
                    return;

                foreach (PointCloud2Visualizer pointCloud2Visualizer in pointCloud2Visualizers)
                {
                    if (pointCloud2Visualizer != null)
                        pointCloud2Visualizer.SetPointCloudData(gameObject.transform, readyPoints, readyColors, readyPointCount);
                }
                isReceived = false;
            }
        }

        public void Write(MessageTypes.Sensor.PointCloud2 pointCloud2)
        {
            if (pointCloud2 == null || pointCloud2.data == null || pointCloud2.fields == null)
                return;

            long numPointsLong = (long)pointCloud2.width * (long)pointCloud2.height;
            if (numPointsLong <= 0 || numPointsLong > int.MaxValue || pointCloud2.point_step == 0)
                return;

            int numPoints = (int)numPointsLong;

            // Find field offsets for x, y, z, and rgb/rgba
            int x_offset = -1, y_offset = -1, z_offset = -1, rgb_offset = -1;
            byte x_datatype = 0, y_datatype = 0, z_datatype = 0, rgb_datatype = 0;

            foreach (var field in pointCloud2.fields)
            {
                if (field.name == "x")
                {
                    x_offset = (int)field.offset;
                    x_datatype = field.datatype;
                }
                else if (field.name == "y")
                {
                    y_offset = (int)field.offset;
                    y_datatype = field.datatype;
                }
                else if (field.name == "z")
                {
                    z_offset = (int)field.offset;
                    z_datatype = field.datatype;
                }
                else if (field.name == "rgb" || field.name == "rgba")
                {
                    rgb_offset = (int)field.offset;
                    rgb_datatype = field.datatype;
                }
            }

            // Parse point cloud data
            int point_step = (int)pointCloud2.point_step;
            byte[] data = pointCloud2.data;

            lock (dataLock)
            {
                EnsureWriteCapacity(numPoints);

                for (int i = 0; i < numPoints; i++)
                {
                    int index = i * point_step;

                    float x = 0, y = 0, z = 0;
                    if (x_offset >= 0)
                        x = ReadFloat(data, index + x_offset, x_datatype, pointCloud2.is_bigendian);
                    if (y_offset >= 0)
                        y = ReadFloat(data, index + y_offset, y_datatype, pointCloud2.is_bigendian);
                    if (z_offset >= 0)
                        z = ReadFloat(data, index + z_offset, z_datatype, pointCloud2.is_bigendian);

                    writePoints[i] = new Vector3(x, y, z).Ros2Unity();

                    if (rgb_offset >= 0)
                    {
                        uint rgb = ReadUInt(data, index + rgb_offset, rgb_datatype, pointCloud2.is_bigendian);
                        writeColors[i] = UnpackRGB(rgb);
                    }
                    else
                    {
                        writeColors[i] = Color.white;
                    }
                }

                // Swap buffers instead of allocating per message.
                Vector3[] oldReadyPoints = readyPoints;
                Color[] oldReadyColors = readyColors;
                readyPoints = writePoints;
                readyColors = writeColors;
                readyPointCount = numPoints;
                writePoints = oldReadyPoints;
                writeColors = oldReadyColors;
                isReceived = true;
            }
        }

        private void EnsureWriteCapacity(int pointCount)
        {
            if (writePoints == null || writePoints.Length < pointCount)
                writePoints = new Vector3[pointCount];
            if (writeColors == null || writeColors.Length < pointCount)
                writeColors = new Color[pointCount];
        }

        private float ReadFloat(byte[] data, int offset, byte datatype, bool isBigEndian)
        {
            // FLOAT32 = 7
            if (datatype == 7)
            {
                if (offset + 4 > data.Length)
                    return 0f;

                UIntFloat value = new UIntFloat { UInt = ReadUInt32(data, offset, isBigEndian) };
                return value.Float;
            }
            // FLOAT64 = 8
            else if (datatype == 8)
            {
                if (offset + 8 > data.Length)
                    return 0f;

                ULongDouble value = new ULongDouble { ULong = ReadUInt64(data, offset, isBigEndian) };
                return (float)value.Double;
            }

            return 0f;
        }

        private uint ReadUInt(byte[] data, int offset, byte datatype, bool isBigEndian)
        {
            // UINT32 = 6, FLOAT32 = 7 (for RGB packed as float)
            if (datatype == 6 || datatype == 7)
                return ReadUInt32(data, offset, isBigEndian);

            return 0;
        }

        private uint ReadUInt32(byte[] data, int offset, bool isBigEndian)
        {
            if (offset + 4 > data.Length)
                return 0;

            if (isBigEndian)
            {
                return ((uint)data[offset] << 24) |
                       ((uint)data[offset + 1] << 16) |
                       ((uint)data[offset + 2] << 8) |
                       (uint)data[offset + 3];
            }

            return (uint)data[offset] |
                   ((uint)data[offset + 1] << 8) |
                   ((uint)data[offset + 2] << 16) |
                   ((uint)data[offset + 3] << 24);
        }

        private ulong ReadUInt64(byte[] data, int offset, bool isBigEndian)
        {
            if (offset + 8 > data.Length)
                return 0;

            if (isBigEndian)
            {
                return ((ulong)data[offset] << 56) |
                       ((ulong)data[offset + 1] << 48) |
                       ((ulong)data[offset + 2] << 40) |
                       ((ulong)data[offset + 3] << 32) |
                       ((ulong)data[offset + 4] << 24) |
                       ((ulong)data[offset + 5] << 16) |
                       ((ulong)data[offset + 6] << 8) |
                       (ulong)data[offset + 7];
            }

            return (ulong)data[offset] |
                   ((ulong)data[offset + 1] << 8) |
                   ((ulong)data[offset + 2] << 16) |
                   ((ulong)data[offset + 3] << 24) |
                   ((ulong)data[offset + 4] << 32) |
                   ((ulong)data[offset + 5] << 40) |
                   ((ulong)data[offset + 6] << 48) |
                   ((ulong)data[offset + 7] << 56);
        }

        private Color UnpackRGB(uint rgb)
        {
            byte r = (byte)((rgb >> 16) & 0xFF);
            byte g = (byte)((rgb >> 8) & 0xFF);
            byte b = (byte)(rgb & 0xFF);

            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
