using RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

public class ICPLauncher : MonoBehaviour
{
    public ComputeShader icp_shader;

    int downsample_kernel;
    int correspondence_kernel;

    int groupsX = Mathf.CeilToInt(80.0f * 60.0f * 2.0f / 256.0f);


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

    void Start()
    {
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

        icp_shader.SetFloat("samplingSize", 8);
        icp_shader.SetFloat("t", 1);

        // compute buffers
        depth3d_downsampled0Buffer = new ComputeBuffer(60 * 80 * 2, sizeof(float) * 3);
        depth3d_downsampled1Buffer = new ComputeBuffer(60 * 80 * 2, sizeof(float) * 3);
        correspondenceBuffer = new ComputeBuffer(60 * 80 * 2, sizeof(int));

        icp_shader.SetBuffer(downsample_kernel, "depth3d_downsampled0", depth3d_downsampled0Buffer);
        icp_shader.SetBuffer(downsample_kernel, "depth3d_downsampled1", depth3d_downsampled1Buffer);

        icp_shader.SetBuffer(correspondence_kernel, "depth3d_downsampled0", depth3d_downsampled0Buffer);
        icp_shader.SetBuffer(correspondence_kernel, "depth3d_downsampled1", depth3d_downsampled1Buffer);
        icp_shader.SetBuffer(correspondence_kernel, "correspondence", correspondenceBuffer);

        // datas
        depth3d_downsampled0 = new float3[60 * 80 * 2];
        depth3d_downsampled1 = new float3[60 * 80 * 2];
        correspondence = new int[60 * 80 * 2];
    }

    public Matrix4x4 run_ICP(ComputeBuffer depth0, ComputeBuffer depth1, ComputeBuffer depth2, ComputeBuffer depth3, bool activate_ICP)
    {
        if (!activate_ICP)
        {
            return Matrix4x4.identity;
        }

        if (frame_count % frames_per_icp != 0)
        {
            return res_trans;
        }

        // ============================================================== //

        //pose
        icp_shader.SetMatrix("_GOPose0", renderer0.get_current_pose());
        icp_shader.SetMatrix("_GOPose1", renderer2.get_current_pose());
        icp_shader.SetMatrix("_RESTrans", res_trans);

        //depth
        icp_shader.SetBuffer(downsample_kernel, "depth0", depth0);
        icp_shader.SetBuffer(downsample_kernel, "depth1", depth1);
        icp_shader.SetBuffer(downsample_kernel, "depth2", depth2);
        icp_shader.SetBuffer(downsample_kernel, "depth3", depth3);

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

        for (int i = 0; i < 60 * 80 * 2; i++)
        {
            mu0 += depth3d_downsampled0[i];
            mu1 += depth3d_downsampled1[i];
        }
        mu0 = mu0 / (80.0f * 60.0f * 2.0f);
        mu1 = mu1 / (80.0f * 60.0f * 2.0f);

        for (int i = 0; i < 60 * 80 * 2; i++)
        {
            m += OuterProduct(depth3d_downsampled0[i] - mu0, depth3d_downsampled1[i] - mu1);
        }
            

        // ============================================================== //

        float[,] rawData = new float[,]
        {
            { m.c0.x, m.c1.x, m.c2.x },
            { m.c0.y, m.c1.y, m.c2.y },
            { m.c0.z, m.c1.z, m.c2.z },
        };

        Matrix<float> mathNetM = DenseMatrix.OfArray(rawData);

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

        float3x3 R = new float3x3(
            Rmat[0, 0], Rmat[0, 1], Rmat[0, 2],
            Rmat[1, 0], Rmat[1, 1], Rmat[1, 2],
            Rmat[2, 0], Rmat[2, 1], Rmat[2, 2]
        );
        float3 t = mu1 - math.mul(R, mu0);

        Matrix4x4 T = new Matrix4x4(
            new Vector4(R.c0.x, R.c0.y, R.c0.z, 0f),
            new Vector4(R.c1.x, R.c1.y, R.c1.z, 0f),
            new Vector4(R.c2.x, R.c2.y, R.c2.z, 0f),
            new Vector4(t.x, t.y, t.z, 1f)
        );

        res_trans = math.mul(T, res_trans);
        //res_trans = math.mul(res_trans, T);

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
