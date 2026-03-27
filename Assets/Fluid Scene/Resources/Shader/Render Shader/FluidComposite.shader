Shader "Fluid/FluidComposite"
{
    Properties
    {
        _FluidDepthTexture ("Fluid Depth", 2D) = "white" {}
        _FluidThicknessTexture ("Fluid Thickness", 2D) = "black" {}
        _FluidNormalTexture ("Fluid Normal", 2D) = "bump" {}
        _FluidBackgroundTexture ("Background", 2D) = "white" {}
        _Color ("Fluid Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _IOR ("Refraction Index", Range(1.0, 3.0)) = 1.33
        _Absorption ("Absorption", Range(0.0, 10.0)) = 1.0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.9
        _Specular ("Specular", Range(0.0, 1.0)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

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
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex; // Background from Blit
            float4 _MainTex_TexelSize;
            
            sampler2D _FluidDepthTexture;
            sampler2D _FluidThicknessTexture;
            sampler2D _FluidNormalTexture;
            // sampler2D _FluidBackgroundTexture; // Replaced by _MainTex
            sampler2D _CameraDepthTexture;

            float4 _Color;
            float _IOR;
            float _Absorption;
            float _Smoothness;
            float _Specular;
            float _FresnelPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // Handle UV flip for Background (_MainTex) if needed
                // _ProjectionParams.x is -1 if projection is flipped
                // But Blit might handle it.
                // If the user says background is inverted, we might need to flip Y.
                // However, usually Blit(tex, dest) is correct.
                // If capturing from Active Render Target was upside down?
                // Let's assume _MainTex (Background) is correct or inverted.
                // We will add a manual flip check or rely on Blit.
                
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                
                // Sample Fluid Depth
                float fluidDepth = tex2D(_FluidDepthTexture, uv).r;
                
                // Check if background (infinite depth)
                if (fluidDepth > 900.0) discard;

                // Scene Depth (Linear Eye)
                float sceneDepthRaw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw);

                // Occlusion Test (Soft or Hard)
                if (fluidDepth > sceneDepth) discard;

                // Sample Normal
                float3 normal = tex2D(_FluidNormalTexture, uv).xyz * 2.0 - 1.0;
                normal = normalize(normal);

                // Sample Thickness
                float thickness = tex2D(_FluidThicknessTexture, uv).r;

                // Refraction
                // Simple distortion based on normal
                float2 offset = normal.xy * 0.05 * clamp(thickness, 0.0, 1.0); // Scale by thickness or fixed
                
                // Background Sampling
                // Use _MainTex which is passed via Blit(bgTexID, ...)
                // Check if we need to flip Y for background sampling
                float2 bgUV = i.uv + offset;
                
                // If rendering to screen, sometimes UVs are flipped relative to texture
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    bgUV.y = 1.0 - bgUV.y;
                #endif

                float3 bg = tex2D(_MainTex, bgUV).rgb;

                // Beer's Law for Absorption
                // T = exp(-absorption * thickness * distance)
                // We use thickness from map
                float3 transmission = exp(-_Color.rgb * _Absorption * thickness);
                
                // Fresnel
                // View Dir approximation
                float3 viewDir = normalize(float3(0,0,1)); // Simplified View Space View Dir
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Specular (Blinn-Phong)
                // Hardcoded Light Dir for now (Top-Right-Forward)
                float3 lightDir = normalize(float3(1, 1, -1)); 
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float specular = pow(NdotH, _Smoothness * 128.0) * _Specular;

                // Compose
                // Mix background with fluid color based on transmission
                // Add Specular and Fresnel
                float3 finalColor = bg * transmission + specular + fresnel * 0.1;
                
                return float4(finalColor, 1.0); // Alpha 1.0 as we blend manually or overwrite
            }
            ENDCG
        }
    }
}
