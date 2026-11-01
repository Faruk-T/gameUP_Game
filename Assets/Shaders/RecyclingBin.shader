Shader "GameUP/Facility/RecyclingBin"
{
    Properties { _BaseMap("Artist Surface",2D)="white"{} _ContainerTint("Category Color",Color)=(.2,.4,.6,1) }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Tags {"LightMode"="UniversalForwardOnly"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;half4 _ContainerTint;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;float fog:TEXCOORD2;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 Frag(V i):SV_Target
            {
                half3 original=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half opening=1-smoothstep(.0005,.008,max(original.r,max(original.g,original.b)));
                half3 color=lerp(_ContainerTint.rgb,half3(.008,.012,.016),opening);
                Light sun=GetMainLight();half3 normal=normalize(i.normalWS);
                color*=min(sun.color,1.2)*(.25+.75*saturate(dot(normal,sun.direction)))+SampleSH(normal)*.35;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
