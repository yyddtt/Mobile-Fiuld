Shader "Hidden/ThicknessNormals" {
    Properties {
        _ThicknessTex("ThicknessTex", 2D) = "black" {}
        _NormalStrength("NormalStrength", Float) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _ThicknessTex;
            float4 _ThicknessTex_TexelSize;
            float _NormalStrength;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            float4 frag(v2f i):SV_Target{
                float2 t = float2(_ThicknessTex_TexelSize.x, _ThicknessTex_TexelSize.y);
                float c = tex2D(_ThicknessTex, i.uv).r;
                float cx1 = tex2D(_ThicknessTex, i.uv + float2(t.x,0)).r;
                float cx2 = tex2D(_ThicknessTex, i.uv - float2(t.x,0)).r;
                float cy1 = tex2D(_ThicknessTex, i.uv + float2(0,t.y)).r;
                float cy2 = tex2D(_ThicknessTex, i.uv - float2(0,t.y)).r;
                float dx = (cx1 - cx2) * 0.5;
                float dy = (cy1 - cy2) * 0.5;
                float3 n = normalize(float3(-dx*_NormalStrength, -dy*_NormalStrength, 1.0));
                float3 enc = n*0.5 + 0.5;
                return float4(enc,1);
            }
            ENDCG
        }
    }
    FallBack Off
}
