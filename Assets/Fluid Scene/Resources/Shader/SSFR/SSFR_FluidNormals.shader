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

            float FixBg(float x, float c)
            {
                return (x > 500.0) ? c : x;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ts = _MainTex_TexelSize.xy;

                float d = tex2D(_MainTex, uv).r;

                // If background
                if (d > 500.0) return float4(0.5, 0.5, 1.0, 1.0); // Flat normal

                // 双尺度差分：近距离捕捉形状，略宽尺度压低深度噪声带来的「颗粒法线」
                float d_l  = FixBg(tex2D(_MainTex, uv + float2(-ts.x, 0)).r, d);
                float d_r  = FixBg(tex2D(_MainTex, uv + float2( ts.x, 0)).r, d);
                float d_d  = FixBg(tex2D(_MainTex, uv + float2(0, -ts.y)).r, d);
                float d_u  = FixBg(tex2D(_MainTex, uv + float2(0,  ts.y)).r, d);
                float d_ll = FixBg(tex2D(_MainTex, uv + float2(-2.0 * ts.x, 0)).r, d);
                float d_rr = FixBg(tex2D(_MainTex, uv + float2( 2.0 * ts.x, 0)).r, d);
                float d_dd = FixBg(tex2D(_MainTex, uv + float2(0, -2.0 * ts.y)).r, d);
                float d_uu = FixBg(tex2D(_MainTex, uv + float2(0,  2.0 * ts.y)).r, d);

                float dzdx_f = (d_r - d_l) * 0.5;
                float dzdy_f = (d_u - d_d) * 0.5;
                float dzdx_c = (d_rr - d_ll) * 0.25;
                float dzdy_c = (d_uu - d_dd) * 0.25;
                float dzdx = lerp(dzdx_c, dzdx_f, 0.55);
                float dzdy = lerp(dzdy_c, dzdy_f, 0.55);

                // 略低于原 100：减轻高频锯齿；仍由 _NormalStrength 控制观感
                float scale = 82.0 * _NormalStrength;

                float3 n = normalize(float3(-dzdx * scale, -dzdy * scale, 1.0));
                // 轻微朝视线方向收，避免薄液膜上法线过度摆动
                n = normalize(lerp(float3(0, 0, 1), n, 0.92));

                // Pack to 0..1 range
                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }
}
