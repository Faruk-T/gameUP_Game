Shader "GameUP/COP31/ParticleGlow"
{
    Properties { [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination blend",Float)=1 _Intensity("Glow intensity",Float)=1 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half _Intensity;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                half glow=pow(saturate(1-length(i.uv-.5)*2),1.5);
                return half4(i.color.rgb*_Intensity,i.color.a*glow);
            }
            ENDHLSL
        }
    }
}
