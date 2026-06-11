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

                // Point center in world space (GameObject pose moves the cloud as a unit).
                float3 centerWorld = mul(_GOPose, float4(_Properties[instanceID].pos.xyz, 1.0)).xyz;

                // True per-point billboard: face whichever camera is rendering this pass
                // (_WorldSpaceCameraPos is set per camera, and per-eye for the XR rig), so the cloud
                // stays a faithful flat-card approximation of the surface from any viewpoint instead
                // of only from one fixed yaw. Replaces the uniform _BillboardRight/_BillboardUp + the
                // 'angle' yaw matrix (both now unused).
                float3 toCam = _WorldSpaceCameraPos.xyz - centerWorld;
                float3 fwd = (dot(toCam, toCam) > 1e-12) ? normalize(toCam) : float3(0.0, 0.0, -1.0);
                float3 worldUp = (abs(fwd.y) > 0.999) ? float3(0.0, 0.0, 1.0) : float3(0.0, 1.0, 0.0);
                float3 right = normalize(cross(worldUp, fwd));
                float3 up = cross(fwd, right);

                float3 offsetWorld = (right * i.vertex.x + up * i.vertex.y) * depthScale;
                o.vertex = mul(UNITY_MATRIX_VP, float4(centerWorld + offsetWorld, 1.0));

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
