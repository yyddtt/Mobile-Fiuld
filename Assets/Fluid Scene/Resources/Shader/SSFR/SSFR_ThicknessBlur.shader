Shader "SSFR/ThicknessBlur"
{
    Properties
    {
        _MainTex ("Thickness Texture", 2D) = "black" {}
        _FilterRadius ("Filter Radius", Int) = 5
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    int _FilterRadius;

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

    float Blur(v2f i, float2 dir)
    {
        float2 uv = i.uv;
        float sum = 0.0;
        float wSum = 0.0;
        
        #define MAX_RADIUS 30
        int r_limit = clamp(_FilterRadius, 1, MAX_RADIUS);

        float sigma = float(r_limit) / 2.0;
        if (sigma < 0.1) sigma = 0.1;
        float twoSigma2 = 2.0 * sigma * sigma;

        for (int r = -MAX_RADIUS; r <= MAX_RADIUS; r++)
        {
            if (abs(r) > r_limit) continue;

            float2 offset = dir * r * _MainTex_TexelSize.xy;
            float val = tex2D(_MainTex, uv + offset).r;

            float weight = exp(-(r * r) / twoSigma2);
            
            sum += val * weight;
            wSum += weight;
        }

        return sum / wSum;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Horizontal
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float frag (v2f i) : SV_Target { return Blur(i, float2(1, 0)); }
            ENDCG
        }

        // Pass 1: Vertical
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float frag (v2f i) : SV_Target { return Blur(i, float2(0, 1)); }
            ENDCG
        }
    }
}
