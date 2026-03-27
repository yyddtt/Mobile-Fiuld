Shader "Fluid/DebugDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _FluidDepthTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                float d = tex2D(_FluidDepthTexture, i.uv).r;
                
                // If depth is very large (background), show black/dark blue
                if (d > 900.0) return fixed4(0, 0, 0.2, 1);
                
                // Show depth with some contours to verify shape
                // Wrap every 1 meter
                float wave = frac(d);
                return fixed4(wave, wave, wave, 1);
            }
            ENDCG
        }
    }
}
