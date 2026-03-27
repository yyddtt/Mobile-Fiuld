Shader "Fluid/DebugNormal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = tex2D(_MainTex, i.uv).rgb;
                // n is in [0,1], representing [-1,1]
                // Just display it as color
                // Map [0,1] back to [-1,1] for visualization? No, just view raw color.
                // If normal is (0,0,1) -> (0.5, 0.5, 1) -> Purple.
                // If normal is flat, it should be purple.
                return fixed4(n, 1);
            }
            ENDCG
        }
    }
}
