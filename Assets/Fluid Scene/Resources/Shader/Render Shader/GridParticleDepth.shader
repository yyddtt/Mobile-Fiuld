Shader "Instanced/GridParticleDepth" {
    Properties {
        _size("Size", Float) = 0.1
        _SizeScale("Size Scale", Float) = 1.0
        _AnisotropyScale("Anisotropy Scale", Float) = 0.5
        _MaxAnisotropy("Max Anisotropy", Float) = 4.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass {
            ZWrite On
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
                float size = _size * _SizeScale; // Apply extra scaling for hole filling

                // Adaptive Anisotropy:
                // Reduce stretching for isolated particles (splash) to prevent "ellipsoid" look.
                // Density ratio usually < 0.5 for splashes.
                float anisotropyFactor = smoothstep(0.15, 0.6, density);

                float speed = length(vel);
                float3 dir = (speed > 0.001) ? vel / speed : float3(0,0,1);
                float stretch = min(1.0 + speed * _AnisotropyScale * anisotropyFactor, _MaxAnisotropy);
                // float squash = 1.0 / sqrt(stretch);
                // Modified: Clamp squash to prevent particles from becoming too thin (which causes holes/pits in turbulent areas)
                // 0.85 ensures that even at max stretch, the particle keeps 85% of its width.
                // This violates volume conservation (adds volume) but is essential for preventing holes in sparse simulations.
                float squash = max(1.0 / sqrt(stretch), 0.85);

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
                float depth : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Use Linear Eye Depth
                // COMPUTE_EYEDEPTH outputs negative Z in some cases? 
                // Unity docs: COMPUTE_EYEDEPTH(o) -> o = -UnityObjectToViewPos(v.vertex).z
                // View Z is negative in front of camera. So -Z is positive distance.
                // This seems correct.
                COMPUTE_EYEDEPTH(o.depth);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                // Return depth. 
                // If this is rendered into RFloat, we get high precision.
                return float4(i.depth, 0, 0, 1);
            }
            ENDCG
        }
    }
}
