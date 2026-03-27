  Shader "Instanced/ParticleDepthMobile" {
    Properties{
        _size("Size", Float) = 0.1
        _DebugMode("DebugMode (0=Depth,1=White,2=ID)", Range(0,2)) = 0
        _ParticleCount("ParticleCount", Float) = 1
    }
    SubShader{
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100
        Pass{
            Cull Off
            ZWrite Off
            ZTest Always
            BlendOp Min
            Blend One One
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"

            float _size;
            float _DebugMode;
            float _ParticleCount;

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

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float idNorm : TEXCOORD1;
                float viewZ : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float4 wpos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wpos.xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                float4 vpos = mul(UNITY_MATRIX_V, wpos);
                o.viewZ = vpos.z;
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                o.idNorm = saturate((float)unity_InstanceID / max(_ParticleCount, 1.0));
                #else
                o.idNorm = 0.0;
                #endif
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                if (_DebugMode >= 1.5)
                {
                    return float4(i.idNorm, i.idNorm, i.idNorm, 1);
                }
                if (_DebugMode >= 0.5)
                {
                    return float4(1,1,1,1);
                }
                #endif
                float farClip = _ProjectionParams.z;
                float4 vp = mul(UNITY_MATRIX_V, float4(i.worldPos,1));
                float zEye = -vp.z;
                float d01 = saturate(zEye / max(farClip, 1e-6));
                return float4(d01, d01, d01, 1);
            }
            ENDCG
        }
        Pass{
            Cull Off
            ZWrite Off
            ZTest Always
            BlendOp Max
            Blend One One
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragBack
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"
            float _size;
            float _DebugMode;
            float _ParticleCount;
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
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float3 worldPos:TEXCOORD0; float idNorm:TEXCOORD1; };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float4 wpos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wpos.xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                o.idNorm = saturate((float)unity_InstanceID / max(_ParticleCount, 1.0));
                #else
                o.idNorm = 0.0;
                #endif
                return o;
            }
            float4 fragBack(v2f i):SV_Target
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                if (_DebugMode >= 1.5) return float4(i.idNorm, i.idNorm, i.idNorm, 1);
                if (_DebugMode >= 0.5) return float4(1,1,1,1);
                #endif
                float farClip = _ProjectionParams.z;
                float4 vp = mul(UNITY_MATRIX_V, float4(i.worldPos,1));
                float zEye = -vp.z;
                float d01 = saturate(zEye / max(farClip, 1e-6));
                return float4(d01, d01, d01, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
