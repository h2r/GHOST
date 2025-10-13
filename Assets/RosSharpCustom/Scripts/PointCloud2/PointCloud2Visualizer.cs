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

namespace RosSharp.RosBridgeClient
{
    public abstract class PointCloud2Visualizer : MonoBehaviour
    {
        protected Transform base_transform;
        protected Vector3[] points;
        protected Color[] colors;

        protected bool IsNewDataReceived;
        protected bool IsVisualized = false;

        abstract protected void Visualize();
        abstract protected void DestroyObjects();

        protected void Update()
        {
            if (!IsNewDataReceived)
                return;

            IsNewDataReceived = false;
            Visualize();
        }

        protected void OnDisable()
        {
            DestroyObjects();
        }

        public void SetPointCloudData(Transform _base_transform, Vector3[] _points, Color[] _colors)
        {
            base_transform = _base_transform;
            points = _points;
            colors = _colors;
            IsNewDataReceived = true;
        }
    }
}
