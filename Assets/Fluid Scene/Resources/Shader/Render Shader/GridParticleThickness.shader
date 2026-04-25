Shader "Instanced/GridParticleThickness" {
    Properties {
        _size("Size", Float) = 0.1
        _SizeScale("Size Scale", Float) = 1.0
        _AnisotropyScale("Anisotropy Scale", Float) = 0.5
        _MaxAnisotropy("Max Anisotropy", Float) = 4.0
        _ContributionScale("Contribution Scale", Float) = 0.05
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        Pass {
            ZWrite Off
            ZTest Always
            Blend One One // Additive
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"

            float _size;
            float _SizeScale;
            float _AnisotropyScale;
            float _MaxAnisotropy;
            float _ContributionScale;

            struct Particle {
                float4 position;
                float4 color;
                float4 velocity;
            };

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<Particle> _particlesBuffer;
            #endif

            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float3 pos = _particlesBuffer[unity_InstanceID].position.xyz;
                float3 vel = _particlesBuffer[unity_InstanceID].velocity.xyz;
                float density = _particlesBuffer[unity_InstanceID].velocity.w;
                float size = _size * _SizeScale;

                // Adaptive Anisotropy:
                // Reduce stretching for isolated particles (splash) to prevent "ellipsoid" look.
                float anisotropyFactor = smoothstep(0.15, 0.6, density);

                float speed = length(vel);
                float3 dir = (speed > 0.001) ? vel / speed : float3(0,0,1);
                float stretch = min(1.0 + speed * _AnisotropyScale * anisotropyFactor * 0.82, _MaxAnisotropy);
                // 与 GridParticleDepth 类似：限制过度压扁，减少厚度通道里的孔洞与「粒粒分明」感
                float squash = max(1.0 / sqrt(stretch), 0.88);

                float3 up = float3(0,1,0);
                if (abs(dot(dir, up)) > 0.99) up = float3(0,0,1);
                float3 right = normalize(cross(up, dir));
                float3 upActual = cross(dir, right);

                float3 s = float3(squash * size, squash * size, stretch * size);

                float4 col0 = float4(right * s.x, 0);
                float4 col1 = float4(upActual * s.y, 0);
                float4 col2 = float4(dir * s.z, 0);
                float4 col3 = float4(pos, 1);

                unity_ObjectToWorld._11_21_31_41 = col0;
                unity_ObjectToWorld._12_22_32_42 = col1;
                unity_ObjectToWorld._13_23_33_43 = col2;
                unity_ObjectToWorld._14_24_34_44 = col3;

                // Inverse (M = R * S) -> inv(M) = inv(S) * R^T
                float3 invS = 1.0f / s;
                unity_WorldToObject._11_21_31_41 = float4(right.x * invS.x, upActual.x * invS.y, dir.x * invS.z, 0);
                unity_WorldToObject._12_22_32_42 = float4(right.y * invS.x, upActual.y * invS.y, dir.y * invS.z, 0);
                unity_WorldToObject._13_23_33_43 = float4(right.z * invS.x, upActual.z * invS.y, dir.z * invS.z, 0);
                
                float3 invT = float3(
                    -dot(right, pos) * invS.x,
                    -dot(upActual, pos) * invS.y,
                    -dot(dir, pos) * invS.z
                );
                
                unity_WorldToObject._14_24_34_44 = float4(invT, 1);

                #endif
            }

            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float thicknessBoost : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                
                o.thicknessBoost = 1.0;
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    // Adaptive Thickness:
                    // Boost the thickness of isolated (low-density) particles so they survive the high cutoff (0.5).
                    // Without this, the blur operation spreads their small contribution too thin, and they get clipped.
                    float density = _particlesBuffer[unity_InstanceID].velocity.w;
                    
                    // Logic: Low Density (Spray) -> High Boost. High Density (Bulk) -> No Boost.
                    // Range: Boost from 1x to 8x based on density.
                    // Thresholds: Density < 0.2 is spray, > 0.6 is bulk.
                    float sprayFactor = 1.0 - smoothstep(0.1, 0.6, density);
                    // 过高会把薄飞沫加成发白雾边；略收敛仍保留薄区可见性
                    o.thicknessBoost = 1.0 + sprayFactor * 3.25;
                #endif
                
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                // Simple constant contribution per fragment
                // This accumulates density
                // For a perfect sphere volume, we might calculate thickness based on ray-sphere intersection
                // But for fast rendering, a constant or simple falloff is often used.
                // Let's use a soft falloff from center to approximate sphere volume
                // Note: UVs are not passed here to save bandwidth, but we could add them if needed.
                // For now, simple constant splat is fast and effective for density.
                
                float finalContrib = _ContributionScale * i.thicknessBoost;
                return float4(finalContrib, finalContrib, finalContrib, 1);
            }
            ENDCG
        }
    }
}
