Shader "Hidden/GameUP/ProductUVBake"
{
    Properties { _OriginalTex("Original",2D)="white"{} _FrontPrint("Front",2D)="white"{} _BackPrint("Back",2D)="white"{} }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _OriginalTex, _FrontPrint, _BackPrint;
            float4 _OriginalST, _OriginalColor, _PrintCenter, _PrintSize;
            float4x4 _ToProduct, _NormalToProduct;
            float _Milk, _Yogurt, _Top, _PrintAllowed;
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 atlas:TEXCOORD1; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 productPosition:TEXCOORD1; float3 normal:TEXCOORD2; };
            Output Vert(Input v)
            {
                Output o; o.vertex=float4(v.atlas*2-1,0,1);
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y=-o.vertex.y;
                #endif
                o.uv=v.uv*_OriginalST.xy+_OriginalST.zw; o.productPosition=mul(_ToProduct,v.vertex).xyz;
                o.normal=mul((float3x3)_NormalToProduct,v.normal); return o;
            }
            fixed4 Frag(Output i):SV_Target
            {
                float3 normal=normalize(i.normal); float facing=_PrintSize.z>.5?normal.y:-normal.z; float3 d=i.productPosition-_PrintCenter.xyz;
                float2 uv=float2((facing>0?d.x:-d.x)/_PrintSize.x+.5,(_PrintSize.z>.5?d.z:d.y)/_PrintSize.y+.5);
                fixed4 color=tex2D(_OriginalTex,i.uv)*_OriginalColor;
                // The source yogurt lid carries a baked 07 DIC 2021 date. Remove it in source UV
                // space before projecting current campaign text, including surfaces outside the print card.
                if(_Yogurt>.5 && i.uv.x>.33 && i.uv.x<.67 && i.uv.y>.515 && i.uv.y<.58)
                    color=tex2D(_OriginalTex,float2(i.uv.x,.49))*_OriginalColor;
                if(_Milk>.5&&abs(facing)>.55&&i.productPosition.y>_Top-.11)color.rgb=float3(.95,.93,.84);
                if(_PrintAllowed>.5&&abs(facing)>.28&&all(uv>=0)&&all(uv<=1))color=facing>0?tex2D(_FrontPrint,uv):tex2D(_BackPrint,uv);
                return fixed4(color.rgb,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
