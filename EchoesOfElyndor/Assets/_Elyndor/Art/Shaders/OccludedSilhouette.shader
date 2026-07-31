// Zeichnet nur dort, wo das Objekt von anderer Geometrie verdeckt ist
// (ZTest Greater) — als sanfte Silhouette, damit Aren unter Baumkronen
// und hinter Huegeln in der Top-Down-Ansicht sichtbar bleibt.
Shader "Elyndor/OccludedSilhouette"
{
    Properties
    {
        _SilhouetteColor("Silhouette Color", Color) = (0.45, 0.75, 0.95, 0.4)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OccludedSilhouette"

            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SilhouetteColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag() : SV_Target
            {
                return _SilhouetteColor;
            }
            ENDHLSL
        }
    }
}
