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
    public class PointCloud2VisualizerSpheres : PointCloud2Visualizer
    {
        [Range(0.001f, 0.1f)]
        public float pointSize = 0.01f;
        public Material material;
        public int maxPointsToVisualize = 10000;

        private GameObject pointCloudContainer;
        private GameObject[] pointSpheres;
        private bool IsCreated = false;

        private void Create(int numPoints)
        {
            // Limit the number of spheres for performance
            int numToCreate = Mathf.Min(numPoints, maxPointsToVisualize);

            pointCloudContainer = new GameObject("PointCloud2Spheres");
            pointCloudContainer.transform.parent = null;

            pointSpheres = new GameObject[numToCreate];

            for (int i = 0; i < numToCreate; i++)
            {
                pointSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(pointSpheres[i].GetComponent<Collider>());
                pointSpheres[i].name = "Point_" + i;
                pointSpheres[i].transform.parent = pointCloudContainer.transform;
                pointSpheres[i].GetComponent<Renderer>().material = material;
            }
            IsCreated = true;
        }

        protected override void Visualize()
        {
            if (points == null || points.Length == 0)
                return;

            int numPoints = Mathf.Min(points.Length, maxPointsToVisualize);

            if (!IsCreated || pointSpheres.Length != numPoints)
            {
                if (IsCreated)
                    DestroyObjects();
                Create(numPoints);
            }

            pointCloudContainer.transform.SetPositionAndRotation(base_transform.position, base_transform.rotation);

            // Calculate stride for downsampling if needed
            int stride = Mathf.Max(1, points.Length / maxPointsToVisualize);

            int sphereIndex = 0;
            for (int i = 0; i < points.Length && sphereIndex < pointSpheres.Length; i += stride)
            {
                pointSpheres[sphereIndex].SetActive(true);
                pointSpheres[sphereIndex].transform.localPosition = points[i];
                pointSpheres[sphereIndex].transform.localScale = pointSize * Vector3.one;

                // Apply color if available
                if (colors != null && i < colors.Length)
                {
                    pointSpheres[sphereIndex].GetComponent<Renderer>().material.SetColor("_Color", colors[i]);
                }

                sphereIndex++;
            }

            // Disable unused spheres
            for (; sphereIndex < pointSpheres.Length; sphereIndex++)
            {
                pointSpheres[sphereIndex].SetActive(false);
            }
        }

        protected override void DestroyObjects()
        {
            if (pointSpheres != null)
            {
                for (int i = 0; i < pointSpheres.Length; i++)
                {
                    if (pointSpheres[i] != null)
                        Destroy(pointSpheres[i]);
                }
            }

            if (pointCloudContainer != null)
                Destroy(pointCloudContainer);

            IsCreated = false;
        }
    }
}
