Shader "Custom/InstancedIndirectColor" {
    SubShader{
        Tags { "RenderType" = "Opaque" }

        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
            };

            struct MeshProperties {
                float4 pos;
            };

            sampler2D _colorMap;

            float invWidth;
            float invHeight;

            float angle;

            float4 _colorMap_ST;
            float4x4 _GOPose;
            // Local billboard basis is set by DrawMeshInstanced; this avoids rebuilding mesh vertices every frame.
            float3 _BillboardRight;
            float3 _BillboardUp;

            float4 screenData;         // (width, height, 1 / width, focalY)
            float samplingSize;        // 1 samples every pixel; 2 samples every other pixel.

            StructuredBuffer<MeshProperties> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float depthScale = _Properties[instanceID].pos.w;

                float sampledIndex = float(instanceID) * samplingSize;

                float3 billboardVertex = _BillboardRight * i.vertex.x + _BillboardUp * i.vertex.y;
                float4 vpos = float4(billboardVertex, 1.0);

                // Depth scales each quad in place, then the GameObject pose moves the cloud as a unit.
                float4x4 mat = {
                    cos(angle) * depthScale,   0.0,          sin(angle) * depthScale,   _Properties[instanceID].pos.x,
                    0.0,                       depthScale,   0.0,                        _Properties[instanceID].pos.y,
                    -sin(angle) * depthScale,  0.0,          cos(angle) * depthScale,    _Properties[instanceID].pos.z,
                    0.0,                       0.0,          0.0,                        1.0
                };

                float4 pos = mul(_GOPose, mul(mat, vpos));
                o.vertex = mul(UNITY_MATRIX_VP, pos);

                // Map the sampled instance back to its source image pixel for color lookup.
                float4 texCoord = {1 - (sampledIndex - floor(sampledIndex * screenData.z) * screenData.x) * invWidth, floor(sampledIndex * screenData.z) * invHeight, 0.0, 0.0};
                float2 uv = TRANSFORM_TEX(texCoord.xy, _colorMap);
                texCoord.x = uv.x; texCoord.y = 1.0 - uv.y;
                o.color = tex2Dlod(_colorMap, texCoord);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                return i.color;
            }

            ENDCG
        }
    }
}
