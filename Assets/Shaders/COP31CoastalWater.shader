Shader "GameUP/COP31/StylizedWaterURP"
{
    Properties
    {
        _ShallowColor ("Shallow Mediterranean", Color) = (.12,.66,.66,1)
        _DeepColor ("Deep Mediterranean", Color) = (.025,.20,.33,1)
        _FoamColor ("Foam", Color) = (.85,.96,.92,1)
        _DistortionTexture ("Asset distortion", 2D) = "gray" {}
        _FoamTexture ("Asset foam", 2D) = "white" {}
        _WaveAmplitude ("Wave amplitude", Float) = .07
        _FoamWidth ("Shore foam depth", Float) = .65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "CoastalWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            TEXTURE2D(_DistortionTexture); SAMPLER(sampler_DistortionTexture);
            TEXTURE2D(_FoamTexture); SAMPLER(sampler_FoamTexture);
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor, _FoamColor;
                float _WaveAmplitude, _FoamWidth;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float eyeDepth : TEXCOORD1; float fog : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 p = o.positionWS.xz;
                o.positionWS.y += (sin(p.x*.37+_Time.y*.8)+sin(p.y*.52-_Time.y*.6)) * _WaveAmplitude*.5;
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.eyeDepth = -TransformWorldToView(o.positionWS).z;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = max(0, LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams) - input.eyeDepth);
                float2 uv = input.positionWS.xz;
                half2 distortion = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, uv*.06+_Time.y*float2(.018,.011)).rg-.5;
                half noise = SAMPLE_TEXTURE2D(_FoamTexture, sampler_FoamTexture, uv*.16+distortion*.15-_Time.y*.018).r;
                half3 tint = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate((input.positionWS.z-26)*.035));
                half3 refracted = SampleSceneColor(screenUV+distortion*.003*saturate(depth));
                half foam = (1-smoothstep(.04,_FoamWidth,depth)) * smoothstep(.35,.72,noise+sin(uv.y*2-_Time.y*1.4)*.12);
                half ripple = pow(saturate(sin(uv.x*1.8+distortion.x*4+_Time.y)*sin(uv.y*1.3-_Time.y*.6)), 14)*.07;
                half3 color = lerp(tint,refracted,.06*(1-saturate(depth*.5)))+ripple;
                color = lerp(color,_FoamColor.rgb,foam*.8);
                return half4(MixFog(color,input.fog),.95);
            }
            ENDHLSL
        }
    }
}
