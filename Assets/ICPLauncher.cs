using RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Meta.WitAi;
using Unity.Sentis;
using System.IO;
using System;

public class ICPLauncher : MonoBehaviour
{
    int W = 160;
    int H = 120;

    public float distanceThreshold;
    public int max_iterations;

    public ComputeShader icp_shader;

    int downsample_kernel;
    int correspondence_kernel;

    int groupsX = Mathf.CeilToInt(160 * 120 * 2.0f / 256.0f);


    public DrawMeshInstanced renderer0;
    public DrawMeshInstanced renderer1;
    public DrawMeshInstanced renderer2;
    public DrawMeshInstanced renderer3;

    private ComputeBuffer depth3d_downsampled0Buffer;
    private ComputeBuffer depth3d_downsampled1Buffer;
    private ComputeBuffer correspondenceBuffer;

    private float3[] depth3d_downsampled0;
    private float3[] depth3d_downsampled1;
    private float3[] depth3d_return;
    private int[] correspondence;

    Matrix4x4 res_trans;
    private int frame_count;
    public int frames_per_icp;

    private ComputeBuffer buffer0;
    private ComputeBuffer buffer1;
    private ComputeBuffer buffer2;
    private ComputeBuffer buffer3;

    float3x3 R_all;
    float3 t_all;

    TensorShape depth_shape;

    //public float3[] get_d0()
    //{
    //    return depth3d_downsampled0;
    //}
    //public float3[] get_d1()
    //{
    //    return depth3d_downsampled1;
    //}

    public float3[] get_current_float3(int image_index)
    {
        if (image_index == 0)
        {
            for (int i = 0; i < H * W; i++)
            {
                depth3d_return[i] = depth3d_downsampled0[i];
            }
        }
        if (image_index == 1)
        {
            for (int i = 0; i < H * W; i++)
            {
                depth3d_return[i] = depth3d_downsampled0[i + H * W];
            }
        }
        if (image_index == 2)
        {
            for (int i = 0; i < H * W; i++)
            {
                depth3d_return[i] = depth3d_downsampled1[i];
            }
        }
        if (image_index == 3)
        {
            for (int i = 0; i < H * W; i++)
            {
                depth3d_return[i] = depth3d_downsampled1[i + H * W];
            }
        }
        //string path = "Assets/PointClouds/dedpth3d_icp_return_" + image_index + ".txt";

        //// Use StreamWriter to write to the file. Overwrites old content by default.
        //using (StreamWriter writer = new StreamWriter(path, false))
        //{
        //    foreach (float3 data in depth3d_return)
        //    {
        //        // Write each float3 as "x, y, z" on its own line.
        //        writer.WriteLine($"{data.x}, {data.y}, {data.z}");
        //    }
        //}
        return depth3d_return;
    }

