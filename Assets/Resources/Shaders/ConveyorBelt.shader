Shader "ElectroGrid/ConveyorBelt"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (0.4, 0.9, 2.2, 1)
        _Cells ("Cells", Float) = 8
        _Direction ("Direction", Float) = 1
        _Speed ("Speed", Float) = 0.6
        _ChevronsPerCell ("Chevrons Per Cell", Float) = 2
        _RailAlpha ("Rails", Range(0, 1)) = 0.8
        _ChevronAlpha ("Chevrons", Range(0, 1)) = 0.35
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
                float _Cells;
                float _Direction;
                float _Speed;
                float _ChevronsPerCell;
                float _RailAlpha;
                float _ChevronAlpha;
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
                float2 uv = input.uv;
                float rails = step(uv.y, 0.035) + step(0.965, uv.y);

                float x = uv.x * _Cells * _ChevronsPerCell - fmod(_Time.y, 3600.0) * _Speed * _ChevronsPerCell * _Direction;
                float local = frac(x);
                float y = abs(uv.y - 0.5);
                float chevronX = _Direction > 0 ? local + y : (1.0 - local) + y;
                float chevron = step(0.45, chevronX) * step(chevronX, 0.62) * step(y, 0.3);

                float fadeEnds = smoothstep(0.0, 0.5 / _Cells, uv.x) * smoothstep(0.0, 0.5 / _Cells, 1.0 - uv.x);
                float3 color = _Color.rgb * (rails * _RailAlpha + chevron * _ChevronAlpha * fadeEnds);
                return half4(color * input.color.a, 1);
            }
            ENDHLSL
        }
    }
}
