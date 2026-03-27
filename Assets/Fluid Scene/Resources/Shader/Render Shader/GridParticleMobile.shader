Shader "Instanced/GridParticleMobile" {
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.4
        _Metallic("Metallic", Range(0,1)) = 0.0
        _size("Size", Float) = 0.1
        _AnisotropyScale("Anisotropy Scale", Float) = 0.5
        _MaxAnisotropy("Max Anisotropy", Float) = 4.0
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup
        sampler2D _MainTex;
        sampler2D _FluidBackgroundTexture;
        float _size;
        float _AnisotropyScale;
        float _MaxAnisotropy;
        struct Input { 
            float2 uv_MainTex; 
            float4 screenPos;
        };
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
            float size = _size;

            // Adaptive Anisotropy:
            // Reduce stretching for isolated particles (splash) to prevent "ellipsoid" look.
            float anisotropyFactor = smoothstep(0.15, 0.6, density);

            float speed = length(vel);
            float3 dir = (speed > 0.001) ? vel / speed : float3(0,0,1);
            float stretch = min(1.0 + speed * _AnisotropyScale * anisotropyFactor, _MaxAnisotropy);
            float squash = 1.0 / sqrt(stretch);

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
        half _Glossiness;
        half _Metallic;
        void surf(Input IN, inout SurfaceOutputStandard o) {
            float4 col = float4(1,1,1,1);
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            col = _particlesBuffer[unity_InstanceID].color;
            #endif
            o.Albedo = col.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

