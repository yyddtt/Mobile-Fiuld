Shader "Hidden/ThicknessBlur" {
    Properties {
        _MainTex("MainTex", 2D) = "black" {}
        _Weights("Weights", Vector) = (0.15,0.4,0.15,0.15)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragH
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Weights;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            float4 fragH(v2f i):SV_Target{
                float2 t = float2(_MainTex_TexelSize.x, 0);
                float c0 = tex2D(_MainTex, i.uv).r;
                float cL1 = tex2D(_MainTex, i.uv - t).r;
                float cR1 = tex2D(_MainTex, i.uv + t).r;
                float cL2 = tex2D(_MainTex, i.uv - 2*t).r;
                float cR2 = tex2D(_MainTex, i.uv + 2*t).r;
                float w0 = _Weights.y;
                float w1 = _Weights.x;
                float w2 = _Weights.z;
                float w3 = _Weights.w;
                float v = w0*c0 + w1*cL1 + w1*cR1 + w2*cL2 + w3*cR2;
                return float4(v,v,v,1);
            }
            ENDCG
        }
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragV
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Weights;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            float4 fragV(v2f i):SV_Target{
                float2 t = float2(0, _MainTex_TexelSize.y);
                float c0 = tex2D(_MainTex, i.uv).r;
                float cU1 = tex2D(_MainTex, i.uv - t).r;
                float cD1 = tex2D(_MainTex, i.uv + t).r;
                float cU2 = tex2D(_MainTex, i.uv - 2*t).r;
                float cD2 = tex2D(_MainTex, i.uv + 2*t).r;
                float w0 = _Weights.y;
                float w1 = _Weights.x;
                float w2 = _Weights.z;
                float w3 = _Weights.w;
                float v = w0*c0 + w1*cU1 + w1*cD1 + w2*cU2 + w3*cD2;
                return float4(v,v,v,1);
            }
            ENDCG
        }
    }
    FallBack Off
}
