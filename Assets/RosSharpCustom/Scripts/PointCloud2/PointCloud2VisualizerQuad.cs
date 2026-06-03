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
using UnityEngine.Serialization;

namespace RosSharp.RosBridgeClient
{
    public class PointCloud2VisualizerQuad : PointCloud2Visualizer
    {
        [Range(0.001f, 200.1f)]
        public float pointSize = 0.01f;

        public float range;
        [FormerlySerializedAs("pc_material")]
        public Material pointCloudMaterial;

        private Mesh mesh;
        public int maxPointsToVisualize = 10000;
        public Transform mainCameraRot;
        ComputeBuffer positionBuffer;

        private RenderParams renderParams;
        private GraphicsBuffer commandBuffer;
        private readonly Vector3[] billboardVertices = new Vector3[4];
        private readonly Vector3[] billboardNormals = new Vector3[4];

        private bool IsCreated = false;

        private void Create(int capacity)
        {
            this.mesh = CreateQuad(pointSize, pointSize);

            positionBuffer = new ComputeBuffer(capacity, sizeof(float) * 3);

            // Setup render params
            renderParams = new RenderParams(pointCloudMaterial);
            renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f); // Large bounds to avoid culling
            renderParams.matProps = new MaterialPropertyBlock();
            renderParams.matProps.SetBuffer("_Positions", positionBuffer);

            // Create command buffer
            commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        }

        protected Vector3 GetNormal()
        {
            if (mainCameraRot == null)
                return -Vector3.forward;

            var relRot = Quaternion.Euler(0f, mainCameraRot.rotation.eulerAngles.y, 0f);
            var res = relRot * new Vector3(0f, 0f, -1.0f);
            return res.normalized;
        }

        void SetupCommandData(int instanceCount)
        {
            var commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
            commandData[0].instanceCount = (uint)instanceCount;
            commandData[0].startIndex = mesh.GetIndexStart(0);
            commandData[0].baseVertexIndex = mesh.GetBaseVertex(0);
            commandData[0].startInstance = 0;

            commandBuffer.SetData(commandData);
        }
        protected override void Visualize()
        {
            if (points == null || pointCount == 0)
            {
                DestroyObjects();
                return;
            }

            int numPoints = Mathf.Min(pointCount, maxPointsToVisualize);
            if (numPoints <= 0)
                return;

            if (!IsCreated)
            {
                Create(numPoints);
                IsCreated = true;
            }
            else if (positionBuffer == null || positionBuffer.count < numPoints)
            {
                DestroyObjects();
                Create(numPoints);
                IsCreated = true;
            }

            positionBuffer.SetData(points, 0, 0, numPoints);
            renderParams.matProps.SetBuffer("_Positions", positionBuffer);
            SetupCommandData(numPoints);
        }

        protected override void Render()
        {
            if (mesh == null || commandBuffer == null)
            {
                return;
            }
            // Orient Quad facing the user
            OrientQuad(mesh, GetNormal());
            // Draw
            Graphics.RenderMeshIndirect(renderParams, mesh, commandBuffer);
        }

        protected override void DestroyObjects()
        {
            positionBuffer?.Release();
            positionBuffer = null;

            commandBuffer?.Release();
            commandBuffer = null;

            if (mesh != null)
            {
                Destroy(mesh);
                mesh = null;
            }

            IsCreated = false;
        }

        private Mesh CreateQuad(float width = 1f, float height = 1f)
        {
            // Create a quad mesh.
            var mesh = new Mesh();

            float w = width * .5f;
            float h = height * .5f;
            var vertices = new Vector3[4] {
                new Vector3(-w, -h, 0),
                new Vector3(w, -h, 0),
                new Vector3(-w, h, 0),
                new Vector3(w, h, 0)
            };

            var tris = new int[6] {
                // lower left tri.
                0, 2, 1,
                // lower right tri
                2, 3, 1
            };

            var normals = new Vector3[4] {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
            };

            var uv = new Vector2[4] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
            };

            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.normals = normals;
            mesh.uv = uv;

            return mesh;
        }

        private void OrientQuad(Mesh mesh, Vector3 direction)
        {
            if (direction.sqrMagnitude < 1e-6f) return;          // guard against zero
            Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, direction.normalized);

            float w = pointSize * .5f;
            float h = pointSize * .5f;

            billboardVertices[0] = new Vector3(-w, -h, 0);
            billboardVertices[1] = new Vector3(w, -h, 0);
            billboardVertices[2] = new Vector3(-w, h, 0);
            billboardVertices[3] = new Vector3(w, h, 0);

            billboardNormals[0] = -Vector3.forward;
            billboardNormals[1] = -Vector3.forward;
            billboardNormals[2] = -Vector3.forward;
            billboardNormals[3] = -Vector3.forward;

            for (int i = 0; i < billboardVertices.Length; ++i)
            {
                billboardVertices[i] = rot * billboardVertices[i];
                billboardNormals[i] = rot * billboardNormals[i];
            }

            mesh.vertices = billboardVertices;
            mesh.normals = billboardNormals;
            //mesh.RecalculateBounds();
        }
    }
}
