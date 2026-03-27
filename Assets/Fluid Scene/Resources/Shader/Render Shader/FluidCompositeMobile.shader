Shader "Instanced/FluidCompositeMobile" {
    Properties{
        _size("Size", Float) = 0.1
        _TintColor("Tint", Color) = (0.6,0.8,1,1)
        _FoamColor("FoamColor", Color) = (1,1,1,1)
        _Opacity("Opacity", Range(0,1)) = 0.6
        _RefractStrength("RefractStrength", Range(0,0.05)) = 0.02
        _FresnelPower("FresnelPower", Range(1,8)) = 4
        _SoftDepth("SoftDepth", Range(0.01,2)) = 0.2
        _SoftDepth01("SoftDepth01", Range(0.001,0.5)) = 0.02
        _MinDepthVisibility("MinDepthVisibility", Range(0,1)) = 0.2
        _EdgeBoost("EdgeBoost", Range(0,2)) = 0.8
        _EdgeWidth("EdgeWidth", Range(0.001,0.2)) = 0.02
        _TintMix("TintMix", Range(0,1)) = 0.5
        _EnvStrength("EnvStrength", Range(0,2)) = 0.6
        _SpecularStrength("SpecularStrength", Range(0,2)) = 0.8
        _HighlightClamp("HighlightClamp", Range(0.2,1)) = 0.95
        _Absorption("Absorption", Range(0,5)) = 0.8
        _ReflectionThicknessSuppress("ReflectionThicknessSuppress", Range(0,4)) = 1.0
        _RefractionThicknessSuppress("RefractionThicknessSuppress", Range(0,4)) = 0.5
        _EnableReflection("EnableReflection", Range(0,1)) = 1
        _ReflectionThinPower("ReflectionThinPower", Range(0.5,4)) = 1.5
        _EnableRefraction("EnableRefraction", Range(0,1)) = 1
        _FresnelAlphaBase("FresnelAlphaBase", Range(0.3,0.9)) = 0.6
        _FresnelAlphaWeight("FresnelAlphaWeight", Range(0.5,2.0)) = 1.0
        _RefractionClampPixels("RefractionClampPixels", Range(0.2,4.0)) = 1.2
        _RefractionEdgeSuppress("RefractionEdgeSuppress", Range(0,1)) = 0.6
        _FrontEdgeBoost("FrontEdgeBoost", Range(0,2)) = 1.0
        _FrontEdgeWidth("FrontEdgeWidth", Range(0.001,0.05)) = 0.01
        _FrontRefractionSuppress("FrontRefractionSuppress", Range(0,1)) = 0.5
        _UseBackground("UseBackground", Range(0,1)) = 1
        _BackgroundWeight("BackgroundWeight", Range(0,2)) = 1
        _AbsorptionColor("AbsorptionColor", Color) = (0.8,0.6,0.5,1)
        _DepthNormalWeight("DepthNormalWeight", Range(0,1)) = 0.5
        _ThicknessTopBias("ThicknessTopBias", Range(0,1)) = 0.5
        _DepthEdgeStrength("DepthEdgeStrength", Range(0,4)) = 0.8
        _DepthEdgeThreshold("DepthEdgeThreshold", Range(0,0.02)) = 0.005
        _ThicknessScale("ThicknessScale", Range(0,4)) = 1
        _ThicknessGamma("ThicknessGamma", Range(0.25,2)) = 1
        _ThicknessMax("ThicknessMax", Range(0.5,64)) = 8
        _ThicknessExposure("ThicknessExposure", Range(0.0,4.0)) = 0.8
        _AlphaFloor("AlphaFloor", Range(0,1)) = 0.15
        _FoamStrength("FoamStrength", Range(0, 2)) = 1.0
        _FoamPower("FoamPower", Range(1, 10)) = 3.0
    }
    SubShader{
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        // GrabPass Removed for Mobile Optimization
        Pass{
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"
            float _size;
            float4 _TintColor;
            float _Opacity;
            float _RefractStrength;
            float _FresnelPower;
            float _SoftDepth;
            float _SoftDepth01;
            float _MinDepthVisibility;
            float _EdgeBoost;
            float _EdgeWidth;
            float _TintMix;
            float _EnvStrength;
            float _SpecularStrength;
            float _HighlightClamp;
            float _Absorption;
            float _ReflectionThicknessSuppress;
            float _RefractionThicknessSuppress;
            float _EnableReflection;
            float _ReflectionThinPower;
            float _EnableRefraction;
            float _FresnelAlphaBase;
            float _FresnelAlphaWeight;
            float _RefractionClampPixels;
            float _RefractionEdgeSuppress;
            float _FrontEdgeBoost;
            float _FrontEdgeWidth;
            float _FrontRefractionSuppress;
            float _UseBackground;
            float _BackgroundWeight;
            float4 _AbsorptionColor;
            float _DepthNormalWeight;
            float _ThicknessScale;
            float _ThicknessGamma;
            float _ThicknessMax;
            float _ThicknessExposure;
            float _AlphaFloor;
            float _ThicknessTopBias;
            float _DepthEdgeStrength;
            float _DepthEdgeThreshold;
            float _FoamStrength;
            float _FoamPower;
            struct Particle { float3 position; float4 color; };
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<Particle> _particlesBuffer;
            #endif
            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float3 pos = _particlesBuffer[unity_InstanceID].position;
                float size = _size;
                unity_ObjectToWorld._11_21_31_41 = float4(size, 0, 0, 0);
                unity_ObjectToWorld._12_22_32_42 = float4(0, size, 0, 0);
                unity_ObjectToWorld._13_23_33_43 = float4(0, 0, size, 0);
                unity_ObjectToWorld._14_24_34_44 = float4(pos.xyz, 1);
                unity_WorldToObject = unity_ObjectToWorld;
                unity_WorldToObject._14_24_34 *= -1;
                unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
                #endif
            }
            sampler2D _FluidBackgroundTexture;
            float4 _FoamColor;
            sampler2D _CameraDepthTexture;
            sampler2D _SSDepthTex;
            float4 _SSDepthTex_TexelSize;
            sampler2D _BlurredThicknessTex;
            sampler2D _ThicknessNormalsTex;
            sampler2D _DepthNormalsTex;
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 grabPos : TEXCOORD2;
            };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float4 wpos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wpos.xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 Nt = tex2D(_ThicknessNormalsTex, uv).rgb * 2.0 - 1.0;
                float3 Nd = tex2D(_DepthNormalsTex, uv).rgb * 2.0 - 1.0;
                float w = _DepthNormalWeight;
                float nbias = saturate(1.0 - _ThicknessTopBias * saturate(Nt.z));
                w *= nbias;
                float3 Ns = normalize(lerp(Nt, Nd, w));
                float F = pow(1.0 - saturate(dot(Ns, V)), _FresnelPower);
                float thick = tex2D(_BlurredThicknessTex, uv).r;
                float tExp = saturate(1.0 - exp(-_ThicknessExposure * thick));
                float tNorm = saturate(thick / max(_ThicknessMax, 1e-6));
                float tBase = saturate(pow(tNorm * _ThicknessScale, _ThicknessGamma));
                float tAlpha = saturate(max(tExp, 0.5 * tBase));
                float suppressR = saturate(1.0 - _RefractionThicknessSuppress * tNorm);
                float2 refractOffset = Ns.xy * _RefractStrength * tExp * suppressR * _EnableRefraction;
                float roLen = length(refractOffset);
                float roClamp = max(_RefractionClampPixels, 1e-6);
                float clampScale = min(1.0, roClamp / max(roLen, 1e-6));
                refractOffset *= clampScale;
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 bgUV = screenUV + refractOffset;
                float3 bg = tex2D(_FluidBackgroundTexture, bgUV).rgb;
                bg = lerp(_TintColor.rgb, bg, saturate(_UseBackground * _BackgroundWeight));
                float4 grabPos = i.screenPos;
                float scene01 = tex2D(_CameraDepthTexture, uv).r;
                float sceneEye = LinearEyeDepth(scene01);
                float part01 = tex2D(_SSDepthTex, uv).r;
                float partEye = part01 * _ProjectionParams.z;
                float occlude = saturate(max(partEye - sceneEye, 0) / _SoftDepth);
                float visibility = max(1.0 - occlude, _MinDepthVisibility);
                float eW = max(_EdgeWidth, 1e-6);
                float edgeFactor = saturate(1.0 - saturate(abs(sceneEye - partEye) / eW));
                float fW = max(_FrontEdgeWidth, 1e-6);
                float frontFactor = saturate((sceneEye - partEye) / fW);
                refractOffset *= (1.0 - _RefractionEdgeSuppress * edgeFactor);
                float2 t = float2(_SSDepthTex_TexelSize.x, _SSDepthTex_TexelSize.y);
                float c0 = part01;
                float cx1 = tex2D(_SSDepthTex, uv + float2(t.x, 0)).r;
                float cx2 = tex2D(_SSDepthTex, uv - float2(t.x, 0)).r;
                float cy1 = tex2D(_SSDepthTex, uv + float2(0, t.y)).r;
                float cy2 = tex2D(_SSDepthTex, uv - float2(0, t.y)).r;
                float g = sqrt(max(0, (cx1 - cx2) * (cx1 - cx2) + (cy1 - cy2) * (cy1 - cy2)));
                float edgeMask = saturate(max(g - _DepthEdgeThreshold, 0) / max(_DepthEdgeThreshold, 1e-6));
                edgeMask = saturate(edgeMask * _DepthEdgeStrength);
                refractOffset *= (1.0 - _FrontRefractionSuppress * frontFactor);
                float4 tint = _TintColor;
                float thickN = thick / max(_ThicknessMax, 1e-6);
                float3 trans3 = exp(-_Absorption * _AbsorptionColor.rgb * thickN);
                float absorbWeight = 1.0 - saturate(max(trans3.r, max(trans3.g, trans3.b)));
                float3 fluidCol = lerp(bg, _TintColor.rgb, absorbWeight);
                float3 baseCol = lerp(bg, fluidCol, saturate(tAlpha * _TintMix));
                #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                float3 worldRefl = reflect(-V, Ns);
                float rough = 0.2;
                float lod = rough * 8.0;
                half3 env = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, worldRefl, lod), unity_SpecCube0_HDR);
                float thin = saturate(1.0 - tAlpha);
                float reflWeight = _EnvStrength * _SpecularStrength * _EnableReflection;
                reflWeight *= F;
                reflWeight *= pow(thin, _ReflectionThinPower);
                reflWeight *= saturate(1.0 - _ReflectionThicknessSuppress * tNorm);
                baseCol = lerp(baseCol, env, saturate(reflWeight));
                #endif
                baseCol = min(baseCol, _HighlightClamp);
                
                // Shoreline Foam
                float foamMask = pow(edgeFactor, _FoamPower);
                baseCol = lerp(baseCol, _FoamColor.rgb, saturate(foamMask * _FoamColor.a * _FoamStrength));
                
                float fresAlpha = lerp(_FresnelAlphaBase, 1.0, saturate(pow(F, _FresnelAlphaWeight)));
                float alpha = _Opacity * visibility * fresAlpha * saturate(max(tAlpha, _AlphaFloor));
                alpha *= (1.0 + _EdgeBoost * edgeFactor);
                alpha *= (1.0 + edgeMask);
                alpha *= (1.0 + _FrontEdgeBoost * frontFactor);
                return float4(baseCol, alpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
