Shader "Custom/PointCloudShader" {
    SubShader{
        Tags { "RenderType" = "Opaque" }

        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            StructuredBuffer<float3> _Positions;

            v2f vert(appdata_t i) {
                v2f o;

                // Get the world position for this instance
                float3 worldPosition = _Positions[i.instanceID];

                // Apply the quad vertex offset + world position
                float3 localPos = i.vertex.xyz + worldPosition;

                // Transform to clip space
                o.vertex = mul(UNITY_MATRIX_VP, float4(localPos, 1.0));
                o.worldPos = worldPosition;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                return fixed4(0, 1, 0, 1); // Green color
            }

            ENDCG
        }
    }
}