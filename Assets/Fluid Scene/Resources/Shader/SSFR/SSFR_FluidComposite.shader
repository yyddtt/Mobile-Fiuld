Shader "SSFR/FluidComposite"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {} 
        _Color ("Fluid Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _Absorption ("Absorption", Float) = 1.0
        _Smoothness ("Smoothness", Float) = 0.9
        _Specular ("Specular", Float) = 0.5
        _ThicknessCutoff ("Thickness Cutoff", Float) = 0.05
        _RefractionStrength ("Refraction Strength", Float) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        Pass
        {
            ZWrite Off
            ZTest Always
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

            sampler2D _FluidDepthTexture;
            sampler2D _FluidThicknessTexture;
            sampler2D _FluidNormalTexture;
            float4 _FluidNormalTexture_TexelSize;
            sampler2D _CameraDepthTexture; 

            float4 _Color;
            float _Absorption;
            float _Smoothness;
            float _Specular;
            float _ThicknessCutoff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // --- BACKGROUND SAMPLING ---
                // Background comes from CommandBuffer.Blit(CurrentActive, tempRT). Its V orientation
                // relative to the fullscreen Blit UVs is indicated by _MainTex_TexelSize.y (Unity
                // convention). Unconditional flips under UNITY_UV_STARTS_AT_TOP break after some
                // Unity / graphics API / HDR paths and misalign refraction with fluid depth.
                float2 bgUV = uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    bgUV.y = 1.0 - bgUV.y;
                #endif

                // --- FLUID DATA SAMPLING ---
                // Fluid textures (Depth, Thickness, Normal) are RenderTextures generated
                // in the same pipeline, so they usually match the screen UVs (0..1).
                // If the fluid is "upright", we don't flip these.
                
                float fluidDepth = tex2D(_FluidDepthTexture, uv).r;
                float thickness = tex2D(_FluidThicknessTexture, uv).r;
                float2 nt = _FluidNormalTexture_TexelSize.xy;
                float3 nC = tex2D(_FluidNormalTexture, uv).rgb * 2.0 - 1.0;
                float3 nL = tex2D(_FluidNormalTexture, uv + float2(-nt.x, 0)).rgb * 2.0 - 1.0;
                float3 nR = tex2D(_FluidNormalTexture, uv + float2( nt.x, 0)).rgb * 2.0 - 1.0;
                float3 nD = tex2D(_FluidNormalTexture, uv + float2(0, -nt.y)).rgb * 2.0 - 1.0;
                float3 nU = tex2D(_FluidNormalTexture, uv + float2(0,  nt.y)).rgb * 2.0 - 1.0;
                float3 normal = normalize(nC * 2.1 + nL + nR + nD + nU);

                // --- EDGE TRIMMING & SPRAY PRESERVATION ---
                // Original logic: thickness = max(0.0, thickness - _ThicknessCutoff);
                // This kills spray particles that are thinner than cutoff.
                
                float rawThickness = thickness;
                float bodyThickness = max(0.0, rawThickness - _ThicknessCutoff);

                // --- EARLY EXIT (Background) ---
                // Only exit if there is TRULY no fluid (rawThickness is near 0)
                if (fluidDepth > 500.0 || rawThickness <= 0.0001) 
                {
                    return tex2D(_MainTex, bgUV);
                }

                // --- OCCLUSION TEST ---
                float sceneDepthRaw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw);
                
                // Sample background early for both paths
                float3 bg = tex2D(_MainTex, bgUV).rgb;
               
                if (fluidDepth > sceneDepth)
                {
                    return float4(bg, 1.0);
                }
                
                float3 viewDir = normalize(float3(0,0,1)); 
                float3 lightDir = normalize(float3(0.5, 0.8, -0.5)); 
                
                // Calculate lighting early so we can use it for spray
                float NdotL = saturate(dot(normal, lightDir));
                float NdotV = saturate(dot(normal, viewDir));
                float3 h = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, h));
                float spec = pow(NdotH, _Smoothness * 128.0) * _Specular;
                
                // Neighborhood thickness to distinguish sheet edges from isolated spray
                float2 texel = _MainTex_TexelSize.xy;
                float tL = tex2D(_FluidThicknessTexture, uv + float2(-texel.x, 0)).r;
                float tR = tex2D(_FluidThicknessTexture, uv + float2(texel.x, 0)).r;
                float tU = tex2D(_FluidThicknessTexture, uv + float2(0, texel.y)).r;
                float tD = tex2D(_FluidThicknessTexture, uv + float2(0, -texel.y)).r;
                float neighborMax = max(max(tL, tR), max(tU, tD));
                
                // --- SPLASH / SPRAY RENDERING ---
                // [DISABLED] User requested strict clipping. This block was re-adding clipped particles as spray.
                /*
                if (bodyThickness <= 0.0001 && neighborMax < _ThicknessCutoff * 0.5)
                {
                    float sprayRatio = smoothstep(0.0, _ThicknessCutoff, rawThickness);
                    float sprayAlpha = smoothstep(0.2, 0.9, sprayRatio);
                    
                    // Boost normal for small droplets to give them "roundness"
                    float3 dropletNormal = normalize(float3(normal.xy * 2.5, normal.z));
                    float NdotV_Drop = saturate(dot(dropletNormal, viewDir));
                    float3 h_Drop = normalize(lightDir + viewDir);
                    float NdotH_Drop = saturate(dot(dropletNormal, h_Drop));

                    // 1. Refraction
                    float2 dropOffset = dropletNormal.xy * 0.03 * sprayAlpha; 
                    float3 refractedDropBg = tex2D(_MainTex, bgUV + dropOffset).rgb;

                    // 2. Specular
                    float spraySpec = pow(NdotH_Drop, _Smoothness * 200.0) * _Specular * 1.2;

                    // 3. Fresnel
                    float dropFresnel = pow(1.0 - NdotV_Drop, 3.0) * 0.4;

                    // 4. Color Tinting
                    float3 waterTint = _Color.rgb * 0.8;
                    float3 dropBodyColor = lerp(refractedDropBg, waterTint, 0.25 * sprayAlpha);

                    // Combine
                    float3 finalDrop = dropBodyColor + (spraySpec + dropFresnel) * sprayAlpha;
                    
                    return float4(lerp(bg, finalDrop, sprayAlpha), 1.0);
                }
                */

                // --- MAIN WATER BODY RENDERING ---
                // Use bodyThickness for absorption and refraction
                thickness = bodyThickness;

                // --- REFRACTION & EDGE SMOOTHING ---
                // Calculate an edge factor that is 0 at thickness=0 and 1 at thickness=0.1
                // This will be used to dampen refraction and normal effects at the very edge.
                // Modified: Use smoothstep for better control and avoid "waves"
                float edgeFactor = smoothstep(0.0, 0.26, thickness);
                
                // 1. Dampen Normal at edges: Force normal to point towards camera (0,0,1)
                // This prevents extreme refraction and specular highlights at the jagged edges.
                normal = lerp(float3(0,0,1), normal, edgeFactor);

                // 2. Dampen Refraction Offset
                // Modified: Reduced multiplier to prevent extreme distortion at edges
                float2 offset = normal.xy * 0.012 * min(thickness, 1.0) * edgeFactor;
                float2 refractedBgUV = bgUV + offset; 
                float3 refractedBg = tex2D(_MainTex, refractedBgUV).rgb;

                // --- LIGHTING & COLOR ---
                // "Tint" logic: Absorb the complementary color.
                // If _Color is Blue (0,0,1), we want to absorb Red/Green.
                // Absorb = (1 - Color) * Absorption
                float3 absorptionCoef = (1.0 - _Color.rgb) * _Absorption;
                float3 transmission = exp(-absorptionCoef * thickness);

                // Note: Lighting vectors calculated above for spray logic
                // float3 viewDir = normalize(float3(0,0,1)); 
                // float3 lightDir = normalize(float3(0.5, 0.8, -0.5)); 
                // float NdotL = saturate(dot(normal, lightDir));
                // float NdotV = saturate(dot(normal, viewDir));
                // float3 h = normalize(lightDir + viewDir);
                // float NdotH = saturate(dot(normal, h));
                // float spec = pow(NdotH, _Smoothness * 128.0) * _Specular;

                float fresnel = pow(1.0 - NdotV, 4.0) * 0.5; 
                
                // Final Color Combination
                // Modified: Separate lighting fade to be much stricter than normal/refraction fade.
                // This kills the white specular/fresnel line at the very edge.
                float lightingFade = smoothstep(0.07, 0.42, thickness);
                float edgeSpec = edgeFactor * edgeFactor;
                float3 finalColor = refractedBg * transmission + (spec + fresnel * edgeSpec) * lightingFade;

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
