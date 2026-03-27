Shader "SSFR/FluidNormals"
{
    Properties
    {
        // We typically bind _FluidDepthTexture globally, but for Blit compatibility we might read _MainTex
        // In the C# script, if we do Blit(depth, dest, mat), depth becomes _MainTex.
        // But if we want to be safe, we can read _FluidDepthTexture directly if we are rendering a full screen quad manually.
        // Given the C# code: fluidCmd.Blit(depthTexID, normalTexID, normalMat);
        // This means _MainTex WILL be the depth texture.
        _MainTex ("Depth Texture", 2D) = "white" {}
        _NormalStrength ("Normal Strength", Float) = 1.0
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

            sampler2D _MainTex; // This is the Depth Texture passed via Blit
            float4 _MainTex_TexelSize;
            float _NormalStrength;

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

            float3 ReconstructViewPos(float2 uv, float depth)
            {
                // Assuming depth is Linear Eye Depth
                // To get actual 3D position, we need camera intrinsics, but for screen space normals,
                // we can often approximate using derivatives of depth.
                // Or simply:
                // We just need normals.
                return float3(uv.x, uv.y, depth); // Very rough approximation
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ts = _MainTex_TexelSize.xy;

                float d = tex2D(_MainTex, uv).r;

                // If background
                if (d > 500.0) return float4(0.5, 0.5, 1.0, 1.0); // Flat normal

                // Finite Difference / Sobel
                //  TL T TR
                //  L  C  R
                //  BL B BR

                float d_l = tex2D(_MainTex, uv + float2(-ts.x, 0)).r;
                float d_r = tex2D(_MainTex, uv + float2( ts.x, 0)).r;
                float d_d = tex2D(_MainTex, uv + float2(0, -ts.y)).r;
                float d_u = tex2D(_MainTex, uv + float2(0,  ts.y)).r;

                // Handle edges (if neighbor is background, use center depth)
                if (d_l > 500.0) d_l = d;
                if (d_r > 500.0) d_r = d;
                if (d_d > 500.0) d_d = d;
                if (d_u > 500.0) d_u = d;

                // Calculate derivatives
                // dx = change in depth per pixel X
                // dy = change in depth per pixel Y
                // We need to scale this to be meaningful. 
                // A large change in depth means the surface is slanted.
                
                // Scale factor: relates pixel size to world depth units.
                // This is the tricky "Magic Number" often.
                // But generally: Normal = normalize(-dx, -dy, 1)
                
                float dzdx = (d_r - d_l) * 0.5;
                float dzdy = (d_u - d_d) * 0.5;

                // Boost strength
                float scale = 100.0 * _NormalStrength; 

                float3 n = normalize(float3(-dzdx * scale, -dzdy * scale, 1.0));

                // Pack to 0..1 range
                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }
}