    void Start()
    {
        depth3d_return = new float3[W * H];

        depth_shape = new TensorShape(1, 1, 480, 640);
        buffer0 = new ComputeBuffer(480 * 640, sizeof(float));
        buffer1 = new ComputeBuffer(480 * 640, sizeof(float));
        buffer2 = new ComputeBuffer(480 * 640, sizeof(float));
        buffer3 = new ComputeBuffer(480 * 640, sizeof(float));

        R_all = float3x3.identity;
        t_all = new float3(0.0f, 0.0f, 0.0f);

        res_trans = Matrix4x4.identity;
        frame_count = 0;

        downsample_kernel = icp_shader.FindKernel("MergeDownsample");
        correspondence_kernel = icp_shader.FindKernel("FindCorrespondence");

        icp_shader.SetVector("intrinsics0", renderer0.get_intrinsics());
        icp_shader.SetVector("intrinsics1", renderer1.get_intrinsics());
        icp_shader.SetVector("intrinsics2", renderer2.get_intrinsics());
        icp_shader.SetVector("intrinsics3", renderer3.get_intrinsics());

        icp_shader.SetVector("screenData0", renderer0.get_screenData());
        icp_shader.SetVector("screenData1", renderer1.get_screenData());
        icp_shader.SetVector("screenData2", renderer2.get_screenData());
        icp_shader.SetVector("screenData3", renderer3.get_screenData());

        icp_shader.SetFloat("samplingSize", 4);
        
        icp_shader.SetFloat("t", 1);

        // compute buffers
        depth3d_downsampled0Buffer = new ComputeBuffer(W * H * 2, sizeof(float) * 3);
        depth3d_downsampled1Buffer = new ComputeBuffer(W * H * 2, sizeof(float) * 3);
        correspondenceBuffer = new ComputeBuffer(W * H * 2, sizeof(int));

        icp_shader.SetBuffer(downsample_kernel, "depth3d_downsampled0", depth3d_downsampled0Buffer);
        icp_shader.SetBuffer(downsample_kernel, "depth3d_downsampled1", depth3d_downsampled1Buffer);

        icp_shader.SetBuffer(correspondence_kernel, "depth3d_downsampled0", depth3d_downsampled0Buffer);
        icp_shader.SetBuffer(correspondence_kernel, "depth3d_downsampled1", depth3d_downsampled1Buffer);
        icp_shader.SetBuffer(correspondence_kernel, "correspondence", correspondenceBuffer);

        // datas
        depth3d_downsampled0 = new float3[W * H * 2];
        depth3d_downsampled1 = new float3[W * H * 2];
        correspondence = new int[W * H * 2];

        icp_shader.SetBuffer(downsample_kernel, "depth0", buffer0);
        icp_shader.SetBuffer(downsample_kernel, "depth1", buffer1);
        icp_shader.SetBuffer(downsample_kernel, "depth2", buffer2);
        icp_shader.SetBuffer(downsample_kernel, "depth3", buffer3);

    }

    void OnDestroy()
    {
        if (depth3d_downsampled0Buffer != null)
        {
            depth3d_downsampled0Buffer.Release();
            depth3d_downsampled0Buffer = null;
        }

        if (depth3d_downsampled1Buffer != null)
        {
            depth3d_downsampled1Buffer.Release();
            depth3d_downsampled1Buffer = null;
        }

        if (correspondenceBuffer != null)
        {
            correspondenceBuffer.Release();
            correspondenceBuffer = null;
        }
    }

