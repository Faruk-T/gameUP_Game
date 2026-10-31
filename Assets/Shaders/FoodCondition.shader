Shader "GameUP/FoodCondition"
{
    Properties
    {
        _BaseMap("Original texture",2D)="white"{}
        _BaseColor("Original color",Color)=(1,1,1,1)
        _Spoilage("Spoilage",Range(0,1))=1
        _SpoilTint("Mold and discoloration",Color)=(.25,.3,.1,1)
        _Swell("Swelling",Range(0,.3))=.03
        _BoundsMin("Mesh bounds minimum",Vector)=(0,0,0,0)
        _BoundsSize("Mesh bounds size",Vector)=(1,1,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _SpoilTint, _BoundsMin, _BoundsSize;
            float _Spoilage, _Swell;
        CBUFFER_END
        float3 UnitPosition(float3 p) { return (p-_BoundsMin.xyz)/max(_BoundsSize.xyz,float3(.0001,.0001,.0001)); }
        float3 Deform(float3 p)
        {
            float3 u=UnitPosition(p);
            float bulge=sin(saturate(u.y)*3.14159265)*_Swell*_Spoilage;
            p.xz+=(u.xz-.5)*_BoundsSize.xz*bulge;
            p.y+=pow(saturate(u.y),5)*_BoundsSize.y*_Swell*_Spoilage*.35;
            return p;
        }
        float Hash(float3 p) { p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z); }
        float Noise(float3 p)
        {
            float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(lerp(Hash(i),Hash(i+float3(1,0,0)),f.x),lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x),f.y),
                lerp(lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x),lerp(Hash(i+float3(0,1,1)),Hash(i+float3(1,1,1)),f.x),f.y),f.z);
        }
        struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0; };
        ENDHLSL
        Pass
        {
            Name "FoodForward" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;float3 unitPosition:TEXCOORD3;};
            Varyings Vert(Attributes input)
            {
                Varyings output;float3 p=Deform(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(p);output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);output.uv=TRANSFORM_TEX(input.uv,_BaseMap);output.unitPosition=UnitPosition(input.positionOS.xyz);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb;
                float patches=Noise(input.unitPosition*8)+Noise(input.unitPosition*29)*.24;
                float stain=smoothstep(.43,.74,patches)*_Spoilage;
                half gray=dot(color,half3(.2126,.7152,.0722));
                color=lerp(color,gray*.58+_SpoilTint.rgb*.28,_Spoilage*.65);
                color=lerp(color,_SpoilTint.rgb,stain*.88);
                color=lerp(color,half3(.73,.76,.56),smoothstep(.76,.9,patches)*_Spoilage*.75);
                half3 normal=normalize(input.normalWS);Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 illumination=SampleSH(normal)+light.color*saturate(dot(normal,light.direction))*light.shadowAttenuation*light.distanceAttenuation;
                return half4(color*illumination,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection, _LightPosition;
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                float3 p=TransformObjectToWorld(Deform(input.positionOS.xyz));float3 n=TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 direction=normalize(_LightPosition-p);
                #else
                    float3 direction=_LightDirection;
                #endif
                float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,direction));
                #if UNITY_REVERSED_Z
                    clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE);
                #else
                    clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return clip;
            }
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(Attributes input):SV_POSITION{return TransformObjectToHClip(Deform(input.positionOS.xyz));}
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
    FallBack Off
}
