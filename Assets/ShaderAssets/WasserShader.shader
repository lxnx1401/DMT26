Shader "Custom/WasserShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Amplitude ("Wave Amplitude", Float) = 0.2
        _Frequency ("Wave Frequency", Float) = 2.0
        _Speed ("Wave Speed", Float) = 0.1
        _FoamDistance("Foam Distance", Float) = 0.3
        _FoamColor("Foam Color", Color) = (1,1,1,1) 
        _HeightMap("Height Map", 2D) = "black" {}
        _HeightStrength("Height Strength", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float waveNoise(float2 xz) {
                float n = sin(xz.x * 12.9898 + xz.y * 78.233) * 43758.5453;
                return (frac(n) * 2 - 1) * 0.5;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;

                float _Amplitude;
                float _Frequency;
                float _Speed;

                float _FoamDistance;
                half4 _FoamColor;

                float4 _HeightMap_ST;
                float _HeightStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Objektposition 
                float3 posOS = IN.positionOS.xyz;

                // Mittelpunkt (Plane Zentrum)
                float3 center = float3(0,0,0);

                // Abstand (XZ Ebene)
                float dist = distance(posOS.xz, center);
                
                // noise
                float n = waveNoise(posOS.xz);

                // Welle (RADIAL)
                float wave =
                    sin(dist * _Frequency + _Time.y * _Speed + n * 0.5)
                    * _Amplitude;

                // vertex nach oben verschieben
                posOS.y += wave;

                float2 heightUV = TRANSFORM_TEX(IN.uv, _HeightMap);
                heightUV += _Time.y * _Speed * 0.05;
                float heightSample = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, heightUV, 0).r;
                float heightOffset = (heightSample * 2.0 - 1.0) * _HeightStrength;
                posOS.y += heightOffset;

                float eps = 0.01;

                // Hilfspunkt links/rechts/vorne/hinten in Object Space berechnen
                float3 posL = IN.positionOS.xyz + float3(-eps, 0, 0);
                float3 posR = IN.positionOS.xyz + float3( eps, 0, 0);
                float3 posD = IN.positionOS.xyz + float3(0, 0, -eps);
                float3 posU = IN.positionOS.xyz + float3(0, 0,  eps);


                posL.y += sin(distance(posL.xz, center) * _Frequency + _Time.y * _Speed + waveNoise(posL.xz)) * _Amplitude;
                posR.y += sin(distance(posR.xz, center) * _Frequency + _Time.y * _Speed + waveNoise(posR.xz)) * _Amplitude;
                posD.y += sin(distance(posD.xz, center) * _Frequency + _Time.y * _Speed + waveNoise(posD.xz)) * _Amplitude;
                posU.y += sin(distance(posU.xz, center) * _Frequency + _Time.y * _Speed + waveNoise(posU.xz)) * _Amplitude;
                float3 tangentX = normalize(posR - posL);
                float3 tangentZ = normalize(posU - posD);
                float3 normalOS = normalize(cross(tangentZ, tangentX));

                OUT.normalWS = TransformObjectToWorldNormal(normalOS);

                OUT.positionHCS = TransformObjectToHClip(posOS);

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                OUT.positionWS = TransformObjectToWorld(posOS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {

                // Licht & View
                float3 lightDir = normalize(float3(0.3, 1.0, 0.2));
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);

                // Basis Wasserfarbe
                half3 deepColor    = float3(0.00, 0.18, 0.28);
                half3 shallowColor = float3(0.10, 0.60, 0.55);

                half3 waterColor = lerp(deepColor, shallowColor, 0.5);

                // Normal (noch fake)
                float3 normal = normalize(IN.normalWS);

                float fresnel = 1.0 - saturate(dot(viewDir, normal));
                fresnel = pow(fresnel, 4.0);

                // Specular Highlight
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), 128);
                half3 specColor = float3(1,1,1) * spec * 0.3;

                // Reflection Farbe (später Skybox)
                half3 reflectionColor = float3(1,1,1);

                // Fresnel blend (große Fläche)
                half3 color = lerp(waterColor, reflectionColor, fresnel);

                // specular oben drauf (kleine Highlights)
                color += specColor;

                //Schaum:
                float2 waterScreenUV = IN.positionHCS.xy / _ScreenParams.xy;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(waterScreenUV), _ZBufferParams);
                float waterDepth = IN.positionHCS.w;
                float depthDiff = sceneDepth - waterDepth;

                // Schaum: je näher am Boden/Insel, desto weißer
                float foam = 1.0 - saturate(depthDiff / _FoamDistance);
                foam = pow(foam, 2.0); // Kante schärfer machen

                // Transparenz: tief = opak, flach = transparenter
                float alpha = saturate(depthDiff / (_FoamDistance * 3.0));
                alpha = lerp(0.4, 1.0, alpha); // Minimum-Opacity damit Wasser sichtbar bleibt

                // Schaum auf Farbe addieren:
                color = lerp(color, _FoamColor.rgb, foam);

                return half4(color, alpha);

            }
            ENDHLSL
        }
    }
}
