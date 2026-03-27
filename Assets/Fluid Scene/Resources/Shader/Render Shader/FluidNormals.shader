Shader "Fluid/FluidNormals" {
    Properties {
        _FluidDepthTexture("FluidDepthTexture", 2D) = "white" {}
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
            
            sampler2D _MainTex; // Was _FluidDepthTexture
            float4 _MainTex_TexelSize;
            float _NormalStrength;
            
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            
            float4 frag(v2f i):SV_Target{
                float2 t = _MainTex_TexelSize.xy;
                
                // Sample depth neighborhood
                // 00 10 20
                // 01 11 21
                // 02 12 22
                
                float d00 = tex2D(_MainTex, i.uv + float2(-t.x, -t.y)).r;
                float d10 = tex2D(_MainTex, i.uv + float2( 0.0, -t.y)).r;
                float d20 = tex2D(_MainTex, i.uv + float2( t.x, -t.y)).r;
                
                float d01 = tex2D(_MainTex, i.uv + float2(-t.x,  0.0)).r;
                float d11 = tex2D(_MainTex, i.uv).r;
                float d21 = tex2D(_MainTex, i.uv + float2( t.x,  0.0)).r;
                
                float d02 = tex2D(_MainTex, i.uv + float2(-t.x,  t.y)).r;
                float d12 = tex2D(_MainTex, i.uv + float2( 0.0,  t.y)).r;
                float d22 = tex2D(_MainTex, i.uv + float2( t.x,  t.y)).r;
                
                // Check for background (very large depth)
                if (d11 > 900.0) return float4(0, 0, 0, 1); // Black for background

                // Clamp neighbors to d11 if they are background
                float maxD = 900.0;
                if(d00 > maxD) d00 = d11;
                if(d10 > maxD) d10 = d11;
                if(d20 > maxD) d20 = d11;
                if(d01 > maxD) d01 = d11;
                if(d21 > maxD) d21 = d11;
                if(d02 > maxD) d02 = d11;
                if(d12 > maxD) d12 = d11;
                if(d22 > maxD) d22 = d11;
                
                // Sobel filter
                float dx = (d20 + 2.0*d21 + d22) - (d00 + 2.0*d01 + d02);
                float dy = (d02 + 2.0*d12 + d22) - (d00 + 2.0*d10 + d20);
                
                // Boost the gradients!
                // Linear Eye Depth is in world units (e.g. meters).
                // UVs are 0..1.
                // We need to relate change in Depth to change in Screen Position.
                // A simple boost factor helps visualize the normals.
                float scale = 100.0; 

                float3 n = normalize(float3(-dx * _NormalStrength * scale, -dy * _NormalStrength * scale, 1.0));

                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
