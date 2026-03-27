// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Hidden/FluidCompositeMPM"
{
    Properties
    {
        _MainTex ("Background", 2D) = "white" {}
        _Color ("Fluid Color", Color) = (0.2, 0.5, 1.0, 1.0)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9
        _IOR ("Refraction Index", Range(1, 2)) = 1.33
        _RefractionStrength ("Refraction Strength", Range(0, 0.5)) = 0.05
        _Absorption ("Absorption", Range(0, 10)) = 2.0
        _SpecularPower ("Specular Power", Range(1, 100)) = 50.0
        _NormalScale ("Normal Scale", Range(1, 500)) = 150.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            sampler2D _SSDepthTex; // Fluid Depth (Linear or Raw?) Let's assume Linear if we processed it, or Raw.
            float4 _SSDepthTex_TexelSize;
            
            sampler2D _FluidThicknessTex;
            sampler2D _CameraDepthTexture; // Scene Depth

            float4 _Color;
            float _Smoothness;
            float _IOR;
            float _RefractionStrength;
            float _Absorption;
            float _SpecularPower;
            float _NormalScale;
            float _FresnelPower;

            // float4x4 _CameraToWorld;
            
            // Reconstruct View Position from Depth
            float3 ReconstructViewPos(float2 uv, float depth)
            {
                // Simple reconstruction assuming standard projection
                // uv is 0..1, depth is 0..1 (LinearEyeDepth)
                // We need to unproject.
                // Or simpler: viewPos.z = depth.
                // viewPos.xy = (uv * 2 - 1) * depth * scale
                
                // Let's rely on screen space normals from derivatives for now, 
                // which is robust enough for SSFR without full matrix inversion
                return float3(uv, depth); 
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // Fix for Inverted UVs on some platforms/pipelines
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.uv.y = 1 - o.uv.y;
                #endif

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // 1. Sample Scene Depth
                // Note: _CameraDepthTexture might be flipped relative to i.uv depending on platform
                float2 depthUV = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    depthUV.y = 1 - depthUV.y;
                #endif

                float sceneDepthRaw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, depthUV);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw);

                // 2. Sample Fluid Depth
                // Our SSDepthTex is a RenderTexture we Blitted to, so it matches _MainTex orientation usually
                // But let's verify. Since we used Graphics.Blit to generate it, it should match.
                float fluidDepth = tex2D(_SSDepthTex, i.uv).r;
                
                // Check if there is fluid here (using a threshold or background value)
                // We initialized depth to a large value (e.g., 1000)
                if (fluidDepth > 900.0) // No fluid
                {
                    return tex2D(_MainTex, i.uv);
                }

                // Occlusion Test: If fluid is behind scene object, show scene
                // Add small bias to prevent z-fighting
                if (fluidDepth > sceneDepth + 0.01)
                {
                    return tex2D(_MainTex, i.uv);
                }

                // 3. Calculate Normal from Depth
                float d0 = fluidDepth;
                float2 uvOffset = _SSDepthTex_TexelSize.xy * 2.0;
                float d1 = tex2D(_SSDepthTex, i.uv + float2(uvOffset.x, 0)).r;
                float d2 = tex2D(_SSDepthTex, i.uv - float2(uvOffset.x, 0)).r;
                float d3 = tex2D(_SSDepthTex, i.uv + float2(0, uvOffset.y)).r;
                float d4 = tex2D(_SSDepthTex, i.uv - float2(0, uvOffset.y)).r;
                
                // Fix: Clamp background samples to center depth to prevent harsh white outlines
                float bgThresh = 800.0;
                if (d1 > bgThresh) d1 = d0;
                if (d2 > bgThresh) d2 = d0;
                if (d3 > bgThresh) d3 = d0;
                if (d4 > bgThresh) d4 = d0;

                // View space normal approximation
                // dz/dx ~ (d1 - d2), dz/dy ~ (d3 - d4)
                float dzdx = (d1 - d2);
                float dzdy = (d3 - d4);
                
                // _NormalScale controls how "flat" the surface is. Higher = Flatter.
                float3 normal = normalize(float3(-dzdx, -dzdy, _SSDepthTex_TexelSize.x * _NormalScale));
                
                // 4. Refraction
                float2 refractUV = i.uv + normal.xy * _RefractionStrength;
                float3 bg = tex2D(_MainTex, refractUV).rgb;

                // 5. Absorption (Beer's Law)
                float thickness = tex2D(_FluidThicknessTex, i.uv).r;
                float3 transmittance = exp(-_Absorption * thickness * (1.0 - _Color.rgb));
                // 6. Specular Highlight

                // 6. Specular Highlight & Reflection
                float3 lightDir = normalize(float3(0.5, 1.0, -0.5)); // Fixed light direction
                float3 viewDir = float3(0, 0, 1); // Approximate view dir in tangent/view space
                float3 halfVec = normalize(lightDir + viewDir); // Blinn-Phong
                
                float NdotH = max(0.0, dot(normal, halfVec));
                float spec = pow(NdotH, _SpecularPower);

                // 7. Fresnel Edge Highlight
                float NdotV = max(0.0, dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                // Mask Fresnel by thickness to prevent white outline on thin edges
                float edgeMask = saturate(smoothstep(0.04, 0.20, thickness)); 
                fresnel *= edgeMask;
                spec *= edgeMask;
                
                // Combine: Background (refracted) + Specular + Fresnel
                float3 fluidColor = _Color.rgb;
                float3 lightColor = float3(1.0, 1.0, 1.0);
                
                // Apply Beer's law (transmittance) to background
                float3 finalColor = bg * transmittance;
                
                // Add Specular and Fresnel
                finalColor += spec * lightColor;
                finalColor += fresnel * fluidColor * 0.5; // Add glowing edge
                
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
