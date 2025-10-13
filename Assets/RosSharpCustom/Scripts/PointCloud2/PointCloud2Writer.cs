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

namespace RosSharp.RosBridgeClient
{
    public class PointCloud2Writer : MonoBehaviour
    {
        private bool isReceived = false;
        private Vector3[] points;
        private Color[] colors;
        private PointCloud2Visualizer[] pointCloud2Visualizers;

        private void Update()
        {
            pointCloud2Visualizers = GetComponents<PointCloud2Visualizer>();
            if (isReceived)
                if (pointCloud2Visualizers != null)
                    foreach (PointCloud2Visualizer pointCloud2Visualizer in pointCloud2Visualizers)
                        pointCloud2Visualizer.SetPointCloudData(gameObject.transform, points, colors);

            isReceived = false;
        }

        public void Write(MessageTypes.Sensor.PointCloud2 pointCloud2)
        {
            int numPoints = (int)(pointCloud2.width * pointCloud2.height);
            points = new Vector3[numPoints];
            colors = new Color[numPoints];

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

            for (int i = 0; i < numPoints; i++)
            {
                int index = i * point_step;

                // Extract x, y, z coordinates
                float x = 0, y = 0, z = 0;
                if (x_offset >= 0)
                    x = ReadFloat(data, index + x_offset, x_datatype, pointCloud2.is_bigendian);
                if (y_offset >= 0)
                    y = ReadFloat(data, index + y_offset, y_datatype, pointCloud2.is_bigendian);
                if (z_offset >= 0)
                    z = ReadFloat(data, index + z_offset, z_datatype, pointCloud2.is_bigendian);

                // Convert from ROS (right-handed, z-up) to Unity (left-handed, y-up)
                points[i] = new Vector3(x, y, z).Ros2Unity();

                // Extract color if available
                if (rgb_offset >= 0)
                {
                    uint rgb = ReadUInt(data, index + rgb_offset, rgb_datatype, pointCloud2.is_bigendian);
                    colors[i] = UnpackRGB(rgb);
                }
                else
                {
                    colors[i] = Color.white;
                }
            }

            isReceived = true;
        }

        private float ReadFloat(byte[] data, int offset, byte datatype, bool isBigEndian)
        {
            if (offset + 4 > data.Length)
                return 0f;

            // FLOAT32 = 7
            if (datatype == 7)
            {
                byte[] bytes = new byte[4];
                Array.Copy(data, offset, bytes, 0, 4);

                if (isBigEndian != BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                return BitConverter.ToSingle(bytes, 0);
            }
            // FLOAT64 = 8
            else if (datatype == 8)
            {
                byte[] bytes = new byte[8];
                Array.Copy(data, offset, bytes, 0, 8);

                if (isBigEndian != BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                return (float)BitConverter.ToDouble(bytes, 0);
            }

            return 0f;
        }

        private uint ReadUInt(byte[] data, int offset, byte datatype, bool isBigEndian)
        {
            if (offset + 4 > data.Length)
                return 0;

            // UINT32 = 6, FLOAT32 = 7 (for RGB packed as float)
            byte[] bytes = new byte[4];
            Array.Copy(data, offset, bytes, 0, 4);

            if (isBigEndian != BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
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
