using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Systems.Presentation
{
    public struct WorldVisualOffset : IComponentData
    {
        public float4x4 TransformMatrix;
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial struct ApplyWorldVisualOffsetSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<PostTransformMatrix>();
            state.RequireForUpdate<LocalToWorld>();
            state.RequireForUpdate<WorldVisualOffset>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            WorldVisualOffset worldVisualOffset = SystemAPI.GetSingleton<WorldVisualOffset>();
            state.Dependency = new WorldVisualOffsetJob
            {
                WorldVisualOffset = worldVisualOffset
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete(); // Modifying LocalToWorld requires a complete dependency.
        }
    }

    [BurstCompile]
    public partial struct WorldVisualOffsetJob : IJobEntity
    {
        public WorldVisualOffset WorldVisualOffset;

        public void Execute(in LocalTransform transform, in PostTransformMatrix postTransform, ref LocalToWorld localToWorld)
        {
            float4x4 realTransform = float4x4.TRS(
                transform.Position,
                transform.Rotation,
                postTransform.Value.Scale()
            );

            localToWorld.Value = math.mul(WorldVisualOffset.TransformMatrix, realTransform);
        }
    }
}