    public Matrix4x4 run_ICP()
    {

        frame_count++;
        if (frame_count % frames_per_icp != 0)
        {
            Debug.Log("ICP");
            return res_trans;
        }

        //pose
        icp_shader.SetMatrix("_GOPose0", renderer0.get_current_pose());
        icp_shader.SetMatrix("_GOPose1", renderer1.get_current_pose());
        icp_shader.SetMatrix("_GOPose2", renderer2.get_current_pose());
        icp_shader.SetMatrix("_GOPose3", renderer3.get_current_pose());
        //depth
        buffer0.SetData(renderer0.get_depth());
        buffer1.SetData(renderer1.get_depth());
        buffer2.SetData(renderer2.get_depth());
        buffer3.SetData(renderer3.get_depth());


        R_all = float3x3.identity;
        t_all = new float3(0.0f, 0.0f, 0.0f);

        res_trans = Matrix4x4.identity;

        for (int j = 0; j < max_iterations; j++)
        {

            // ============================================================== //


            icp_shader.SetMatrix("_RESTrans", res_trans);
            icp_shader.SetFloat("distanceThreshold", distanceThreshold);


            //dispatch
            icp_shader.Dispatch(downsample_kernel, groupsX, 1, 1);

            // ============================================================== //

            //dispatch
            icp_shader.Dispatch(correspondence_kernel, groupsX, 1, 1);

            // ============================================================== //

            depth3d_downsampled0Buffer.GetData(depth3d_downsampled0);
            depth3d_downsampled1Buffer.GetData(depth3d_downsampled1);
            correspondenceBuffer.GetData(correspondence);

            // ============================================================== //

            float3 mu0 = new float3(0.0f, 0.0f, 0.0f);
            float3 mu1 = new float3(0.0f, 0.0f, 0.0f);
            float3x3 m = float3x3.zero;

            int count = 0;

            int min_index;
            for (int i = 0; i < W * H * 2; i++)
            {
                min_index = correspondence[i];
                if (min_index >= 0 && min_index < W * H * 2 && depth3d_downsampled0[i].z > -1000.0f && depth3d_downsampled1[min_index].z > -1000.0f)
                {
                    mu0 += depth3d_downsampled0[i];
                    mu1 += depth3d_downsampled1[min_index];
                    count++;
                }
            }
            mu0 = mu0 / (float)count;
            mu1 = mu1 / (float)count;

            Debug.Log(count);

            //float dis_threshold = 3 * math.distance(mu0, mu1);
            //float dis_threshold = 100.0f;
            //float dis_all = 100.0f;

            //count = 0;
            //for (int i = 0; i < 120 * 160 * 2; i++)
            //{
            //    min_index = correspondence[i];
            //    if (min_index >= 0 && min_index < 120 * 160 * 2 && depth3d_downsampled0[i].z > -1000.0f && depth3d_downsampled1[min_index].z > -1000.0f)
            //    {
            //        dis_all += math.distance(depth3d_downsampled0[i], depth3d_downsampled1[min_index]);
            //        count ++;
            //    }
            //}
            //dis_all = dis_all / (float)count;


            for (int i = 0; i < W * H * 2; i++)
            {
                min_index = correspondence[i];

                float3 p1 = depth3d_downsampled0[i];

                if (min_index >= 0 && min_index < W * H * 2 && p1.z > -1000.0f && depth3d_downsampled1[min_index].z > -1000.0f)
                {
                    float3 p2 = depth3d_downsampled1[min_index];
                    //float curr_dis = math.distance(p1, p2);
                    //if (curr_dis < dis_all && curr_dis < dis_threshold && math.distance(p1, mu0) < 1.0f && math.distance(p2, mu1) < 1.0f)
                    //{
                    //    m += OuterProduct(p1 - mu0, p2 - mu1);
                    //}
                    m += OuterProduct(p1 - mu0, p2 - mu1);
                }
            }


            // ============================================================== //

            float[,] result = new float[3, 3];
            result[0, 0] = m.c0.x; result[0, 1] = m.c1.x; result[0, 2] = m.c2.x;
            result[1, 0] = m.c0.y; result[1, 1] = m.c1.y; result[1, 2] = m.c2.y;
            result[2, 0] = m.c0.z; result[2, 1] = m.c1.z; result[2, 2] = m.c2.z;

            Matrix<float> mathNetM = DenseMatrix.OfArray(result);

            var svd = mathNetM.Svd(computeVectors: true);
            Matrix<float> U = svd.U;
            //Vector<float> S = svd.S;
            Matrix<float> VT = svd.VT;
            Matrix<float> V = VT.Transpose();
            Matrix<float> UT = U.Transpose();

            // ============================================================= //

            Matrix<float> Rmat = V * UT;

            var Rdet = Rmat.Determinant();
            if (Rdet < 0)
            {
                for (int row = 0; row < 3; row++)
                    V[row, 2] = -V[row, 2];
                Rmat = V * UT;
            }

            float3x3 R_new = new float3x3(
                Rmat[0, 0], Rmat[0, 1], Rmat[0, 2],
                Rmat[1, 0], Rmat[1, 1], Rmat[1, 2],
                Rmat[2, 0], Rmat[2, 1], Rmat[2, 2]
            );
            float3 t_new = mu1 - math.mul(R_new, mu0);

            R_all = math.mul(R_new, R_all);
            t_all = math.mul(R_new, t_all) + t_new;

            //R_all = float3x3.identity;
            //t_all = t_all + mu1 - mu0;

            res_trans.SetColumn(0, new Vector4(R_all.c0.x, R_all.c0.y, R_all.c0.z, 0.0f));
            res_trans.SetColumn(1, new Vector4(R_all.c1.x, R_all.c1.y, R_all.c1.z, 0.0f));
            res_trans.SetColumn(2, new Vector4(R_all.c2.x, R_all.c2.y, R_all.c2.z, 0.0f));
            res_trans.SetColumn(3, new Vector4(t_all.x, t_all.y, t_all.z, 1.0f));

        }


        return res_trans;
    }

    private static float3x3 OuterProduct(float3 p, float3 q)
    {
        return new float3x3(
            p.x * q.x, p.x * q.y, p.x * q.z,
            p.y * q.x, p.y * q.y, p.y * q.z,
            p.z * q.x, p.z * q.y, p.z * q.z
        );
    }
}
