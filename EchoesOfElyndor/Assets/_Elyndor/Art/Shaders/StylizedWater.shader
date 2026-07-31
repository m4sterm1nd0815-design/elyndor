// Stilisiertes Wasser: sanftes Auf und Ab, wandernde Glanzstreifen,
// transparent mit Nebel-Anbindung. Bewusst ohne Depth-Effekte —
// Uferschaum folgt in einem spaeteren Pass.
Shader "Elyndor/StylizedWater"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.25, 0.34, 0.4, 0.82)
        _RippleColor("Ripple Color", Color) = (0.55, 0.68, 0.72, 1)
        _WaveHeight("Wave Height", Range(0, 0.3)) = 0.05
        _WaveSpeed("Wave Speed", Range(0, 4)) = 1.1
        _RippleScale("Ripple Scale", Range(0, 4)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RippleColor;
                half _WaveHeight;
                half _WaveSpeed;
                half _RippleScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.y += sin(_Time.y * _WaveSpeed + positionWS.x * 0.6 +
                                    positionWS.z * 0.4) * _WaveHeight;

                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;

                // Zwei ueberlagerte Streifenmuster ergeben wanderndes Glitzern.
                half ripple =
                    sin(input.positionWS.x * _RippleScale * 2.1 + t) *
                    sin(input.positionWS.z * _RippleScale * 1.7 - t * 0.8);
                half rippleMask = saturate(ripple - 0.55) * 2.2h;

                half3 color = lerp(_BaseColor.rgb, _RippleColor.rgb, rippleMask);
                color = MixFog(color, input.fogFactor);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
