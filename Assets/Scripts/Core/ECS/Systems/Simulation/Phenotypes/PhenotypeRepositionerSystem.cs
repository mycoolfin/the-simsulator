using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Systems.Simulation.Phenotypes
{
    using Components.Phenotype;

    public enum RepositionPivot
    {
        BoundingBoxCenter,
        BoundingBoxCenterYMin
    }

    public struct PhenotypeRepositionerSettings : IComponentData
    {
        public bool Enabled;
        public AABB AllowedZone;
        public RepositionPivot Pivot;
        public float3 TargetPosition;
        public float Margin;
        public bool ZeroVelocitiesOnReposition;
    }

    public struct RepositionPhenotypeEntitiesRequest : IComponentData
    {
        public float3 Translation;
        public bool ZeroVelocities;
    }

    [UpdateInGroup(typeof(PhenotypeSystemGroup))]
    public partial struct PhenotypeRepositionerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhenotypeRepositionerSettings>();
        }

        public void OnUpdate(ref SystemState state)
        {
            PhenotypeRepositionerSettings settings = SystemAPI.GetSingleton<PhenotypeRepositionerSettings>();
            if (!settings.Enabled)
                return;

            using EntityCommandBuffer ecb = new(Allocator.TempJob);

            new RepositionPhenotypesJob
            {
                Ecb = ecb.AsParallelWriter(),
                Settings = settings
            }.ScheduleParallel(state.Dependency).Complete();

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    public partial struct RepositionPhenotypesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public PhenotypeRepositionerSettings Settings;

        public void Execute([ChunkIndexInQuery] int index, Entity entity, in PhenotypeBoundingBox boundingBox)
        {
            float3 currentPosition = Settings.Pivot == RepositionPivot.BoundingBoxCenterYMin
                ? new(boundingBox.Center.x, boundingBox.Center.y - boundingBox.CurrentExtents.y, boundingBox.Center.z)
                : boundingBox.Center;
            if (!IsWithinAllowedZone(currentPosition))
            {
                float3 translation = Settings.TargetPosition - currentPosition;
                Ecb.AddComponent(index, entity, new RepositionPhenotypeEntitiesRequest { Translation = translation, ZeroVelocities = Settings.ZeroVelocitiesOnReposition });
            }
        }

        [BurstCompile]
        private readonly bool IsWithinAllowedZone(float3 position)
        {
            AABB a = Settings.AllowedZone;
            float margin = Settings.Margin;
            return position.x >= a.Min.x - margin && position.x <= a.Max.x + margin &&
                   position.y >= a.Min.y - margin && position.y <= a.Max.y + margin &&
                   position.z >= a.Min.z - margin && position.z <= a.Max.z + margin;
        }
    }
}
