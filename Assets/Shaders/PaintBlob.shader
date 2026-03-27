Shader "Custom/PaintBlob"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0, 0, 1)
        _Glossy ("Glossiness", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 posOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Glossy;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posWS = TransformObjectToWorld(IN.posOS.xyz);
                OUT.posCS = TransformWorldToHClip(OUT.posWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(OUT.posWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 N = normalize(IN.normalWS);
                float3 L = mainLight.direction;
                float3 V = normalize(IN.viewDirWS);
                float3 H = normalize(L + V);

                // 확산 조명
                float NdotL = saturate(dot(N, L));
                float3 diffuse = _Color.rgb * (0.35 + 0.65 * NdotL);

                // 반사 광택
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, 64.0) * _Glossy;
                float3 specular = mainLight.color * spec;

                float3 result = (diffuse + specular) * mainLight.color;
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
