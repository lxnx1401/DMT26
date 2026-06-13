Shader "DMT/Terrain Projected Hex Overlay"
{
    Properties
    {
        _Color ("Tile Color", Color) = (0.18, 0.78, 0.95, 1)
        _HoverColor ("Hover Color", Color) = (0.55, 0.95, 1, 1)
        _SelectedColor ("Selected Color", Color) = (1, 0.92, 0.18, 1)
        _EdgeColor ("Edge Color", Color) = (0.8, 1, 1, 1)
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.32
        _HoverAlphaBoost ("Hover Alpha Boost", Range(1, 4)) = 2.15
        _SelectedAlphaBoost ("Selected Alpha Boost", Range(1, 4)) = 2.75
        _InnerFade ("Inner Fade Start", Range(0, 1)) = 0.18
        _OuterFade ("Outer Fade End", Range(0, 1)) = 0.88
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.13
        _EdgeStrength ("Edge Color Strength", Range(0, 1)) = 0.65
        _EdgeAlphaBoost ("Edge Alpha Boost", Range(0, 1)) = 0.25
        [HideInInspector] _Hover ("Hover", Range(0, 1)) = 0
        [HideInInspector] _Selected ("Selected", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "HexTerrainOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _HoverColor;
                float4 _SelectedColor;
                float4 _EdgeColor;
                float _BaseAlpha;
                float _HoverAlphaBoost;
                float _SelectedAlphaBoost;
                float _InnerFade;
                float _OuterFade;
                float _EdgeWidth;
                float _EdgeStrength;
                float _EdgeAlphaBoost;
                float _Hover;
                float _Selected;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // UV (0,0) is the tile center. Values near 1 are near the edge.
                float distanceFromCenter = saturate(length(input.uv));

                // The middle stays subtle, the outside becomes more visible.
                float inwardFalloff = smoothstep(_InnerFade, _OuterFade, distanceFromCenter);

                // A narrow extra band keeps the hex boundary readable.
                float edgeStart = saturate(1.0 - _EdgeWidth);
                float edgeMask = smoothstep(edgeStart, 1.0, distanceFromCenter);

                float hover = saturate(_Hover);
                float selected = saturate(_Selected);
                float hoverAlpha = lerp(1.0, _HoverAlphaBoost, hover);
                float selectedAlpha = lerp(1.0, _SelectedAlphaBoost, selected);
                float interactionAlpha = max(hoverAlpha, selectedAlpha);

                float alpha = _BaseAlpha * inwardFalloff * interactionAlpha;
                alpha += edgeMask * _BaseAlpha * _EdgeAlphaBoost * interactionAlpha;
                alpha = saturate(alpha);

                float3 tileColor = lerp(_Color.rgb, _HoverColor.rgb, hover * 0.55);
                tileColor = lerp(tileColor, _SelectedColor.rgb, selected * 0.75);
                tileColor = lerp(tileColor, _EdgeColor.rgb, edgeMask * _EdgeStrength);

                return half4(tileColor, alpha);
            }
            ENDHLSL
        }
    }
}
