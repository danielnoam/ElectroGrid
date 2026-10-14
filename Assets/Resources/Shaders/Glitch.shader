Shader "ElectroGrid/Glitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _ColorA ("Color A", Color) = (0, 2.2, 2, 1)
        [HDR] _ColorB ("Color B", Color) = (2.4, 0, 1.2, 1)
        _Seed ("Seed", Float) = 0
        _Intensity ("Intensity", Range(0, 1)) = 1
        _BandCount ("Bands", Float) = 9
        _Speed ("Speed", Float) = 6
        _Border ("Border", Range(0, 0.5)) = 0.07
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ColorA;
                float4 _ColorB;
                float _Seed;
                float _Intensity;
                float _BandCount;
                float _Speed;
                float _Border;
            CBUFFER_END

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = floor(_Time.y * _Speed + _Seed * 13.0);

                float band = floor(uv.y * _BandCount);
                float bandNoise = Hash(float2(band, t + _Seed));
                float shift = (bandNoise - 0.5) * 0.25 * step(0.55, bandNoise);
                float2 shifted = float2(uv.x + shift, uv.y);

                float inside = step(0.0, shifted.x) * step(shifted.x, 1.0);
                float2 edge = min(shifted, 1.0 - shifted);
                float frame = step(min(edge.x, edge.y), _Border) * inside;

                float blockNoise = Hash(floor(shifted * float2(4.0, _BandCount)) + float2(t, _Seed));
                float blocks = step(0.82, blockNoise) * inside * 0.55;

                float scan = 0.65 + 0.35 * sin(uv.y * 80.0 + _Time.y * 20.0);

                float split = 0.04;
                float a = step(abs(uv.x - 0.5 + shift - split), 0.5) * (frame + blocks);
                float b = step(abs(uv.x - 0.5 + shift + split), 0.5) * (frame + blocks);

                float flicker = 0.75 + 0.25 * Hash(float2(t, _Seed * 3.1));
                float mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                float coverage = saturate(a + b) * mask;
                float3 color = (_ColorA.rgb * a + _ColorB.rgb * b * 0.6) / max(a + b * 0.6, 0.001) * scan * flicker;
                return half4(color, coverage * _Intensity * input.color.a);
            }
            ENDHLSL
        }
    }
}
