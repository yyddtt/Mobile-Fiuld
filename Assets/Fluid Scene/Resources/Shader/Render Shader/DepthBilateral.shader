Shader "Hidden/DepthBilateral" {
    Properties {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _SigmaSpatial("SigmaSpatial", Float) = 5.0
        _SigmaRange("SigmaRange", Float) = 0.2
        _FilterRadius("FilterRadius", Int) = 5
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass {
            Name "Horizontal"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragH
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _SigmaSpatial;
            float _SigmaRange;
            int _FilterRadius;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            
            float4 fragH(v2f i):SV_Target{
                float c0 = tex2D(_MainTex, i.uv).r;
                float2 t = float2(_MainTex_TexelSize.x, 0);
                
                float wSum = 0;
                float vSum = 0;
                
                float kS = 1.0 / (2.0 * _SigmaSpatial * _SigmaSpatial + 1e-5);
                float kR = 1.0 / (2.0 * _SigmaRange * _SigmaRange + 1e-5);
                
                int radius = clamp(_FilterRadius, 1, 10);

                for(int r = -radius; r <= radius; r++) {
                    float val = tex2Dlod(_MainTex, float4(i.uv + r * t, 0, 0)).r;
                    
                    float diff = val - c0;
                    float dist = (float)r;
                    
                    float w = exp(-(dist*dist*kS) - (diff*diff*kR));
                    
                    vSum += val * w;
                    wSum += w;
                }
                
                float v = vSum / max(wSum, 1e-6);
                return float4(v,v,v,1);
            }
            ENDCG
        }
        Pass {
            Name "Vertical"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragV
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _SigmaSpatial;
            float _SigmaRange;
            int _FilterRadius;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_img v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            
            float4 fragV(v2f i):SV_Target{
                float c0 = tex2D(_MainTex, i.uv).r;
                float2 t = float2(0, _MainTex_TexelSize.y);
                
                float wSum = 0;
                float vSum = 0;
                
                float kS = 1.0 / (2.0 * _SigmaSpatial * _SigmaSpatial + 1e-5);
                float kR = 1.0 / (2.0 * _SigmaRange * _SigmaRange + 1e-5);
                
                int radius = clamp(_FilterRadius, 1, 10);

                for(int r = -radius; r <= radius; r++) {
                    float val = tex2Dlod(_MainTex, float4(i.uv + r * t, 0, 0)).r;
                    
                    float diff = val - c0;
                    float dist = (float)r;
                    
                    float w = exp(-(dist*dist*kS) - (diff*diff*kR));
                    
                    vSum += val * w;
                    wSum += w;
                }
                
                float v = vSum / max(wSum, 1e-6);
                return float4(v,v,v,1);
            }
            ENDCG
        }
    }
    FallBack Off
}
