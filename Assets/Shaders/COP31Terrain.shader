Shader "GameUP/COP31/Terrain"
{
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
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;half4 color:COLOR;float fog:TEXCOORD2;};
            V Vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
            half4 Frag(V i):SV_Target
            {
                Light sun=GetMainLight();half3 normal=normalize(i.normalWS);
                half shade=.24+.76*saturate(dot(normal,sun.direction));
                half grain=frac(sin(dot(floor(i.positionWS.xz*1.8),float2(12.9898,78.233)))*43758.5453);
                half3 color=i.color.rgb*(min(sun.color,1.3)*shade+SampleSH(normal)*.2)*(1+grain*.06);
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
