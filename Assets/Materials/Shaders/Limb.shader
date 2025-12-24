Shader "Custom/Limb"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
        _OutlineStrength ("Outline Strength", Range(0, 1)) = 0

        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        _MainTex_ST("MainTex_ST", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        HLSLINCLUDE
        #pragma target 4.5
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // -------- Vertex I/O --------
        struct appdata
        {
            float3 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 uv0        : TEXCOORD0;

            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };

        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv0        : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float3 positionWS : TEXCOORD2;
            float  strength   : TEXCOORD3;

            #if UNITY_ANY_INSTANCING_ENABLED
            uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };

        // -------- Per-material defaults (SRP Batcher) --------
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineStrength;
            float4 _MainTex_ST;
        CBUFFER_END

        // -------- DOTS instanced overrides --------
        #if defined(UNITY_DOTS_INSTANCING_ENABLED)
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _OutlineColor)
                UNITY_DOTS_INSTANCED_PROP(float,  _OutlineWidth)
                UNITY_DOTS_INSTANCED_PROP(float,  _OutlineStrength)
                UNITY_DOTS_INSTANCED_PROP(float4, _MainTex_ST)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            #define _BaseColor       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
            #define _OutlineColor    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _OutlineColor)
            #define _OutlineWidth    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _OutlineWidth)
            #define _OutlineStrength UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _OutlineStrength)
            #define _MainTex_ST      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MainTex_ST)
        #endif

        sampler2D _MainTex;

        inline void SetupInstance(appdata v, inout v2f o)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);

            #if UNITY_ANY_INSTANCING_ENABLED
            o.instanceID = v.instanceID;
            #endif
        }
        ENDHLSL

        // ==========================
        // PASS 1: OUTLINE
        // ==========================
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest Less
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex   vert_outline
            #pragma fragment frag_outline

            v2f vert_outline(appdata v)
            {
                v2f o = (v2f)0;
                SetupInstance(v, o);

                float s = saturate(_OutlineStrength);
                o.strength = s;

                // Expand in object space along normal
                float3 posOS = v.positionOS + v.normalOS * (_OutlineWidth * s);

                o.positionWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);

                // Not strictly needed for outline frag, but keep populated
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                // UV not used in outline
                o.uv0 = 0;

                return o;
            }

            half4 frag_outline(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                if (i.strength <= 0.001)
                    discard;

                half4 c = (half4)_OutlineColor;
                c.a *= (half)i.strength;
                return c;
            }
            ENDHLSL
        }

        // ==========================
        // PASS 2: MAIN (simple lit)
        // ==========================
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert_main
            #pragma fragment frag_main

            v2f vert_main(appdata v)
            {
                v2f o = (v2f)0;
                SetupInstance(v, o);

                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);

                o.uv0 = v.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                o.strength = 0;

                return o;
            }

            half4 frag_main(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Base albedo = texture * base color
                half4 albedo = tex2D(_MainTex, i.uv0) * (half4)_BaseColor;

                // Simple lambert: main light + ambient
                float3 n = normalize(i.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float ndotl = saturate(dot(n, mainLight.direction));

                half3 ambient = half3(0.2h, 0.2h, 0.2h);
                half3 lit = ambient + (half3)mainLight.color * (half)ndotl;

                return half4(albedo.rgb * lit, albedo.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
