Shader "Hidden/ParticleThickness" {
    Properties {
        _DepthTex("DepthTex", 2D) = "black" {}
        _Threshold("DepthDiffThreshold", Float) = 0.01
        _Scale("ThicknessScale", Float) = 1.0
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
            float4 _DepthTex_TexelSize; // x = 1/width, y = 1/height
            float _Threshold;
            float _Scale;
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };
            v2f vert(appdata_img v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }
            float4 frag(v2f i) : SV_Target {
                float c = tex2D(_DepthTex, i.uv).r;
                float2 t = float2(_DepthTex_TexelSize.x, _DepthTex_TexelSize.y);
                float baseValid = step(1e-6, c);
                float th = max(_Threshold, 1e-6);
                float wSum = 0.0;
                float vSum = 0.0;
                float s0 = 0.4;
                float s1 = 0.25;
                float s2 = 0.1;
                float c0 = c;
                float cL  = tex2D(_DepthTex, i.uv + float2(-t.x, 0)).r;
                float cR  = tex2D(_DepthTex, i.uv + float2( t.x, 0)).r;
                float cU  = tex2D(_DepthTex, i.uv + float2(0, -t.y)).r;
                float cD  = tex2D(_DepthTex, i.uv + float2(0,  t.y)).r;
                float cLU = tex2D(_DepthTex, i.uv + float2(-t.x, -t.y)).r;
                float cLD = tex2D(_DepthTex, i.uv + float2(-t.x,  t.y)).r;
                float cRU = tex2D(_DepthTex, i.uv + float2( t.x, -t.y)).r;
                float cRD = tex2D(_DepthTex, i.uv + float2( t.x,  t.y)).r;
                float r0 = saturate(1.0 - abs(c0 - c) / th);
                wSum += s0 * r0; vSum += s0 * r0;
                float rL = saturate(1.0 - abs(cL - c) / th);
                float rR = saturate(1.0 - abs(cR - c) / th);
                float rU = saturate(1.0 - abs(cU - c) / th);
                float rD = saturate(1.0 - abs(cD - c) / th);
                wSum += s1 * (rL + rR + rU + rD);
                vSum += s1 * (rL + rR + rU + rD);
                float rLU = saturate(1.0 - abs(cLU - c) / th);
                float rLD = saturate(1.0 - abs(cLD - c) / th);
                float rRU = saturate(1.0 - abs(cRU - c) / th);
                float rRD = saturate(1.0 - abs(cRD - c) / th);
                wSum += s2 * (rLU + rLD + rRU + rRD);
                vSum += s2 * (rLU + rLD + rRU + rRD);
                float thickness = baseValid * saturate(vSum / max(wSum, 1e-6)) * _Scale;
                return float4(thickness, thickness, thickness, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
