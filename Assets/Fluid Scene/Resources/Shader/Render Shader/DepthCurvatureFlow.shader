Shader "Hidden/DepthCurvatureFlow" {
    Properties {
        _DepthTex("DepthTex", 2D) = "black" {}
        _Lambda("Lambda", Float) = 0.2
        _SigmaRange("SigmaRange", Float) = 0.03
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _DepthTex;
            float4 _DepthTex_TexelSize;
            float _Lambda;
            float _SigmaRange;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            float gauss(float x, float sigma){ return exp(-(x*x)/(2.0*sigma*sigma)); }
            float4 frag(v2f i):SV_Target{
                float c = tex2D(_DepthTex, i.uv).r;
                float2 t = float2(_DepthTex_TexelSize.x, _DepthTex_TexelSize.y);
                float L = tex2D(_DepthTex, i.uv + float2(-t.x, 0)).r;
                float R = tex2D(_DepthTex, i.uv + float2( t.x, 0)).r;
                float U = tex2D(_DepthTex, i.uv + float2(0, -t.y)).r;
                float D = tex2D(_DepthTex, i.uv + float2(0,  t.y)).r;
                float wL = gauss(abs(L - c), _SigmaRange);
                float wR = gauss(abs(R - c), _SigmaRange);
                float wU = gauss(abs(U - c), _SigmaRange);
                float wD = gauss(abs(D - c), _SigmaRange);
                float sumW = wL + wR + wU + wD + 1e-6;
                float lap = (wL*(L - c) + wR*(R - c) + wU*(U - c) + wD*(D - c)) / sumW;
                float next = c + _Lambda * lap;
                next = saturate(next);
                return float4(next, next, next, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
