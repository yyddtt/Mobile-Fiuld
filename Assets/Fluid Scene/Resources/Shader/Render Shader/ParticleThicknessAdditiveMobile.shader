Shader "Instanced/ParticleThicknessAdditiveMobile" {
    Properties{
        _size("Size", Float) = 0.1
        _ContributionScale("ContributionScale", Float) = 1.0
    }
    SubShader{
        Tags { "Queue"="Overlay" "RenderType"="Opaque" }
        Pass{
            ZWrite Off
            ZTest Always
            Blend One One
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"
            float _size;
            float _ContributionScale;
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
                float3 viewPos : TEXCOORD1;
                float3 centerViewPos : TEXCOORD2;
            };
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                float4 wpos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wpos.xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                float4 vp = mul(UNITY_MATRIX_V, wpos);
                o.viewPos = vp.xyz;
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float3 centerWorld = _particlesBuffer[unity_InstanceID].position;
                float4 centerView = mul(UNITY_MATRIX_V, float4(centerWorld,1));
                o.centerViewPos = centerView.xyz;
                #else
                o.centerViewPos = vp.xyz; // fallback
                #endif
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float2 delta = i.viewPos.xy - i.centerViewPos.xy;
                float r = _size;
                float d2 = dot(delta, delta);
                float r2 = r*r;
                float inside = step(d2, r2);
                float segment = 2.0 * sqrt(max(r2 - d2, 0.0));
                float norm = saturate(segment / max(2.0*r, 1e-6));
                float contrib = inside * norm * _ContributionScale;
                return float4(contrib, contrib, contrib, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
