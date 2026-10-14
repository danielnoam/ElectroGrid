Shader "ElectroGrid/FuseRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (2, 1.6, 1.2, 1)
        [HDR] _SparkColor ("Spark", Color) = (4, 3, 1.5, 1)
        _Fill ("Fill", Range(0, 1)) = 1
        _Radius ("Radius", Range(0, 0.5)) = 0.43
        _Thickness ("Thickness", Range(0, 0.2)) = 0.05
        _TrackAlpha ("Track", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _SparkColor;
                float _Fill;
                float _Radius;
                float _Thickness;
                float _TrackAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv - 0.5;
                float dist = length(p);
                float ring = 1.0 - smoothstep(_Thickness * 0.5, _Thickness * 0.5 + 0.012, abs(dist - _Radius));

                float angle = atan2(p.x, p.y) / (2.0 * PI);
                angle = frac(angle + 1.0);
                float lit = step(angle, _Fill);

                float sparkAngle = _Fill * 2.0 * PI;
                float2 sparkPos = float2(sin(sparkAngle), cos(sparkAngle)) * _Radius;
                float flicker = 0.7 + 0.3 * sin(_Time.y * 40.0);
                float spark = (1.0 - smoothstep(0.0, _Thickness * 1.8, length(p - sparkPos))) * flicker * step(0.001, _Fill);

                float3 color = _Color.rgb * ring * (lit + (1.0 - lit) * _TrackAlpha) + _SparkColor.rgb * spark;
                return half4(color * input.color.rgb * input.color.a, 1);
            }
            ENDHLSL
        }
    }
}
