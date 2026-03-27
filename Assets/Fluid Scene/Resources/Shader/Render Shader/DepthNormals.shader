Shader "Hidden/DepthNormals" {
    Properties {
        _DepthTex("DepthTex", 2D) = "black" {}
        _NormalStrength("NormalStrength", Float) = 6.0
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
            float _NormalStrength;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            float4 frag(v2f i):SV_Target{
                float2 t = float2(_DepthTex_TexelSize.x, _DepthTex_TexelSize.y);
                float farClip = _ProjectionParams.z;
                float d00 = tex2D(_DepthTex, i.uv + float2(-t.x, -t.y)).r * farClip;
                float d10 = tex2D(_DepthTex, i.uv + float2( 0.0, -t.y)).r * farClip;
                float d20 = tex2D(_DepthTex, i.uv + float2( t.x, -t.y)).r * farClip;
                float d01 = tex2D(_DepthTex, i.uv + float2(-t.x,  0.0)).r * farClip;
                float d11 = tex2D(_DepthTex, i.uv).r * farClip;
                float d21 = tex2D(_DepthTex, i.uv + float2( t.x,  0.0)).r * farClip;
                float d02 = tex2D(_DepthTex, i.uv + float2(-t.x,  t.y)).r * farClip;
                float d12 = tex2D(_DepthTex, i.uv + float2( 0.0,  t.y)).r * farClip;
                float d22 = tex2D(_DepthTex, i.uv + float2( t.x,  t.y)).r * farClip;
                float dx = (d20 + 2.0*d21 + d22) - (d00 + 2.0*d01 + d02);
                float dy = (d02 + 2.0*d12 + d22) - (d00 + 2.0*d10 + d20);
                float3 n = normalize(float3(-dx*_NormalStrength, -dy*_NormalStrength, 1.0));
                float3 enc = n*0.5 + 0.5;
                return float4(enc,1);
            }
            ENDCG
        }
    }
    FallBack Off
}
