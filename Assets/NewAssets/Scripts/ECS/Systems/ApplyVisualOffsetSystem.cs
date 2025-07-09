using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct ApplyVisualOffsetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, postTransform, offset, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PostTransformMatrix>, RefRO<VisualOffset>>().WithEntityAccess())
        {
            float3 visualPosition = transform.ValueRO.Position + offset.ValueRO.Offset;

            state.EntityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = float4x4.TRS(
                    visualPosition,
                    transform.ValueRO.Rotation,
                    postTransform.ValueRO.Value.Scale())
            });
        }
    }
}
