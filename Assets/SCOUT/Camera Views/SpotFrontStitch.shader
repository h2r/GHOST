Shader "SCOUT/Spot Front Stitch"
{
    Properties
    {
        _FrontRightTex ("Front Right", 2D) = "black" {}
        _FrontLeftTex ("Front Left", 2D) = "black" {}
        _BackColor ("Background", Color) = (0, 0, 0, 1)
        _PlaneDistance ("Plane Distance", Float) = 2
        _PlaneSize ("Plane Size", Vector) = (7.6, 5.7, 0, 0)
        _BlendingPower ("Blending Power", Float) = 10
        _BlendingMinimum ("Blending Minimum", Range(0, 1)) = 0.001
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 planePoint : TEXCOORD0;
            };

            sampler2D _FrontRightTex;
            sampler2D _FrontLeftTex;

            float4x4 _FrontRightMVP;
            float4x4 _FrontLeftMVP;

            float4 _FrontRightUvTransform;
            float4 _FrontLeftUvTransform;
            float _FrontRightUvRotation;
            float _FrontLeftUvRotation;

            float _PlaneDistance;
            float2 _PlaneSize;
            float4 _BackColor;
            float _BlendingPower;
            float _BlendingMinimum;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float2 centeredUv = v.uv - 0.5;
                o.planePoint = float3(
                    centeredUv.x * _PlaneSize.x,
                    -centeredUv.y * _PlaneSize.y,
                    _PlaneDistance);

                return o;
            }

            float CalculateScore(float2 imageLocation)
            {
                float2 locationNormalized = imageLocation * 2.0 - 1.0;
                locationNormalized = 1.0 - abs(locationNormalized);

                if (min(locationNormalized.x, locationNormalized.y) < 0.0)
                    return 0.0;

                float score = lerp(locationNormalized.x * locationNormalized.y, 1.0, _BlendingMinimum);
                return pow(score, _BlendingPower);
            }

            float2 RotateUv(float2 uv, float rotation)
            {
                if (rotation < 0.5)
                    return uv;

                if (rotation < 1.5)
                    return float2(uv.y, 1.0 - uv.x);

                if (rotation < 2.5)
                    return float2(1.0 - uv.y, uv.x);

                return float2(1.0 - uv.x, 1.0 - uv.y);
            }

            float2 TransformUv(float2 uv, float4 uvTransform, float rotation)
            {
                uv = RotateUv(uv, rotation);
                return uv * uvTransform.xy + uvTransform.zw;
            }

            float2 CalculateImageLocation(float4 projectedPoint, out float weight)
            {
                if (projectedPoint.z <= 0.0)
                {
                    weight = 0.0;
                    return float2(0.0, 0.0);
                }

                float2 location = projectedPoint.xy / projectedPoint.z;
                weight = CalculateScore(location);
                return location;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rightWeight;
                float2 rightUv = CalculateImageLocation(mul(_FrontRightMVP, float4(i.planePoint, 1.0)), rightWeight);

                float leftWeight;
                float2 leftUv = CalculateImageLocation(mul(_FrontLeftMVP, float4(i.planePoint, 1.0)), leftWeight);

                float totalWeight = rightWeight + leftWeight;
                if (totalWeight <= 0.0)
                    return _BackColor;

                rightUv = saturate(TransformUv(rightUv, _FrontRightUvTransform, _FrontRightUvRotation));
                leftUv = saturate(TransformUv(leftUv, _FrontLeftUvTransform, _FrontLeftUvRotation));

                fixed4 rightColor = tex2D(_FrontRightTex, rightUv);
                fixed4 leftColor = tex2D(_FrontLeftTex, leftUv);

                return (rightColor * rightWeight + leftColor * leftWeight) / totalWeight;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Texture"
}
