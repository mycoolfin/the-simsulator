using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Sensors
{
    using Core.ECS.Components.WorldObject;
    using Core.ECS.Components.Shared;
    using Components.Phenotype;

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateSensorsSystemGroup))]
    public partial struct UpdateLightSensorsSystem : ISystem
    {
        private ComponentLookup<LocalTransform> localTransformLookup;
        private BufferLookup<EmitterState> emitterStatesLookup;
        private EntityQuery lightSourceQuery;

        public void OnCreate(ref SystemState state)
        {
            localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: false);

            lightSourceQuery = state.GetEntityQuery(typeof(LightSourceTag), typeof(LocalTransform));

            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<LightSourceTag>();
            state.RequireForUpdate<LightSensors>();
        }

        public void OnUpdate(ref SystemState state)
        {
            localTransformLookup.Update(ref state);
            emitterStatesLookup.Update(ref state);

            using NativeArray<LocalTransform> lightSourceTransforms = lightSourceQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            new UpdateLightSensorsJob
            {
                LightSourceTransforms = lightSourceTransforms,
                EmitterStateBuffers = emitterStatesLookup
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct UpdateLightSensorsJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> LightSourceTransforms;
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(in LocalTransform localTransform, ref LightSensors lightSensors, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];

            float3 intensities = new(0f, 0f, 0f);

            if (lightSensors.XAxisSensorEmitterIndex < (ushort)buffer.Length)
            {
                float intensity = GetLightIntensityAtSensor(localTransform, new float3(1, 0, 0));
                intensities.x = intensity;
                buffer[lightSensors.XAxisSensorEmitterIndex] = new EmitterState { Value = intensity };
            }

            if (lightSensors.YAxisSensorEmitterIndex < (ushort)buffer.Length)
            {
                float intensity = GetLightIntensityAtSensor(localTransform, new float3(0, 1, 0));
                intensities.y = intensity;
                buffer[lightSensors.YAxisSensorEmitterIndex] = new EmitterState { Value = intensity };
            }

            if (lightSensors.ZAxisSensorEmitterIndex < (ushort)buffer.Length)
            {
                float intensity = GetLightIntensityAtSensor(localTransform, new float3(0, 0, 1));
                intensities.z = intensity;
                buffer[lightSensors.ZAxisSensorEmitterIndex] = new EmitterState { Value = intensity };
            }

            lightSensors.Intensities = intensities;
        }

        [BurstCompile]
        private readonly float GetLightIntensityAtSensor(in LocalTransform sensorTransform, in float3 localSensorDirection)
        {
            float3 worldSensorDirection = math.mul(sensorTransform.Rotation, localSensorDirection);
            float totalIntensity = 0f;
            foreach (LocalTransform lightSource in LightSourceTransforms)
            {
                // Calculate the light intensity contribution from this light source.
                float3 lightToSensor = sensorTransform.Position - lightSource.Position;
                float distance = math.length(lightToSensor);

                // Avoid division by zero and ensure minimum distance.
                if (distance < 0.01f) distance = 0.01f;

                // Normalise the direction from light to sensor.
                float3 lightDirection = lightToSensor / distance;

                // Calculate angle factor (how aligned the sensor is with the light direction).
                float angleFactor = math.dot(worldSensorDirection, -lightDirection);

                // Exponential falloff: intensity decreases exponentially with distance
                // and is modulated by the angle factor.
                float distanceFalloff = math.exp(-distance * 0.1f);
                float intensity = distanceFalloff * angleFactor;

                totalIntensity += intensity;
            }
            return math.clamp(totalIntensity, -1f, 1f);
        }
    }
}
