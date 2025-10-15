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
    public class PointCloud2VisualizerQuad : PointCloud2Visualizer
    {
        [Range(0.001f, 200.1f)]
        public float pointSize = 0.01f;

        public float range;
        public Material pc_material;

        private Mesh mesh;
        public int maxPointsToVisualize = 10000;
        public Transform mainCameraRot;
        ComputeBuffer positionBuffer;

        private RenderParams renderParams;
        private GraphicsBuffer commandBuffer;

        private GameObject pointCloudContainer;
        private GameObject[] pointSpheres;
        private bool IsCreated = false;

        private bool BufferPopulated = false;

        private void Create(Vector3[] positions)
        {
            this.mesh = CreateQuad(pointSize, pointSize);

            // Create and populate position buffer
            positionBuffer = new ComputeBuffer(positions.Length, sizeof(float) * 3);
            positionBuffer.SetData(positions);

            // Setup render params
            renderParams = new RenderParams(pc_material);
            renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f); // Large bounds to avoid culling
            renderParams.matProps = new MaterialPropertyBlock();
            renderParams.matProps.SetBuffer("_Positions", positionBuffer);

            // Create command buffer
            commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        }

        protected Vector3 get_normal()
        {
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
            if (points == null || points.Length == 0)
                return;

            // Limit points if necessary
            int numPoints = Mathf.Min(points.Length, maxPointsToVisualize);
            Vector3[] positions = new Vector3[numPoints];
            System.Array.Copy(points, positions, numPoints);

            if (!IsCreated)
            {
                Create(positions);
                IsCreated = true;
            }
            else
            {
                // Update positions if already created
                if (positionBuffer != null && positionBuffer.count == numPoints)
                {
                    positionBuffer.SetData(positions);
                    renderParams.matProps.SetBuffer("_Positions", positionBuffer);
                }
                else
                {
                    // Recreate if point count changed
                    DestroyObjects();
                    Create(positions);
                    IsCreated = true;
                }
            }

            SetupCommandData(numPoints);
        }

        protected override void Render()
        {
            // Orient Quad facing the user
            OrientQuad(mesh, get_normal());
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

        private void OrientQuad(Mesh mesh, Vector3 vec_dir)
        {
            if (vec_dir.sqrMagnitude < 1e-6f) return;          // guard against zero
            Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, vec_dir.normalized);

            float w = pointSize * .5f;
            float h = pointSize * .5f;

            Vector3[] v = new Vector3[4] {
                new Vector3(-w, -h, 0),
                new Vector3(w, -h, 0),
                new Vector3(-w, h, 0),
                new Vector3(w, h, 0)
            };

            Vector3[] n = new Vector3[4] {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
            };

            for (int i = 0; i < v.Length; ++i)
            {
                v[i] = rot * v[i];      // rotate vertex positions
                n[i] = rot * n[i];      // rotate normals
            }

            mesh.vertices = v;
            mesh.normals = n;
            //mesh.RecalculateBounds();
        }
    }
}
