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

using System;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public abstract class PointCloud2Visualizer : MonoBehaviour
    {
        protected Transform base_transform;
        protected Vector3[] points;
        protected Color[] colors;
        protected int pointCount;

        protected bool IsNewDataReceived;
        protected bool IsVisualized = false;

        abstract protected void Render();
        abstract protected void Visualize();
        abstract protected void DestroyObjects();

        protected void Update()
        {
            if (IsNewDataReceived)
            {
                Visualize();
                IsNewDataReceived = false;
            }
            Render();
        }

        protected void OnDisable()
        {
            DestroyObjects();
        }

        protected void OnDestroy()
        {
            DestroyObjects();
        }

        public void SetPointCloudData(Transform _base_transform, Vector3[] _points, Color[] _colors)
        {
            SetPointCloudData(_base_transform, _points, _colors, _points == null ? 0 : _points.Length);
        }

        public void SetPointCloudData(Transform _base_transform, Vector3[] _points, Color[] _colors, int _pointCount)
        {
            base_transform = _base_transform;
            if (_points == null)
            {
                points = null;
                colors = null;
                pointCount = 0;
                IsNewDataReceived = true;
                return;
            }

            pointCount = Mathf.Min(Mathf.Max(0, _pointCount), _points.Length);
            if (pointCount == 0)
            {
                points = null;
                colors = null;
                IsNewDataReceived = true;
                return;
            }

            // Keep a visualizer-owned copy so callback buffers can be reused safely.
            if (points == null || points.Length < pointCount)
                points = new Vector3[pointCount];
            Array.Copy(_points, points, pointCount);

            if (_colors != null)
            {
                int colorCount = Mathf.Min(pointCount, _colors.Length);
                if (colors == null || colors.Length < pointCount)
                    colors = new Color[pointCount];
                Array.Copy(_colors, colors, colorCount);
                for (int i = colorCount; i < pointCount; i++)
                    colors[i] = Color.white;
            }
            else
            {
                colors = null;
            }

            IsNewDataReceived = true;
        }
    }
}
