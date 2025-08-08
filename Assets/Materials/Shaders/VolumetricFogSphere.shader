Shader "Custom/VolumetricFogSphere"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogDensity ("Fog Density", Range(0, 10)) = 1.0
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseSpeed ("Noise Speed", Float) = 1.0
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        Pass
        {
            Name "VolumetricFog"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 objectCenter : TEXCOORD1;
                float3 localPos : TEXCOORD2;
                float3 rayDir : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogDensity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseIntensity;
                float _EdgeSoftness;
            CBUFFER_END
            
            // 3D Procedural noise functions
            float hash(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453123);
            }
            
            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(
                        lerp(hash(i + float3(0, 0, 0)), hash(i + float3(1, 0, 0)), u.x),
                        lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), u.x), u.y),
                    lerp(
                        lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), u.x),
                        lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), u.x), u.y), u.z);
            }
            
            float fbm3D(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * noise3D(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.objectCenter = TransformObjectToWorld(float3(0, 0, 0));
                output.localPos = input.positionOS.xyz;
                
                // Calculate ray direction from camera to vertex
                float3 cameraPos = GetCameraPositionWS();
                output.rayDir = normalize(output.positionWS - cameraPos);
                
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Setup ray and sphere parameters
                float3 cameraPos = GetCameraPositionWS();
                float3 sphereCenter = input.objectCenter;
                float sphereRadius = length(TransformObjectToWorld(float3(0.5, 0, 0)) - sphereCenter);
                
                float3 rayDir = input.rayDir;
                float3 rayStart = cameraPos;
                
                // Ray-sphere intersection
                float3 toSphere = rayStart - sphereCenter;
                float a = dot(rayDir, rayDir);
                float b = 2.0 * dot(toSphere, rayDir);
                float c = dot(toSphere, toSphere) - sphereRadius * sphereRadius;
                float discriminant = b * b - 4.0 * a * c;
                
                if (discriminant < 0.0)
                    discard;
                
                float sqrtDisc = sqrt(discriminant);
                float t1 = (-b - sqrtDisc) / (2.0 * a);
                float t2 = (-b + sqrtDisc) / (2.0 * a);
                
                float tNear = min(t1, t2);
                float tFar = max(t1, t2);
                
                // Determine ray marching bounds
                float tStart = max(tNear, 0.0);
                float tEnd = tFar;
                
                if (tStart >= tEnd)
                    discard;
                
                // Ray marching setup
                float rayLength = tEnd - tStart;
                const int numSamples = 16;
                float stepSize = rayLength / float(numSamples);
                
                float totalFog = 0.0;
                
                // March through the sphere volume
                for (int i = 0; i < numSamples; i++)
                {
                    float t = tStart + (float(i) + 0.5) * stepSize;
                    float3 samplePos = rayStart + rayDir * t;
                    
                    // Convert to local space for noise sampling
                    float3 localSamplePos = mul(unity_WorldToObject, float4(samplePos, 1.0)).xyz;
                    
                    // Sample 3D noise
                    float3 noisePos = localSamplePos * _NoiseScale + _Time.y * _NoiseSpeed;
                    float noiseValue = fbm3D(noisePos);
                    noiseValue = lerp(1.0, noiseValue, _NoiseIntensity);
                    
                    // Calculate distance-based density falloff
                    float distFromCenter = length(localSamplePos);
                    float normalizedDist = distFromCenter / 0.5; // 0.5 is local space sphere radius
                    
                    // Skip samples outside the sphere
                    if (normalizedDist > 1.0) continue;
                    
                    // Apply edge softness
                    float densityFalloff;
                    if (_EdgeSoftness > 0.0)
                        densityFalloff = saturate((1.0 - normalizedDist) / _EdgeSoftness);
                    else
                        densityFalloff = 1.0;
                    
                    // Accumulate fog density
                    float sampleDensity = densityFalloff * noiseValue * _FogDensity;
                    totalFog += sampleDensity * stepSize;
                }
                
                // Convert accumulated fog to alpha using Beer's law
                float fogAlpha = 1.0 - exp(-totalFog);
                
                // Apply lighting
                Light mainLight = GetMainLight();
                float3 lightColor = max(mainLight.color, float3(0.3, 0.3, 0.3));
                
                float3 finalColor = _FogColor.rgb * lightColor;
                float finalAlpha = saturate(fogAlpha) * _FogColor.a;
                
                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
