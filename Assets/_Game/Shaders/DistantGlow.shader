
Shader "Custom/DistantGlow"
{
    Properties
    {
        _MainTex("Glow Texture", 2D) = "white" {}
        [HDR] _Color("Glow Color", Color) = (1, 0.45, 0.1, 1)
        _Intensity("Emission Intensity", Range(0, 20)) = 4
    }

        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "Queue" = "Transparent"
                "RenderType" = "Transparent"
            }

            Pass
            {
                Blend SrcAlpha One
                ZWrite Off
                ZTest LEqual
                Cull Off

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                CBUFFER_START(UnityPerMaterial)
                    float4 _Color;
                    float _Intensity;
                CBUFFER_END

                Varyings vert(Attributes input)
                {
                    Varyings output;
                    output.positionHCS =
                        TransformObjectToHClip(input.positionOS.xyz);
                    output.uv = input.uv;
                    return output;
                }

                half4 frag(Varyings input) : SV_Target
                {
                    half4 tex = SAMPLE_TEXTURE2D(
                        _MainTex, sampler_MainTex, input.uv);

                    half alpha = tex.a * _Color.a;
                    half3 rgb = tex.rgb * _Color.rgb * _Intensity;

                    return half4(rgb, alpha);
                }
                ENDHLSL
            }
        }
}
