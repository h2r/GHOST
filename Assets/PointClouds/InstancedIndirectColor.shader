Shader "Custom/InstancedIndirectColor" {
    SubShader{
        Tags { "RenderType" = "Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct MeshProperties {
                float4 pos;
            };

            sampler2D _colorMap;

            int w;                      // Here is the real width but hidden under a pseudonym
            float width; float height;  //   1 / width and 1 / height actually but don't tell anyone

            float a;                   // angle

            float4 _colorMap_ST;

            float4 intrinsics;         // [2023-10-30][JHT] Contains (CX, CY, FX, FY) - center point x, center point y, focal length x, focal length y
            float4 screenData;         // [2023-10-30][JHT] Contains (width, height, 1/width, FY) - screen/image coordinate data
            float samplingSize;        // [2023-10-30][JHT] Value of 1 samples all pixels; value of 2 samples half the pixels

            float pS;                   // point scalar

            StructuredBuffer<MeshProperties> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                //float scalarProd = -(spotPos.z - _Properties[instanceID].pos.z) / sqrt(pow(spotPos.x - _Properties[instanceID].pos.x,2) + pow(spotPos.z - _Properties[instanceID].pos.z,2)); 
                //float sina = sin(acos(scalarProd));
                
                float d = _Properties[instanceID].pos.w ;

                float iii = float(instanceID) * samplingSize;

                // [2023-10-30][JHT] Expose a quick scalar for the points themselves
                float4x4 matS = {   pS,  0.0, 0.0, 0.0,
							        0.0, pS,  0.0, 0.0,
							        0.0, 0.0, pS,  0.0,
							        0.0, 0.0, 0.0, 1.0 };

                float4 vpos = mul(matS, i.vertex);

                // [2023-10-30][JHT] What is this matrix? 
                // We have scaling, rotation around X axis, and translation components
                // It looks like we have already multiplied the scaling and rotation components, but simply added the translation.
                // But, what is that middle d doing there in row 2? when we multiply scale by rotation, it shouldn't exist...
                //
                float4x4 mat = {    cos(a) * d,     0.0,    sin(a) * d, _Properties[instanceID].pos.x,
							        0.0,            d,      0.0,        _Properties[instanceID].pos.y,
							        - sin(a) * d,   0.0,    cos(a) * d, _Properties[instanceID].pos.z,
							        0.0,            0.0,    0.0,        1.0 };
                                  
                /*
                float4x4 mat = 	 {1.0,0.0,0.0,_Properties[instanceID].pos.x,
							      0.0,1.0,0.0,_Properties[instanceID].pos.y,
							      0.0,0.0,1.0,_Properties[instanceID].pos.z,
							      0.0,0.0,0.0,1.0 }; */

                float4 pos = mul(mat, vpos);
                //float4 pos = mul(mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                // o.vertex = _Properties[instanceID].pos;

                //o.color = _Properties[instanceID].color;
                //o.color.r = instanceID * 0.000003;
                
                float id = float(instanceID);

                float4 coor = {1 - (iii - floor(iii * screenData.z) * screenData.x) * width, floor(iii * screenData.z) * height, 0.0, 0.0};
                
                //float4 coor = {floor(instanceID * width), floor(instanceID * width), 0.0, 0.0};
                float2 uv = TRANSFORM_TEX(coor.xy, _colorMap);
                coor.x = uv.x; coor.y = uv.y;
                //float4 coor = {0.2,0.5,0.0,0.0};
                o.color = tex2Dlod(_colorMap, coor);

                // if (_Properties[instanceID].pos.y < 0.4 || _Properties[instanceID].pos.z < -0.25)
                // {
                //     o.vertex = float4(0.0, 0.0, 0.0, 1.0);
                // }

                //o.uv = TRANSFORM_TEX (i.texcoord, _colorMap);
                //o.color.r = _Properties[instanceID].pos.w * 100000;
                //o.color.g = _Properties[instanceID].pos.w / 64;
                //o.color.b = _Properties[instanceID].pos.w / 64;
                //o.color.b = coor.y;
                //o.color.r = coor.y;
                //o.color.g = coor.y;
                //o.color.g = 0.0;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                return i.color;
                //return tex2D(_colorMap,i.uv);
            }

            ENDCG
        }
    }
}