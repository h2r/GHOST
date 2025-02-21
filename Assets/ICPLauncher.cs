using RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Meta.WitAi;

public class ICPLauncher : MonoBehaviour
{
    public float distanceThreshold;
    public int max_iterations;

    public ComputeShader icp_shader;

    int downsample_kernel;
    int correspondence_kernel;

    int groupsX = Mathf.CeilToInt(160.0f * 120.0f * 2.0f / 256.0f);


    public DrawMeshInstanced renderer0;
    public DrawMeshInstanced renderer1;
    public DrawMeshInstanced renderer2;
    public DrawMeshInstanced renderer3;

    private ComputeBuffer depth3d_downsampled0Buffer;
    private ComputeBuffer depth3d_downsampled1Buffer;
    private ComputeBuffer correspondenceBuffer;

    private float3[] depth3d_downsampled0;
    private float3[] depth3d_downsampled1;
    private int[] correspondence;

    Matrix4x4 res_trans;
    private int frame_count;
    public int frames_per_icp;

    float3x3 R_all;
    float3 t_all;

    void Start()
    {
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
        icp_shader.SetFloat("distanceThreshold", distanceThreshold);
        icp_shader.SetFloat("t", 1);

        // compute buffers
        depth3d_downsampled0Buffer = new ComputeBuffer(120 * 160 * 2, sizeof(float) * 3);
        depth3d_downsampled1Buffer = new ComputeBuffer(120 * 160 * 2, sizeof(float) * 3);
        correspondenceBuffer = new ComputeBuffer(120 * 160 * 2, sizeof(int));

        icp_shader.SetBuffer(downsample_kernel, "depth3d_downsampled0", depth3d_downsampled0Buffer);
        icp_shader.SetBuffer(downsample_kernel, "depth3d_downsampled1", depth3d_downsampled1Buffer);

        icp_shader.SetBuffer(correspondence_kernel, "depth3d_downsampled0", depth3d_downsampled0Buffer);
        icp_shader.SetBuffer(correspondence_kernel, "depth3d_downsampled1", depth3d_downsampled1Buffer);
        icp_shader.SetBuffer(correspondence_kernel, "correspondence", correspondenceBuffer);

        // datas
        depth3d_downsampled0 = new float3[120 * 160 * 2];
        depth3d_downsampled1 = new float3[120 * 160 * 2];
        correspondence = new int[120 * 160 * 2];
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

    public Matrix4x4 run_ICP(ComputeBuffer depth0, ComputeBuffer depth1, ComputeBuffer depth2, ComputeBuffer depth3, bool activate_ICP)
    {
        if (!activate_ICP)
        {
            return Matrix4x4.identity;
        }

        frame_count++;
        if (frame_count % frames_per_icp != 0)
        {
            Debug.Log("ICP");
            return res_trans;
        }

        //pose
        icp_shader.SetMatrix("_GOPose0", renderer0.get_current_pose());
        icp_shader.SetMatrix("_GOPose1", renderer2.get_current_pose());
        //depth
        icp_shader.SetBuffer(downsample_kernel, "depth0", depth0);
        icp_shader.SetBuffer(downsample_kernel, "depth1", depth1);
        icp_shader.SetBuffer(downsample_kernel, "depth2", depth2);
        icp_shader.SetBuffer(downsample_kernel, "depth3", depth3);

        R_all = float3x3.identity;
        t_all = new float3(0.0f, 0.0f, 0.0f);

        res_trans = Matrix4x4.identity;

        for (int j = 0; j < max_iterations; j++)
        {

            // ============================================================== //


            icp_shader.SetMatrix("_RESTrans", res_trans);

            

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
            for (int i = 0; i < 160 * 120 * 2; i++)
            {
                min_index = correspondence[i];
                if (min_index >= 0 && min_index < 120 * 160 * 2 && depth3d_downsampled0[i].z > -1000.0f && depth3d_downsampled1[min_index].z > -1000.0f)
                {
                    mu0 += depth3d_downsampled0[i];
                    mu1 += depth3d_downsampled1[min_index];
                    count++;
                }
            }
            mu0 = mu0 / (float)count;
            mu1 = mu1 / (float)count;


            for (int i = 0; i < 120 * 160 * 2; i++)
            {
                min_index = correspondence[i];
                if (min_index >= 0 && min_index < 120 * 160 * 2 && depth3d_downsampled0[i].z > -1000.0f && depth3d_downsampled1[min_index].z > -1000.0f)
                {
                    m += OuterProduct(depth3d_downsampled0[i] - mu0, depth3d_downsampled1[min_index] - mu1);
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


        }

        res_trans.SetColumn(0, new Vector4(R_all.c0.x, R_all.c0.y, R_all.c0.z, 0.0f));
        res_trans.SetColumn(1, new Vector4(R_all.c1.x, R_all.c1.y, R_all.c1.z, 0.0f));
        res_trans.SetColumn(2, new Vector4(R_all.c2.x, R_all.c2.y, R_all.c2.z, 0.0f));
        res_trans.SetColumn(3, new Vector4(t_all.x, t_all.y, t_all.z, 1.0f));

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
