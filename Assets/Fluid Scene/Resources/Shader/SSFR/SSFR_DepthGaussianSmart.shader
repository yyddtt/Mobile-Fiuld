Shader "SSFR/DepthGaussianSmart"
{
    Properties
    {
        _MainTex ("Depth Texture", 2D) = "white" {}
        _SigmaSpatial ("Spatial Sigma", Float) = 5.0
        _SigmaRange ("Range Sigma", Float) = 0.2
        _FilterRadius ("Filter Radius", Int) = 5
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    float _SigmaSpatial;
    float _SigmaRange;
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

    // Gaussian Blur with Depth Masking (Smart Gaussian)
    // dir: (1,0) or (0,1)
    float Blur(v2f i, float2 dir)
    {
        float2 uv = i.uv;
        float centerDepth = tex2D(_MainTex, uv).r;

        // Skip background pixels
        if (centerDepth > 500.0) return centerDepth;

        float sum = 0.0;
        float wSum = 0.0;

        // Use a fixed max radius for loop unrolling
        #define MAX_RADIUS 30
        
        // We clamp the radius from property to the max supported
        int r_limit = clamp(_FilterRadius, 1, MAX_RADIUS);

        float twoSigmaSpatial2 = 2.0 * _SigmaSpatial * _SigmaSpatial;
        float twoSigmaRange2 = 2.0 * _SigmaRange * _SigmaRange;

        for (int r = -MAX_RADIUS; r <= MAX_RADIUS; r++)
        {
            // Dynamic branch to skip invalid iterations
            if (abs(r) > r_limit) continue;

            float2 offset = dir * r * _MainTex_TexelSize.xy;
            float sampleDepth = tex2D(_MainTex, uv + offset).r;

            // CRITICAL: Ignore background samples to prevent bleeding
            // This makes it a "Smart" Gaussian that only blurs fluid with fluid
            if (sampleDepth > 500.0) continue;

            // Spatial weight (Gaussian)
            float sW = exp(-(r * r) / twoSigmaSpatial2);

            // Range weight (Edge Preservation)
            // Even in Gaussian mode, we need this to prevent blurring across distinct edges
            float diff = sampleDepth - centerDepth;
            float rW = exp(-(diff * diff) / twoSigmaRange2);

            float weight = sW * rW;

            sum += sampleDepth * weight;
            wSum += weight;
        }

        if (wSum > 0.0)
            return sum / wSum;
        else
            return centerDepth;
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
            
            float frag (v2f i) : SV_Target
            {
                return Blur(i, float2(1, 0));
            }
            ENDCG
        }

        // Pass 1: Vertical
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            float frag (v2f i) : SV_Target
            {
                return Blur(i, float2(0, 1));
            }
            ENDCG
        }
    }
}
