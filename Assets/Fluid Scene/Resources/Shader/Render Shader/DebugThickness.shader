Shader "Fluid/DebugThickness"
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
            
            sampler2D _FluidThicknessTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                float d = tex2D(_FluidThicknessTexture, i.uv).r;
                // Visualize thickness: black = 0, white = high thickness
                // Multiply to make it more visible if values are small
                return fixed4(d, d, d, 1);
            }
            ENDCG
        }
    }
}
