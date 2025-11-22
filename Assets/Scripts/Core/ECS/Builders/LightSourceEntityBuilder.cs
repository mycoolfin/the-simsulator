using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Builders
{
    using Components.WorldObject;
    using Core.ECS.Rendering;

    [BurstCompile]
    public static class LightSourceEntityBuilder
    {
        public static RenderMeshArrayCreator RenderMeshArrayCreator;

        public static bool IsReady()
        {
            RenderMeshArray renderMeshArray = RenderMeshArrayCreator.RenderMeshArray;
            return renderMeshArray.MaterialReferences != null && renderMeshArray.MaterialReferences.Length != 0
                && renderMeshArray.MeshReferences != null && renderMeshArray.MeshReferences.Length != 0;
        }

        public static void CreateLightSource(EntityManager entityManager)
        {
            Entity lightSource = entityManager.CreateEntity(
                typeof(LightSourceTag),
                typeof(LocalTransform),
                typeof(PostTransformMatrix), // For compatibility with ApplyWorldVisualOffsetSystem.
                typeof(URPMaterialPropertyBaseColor)
            );

            entityManager.SetComponentData(lightSource, new LocalTransform
            {
                Position = new float3(0f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            entityManager.SetComponentData(lightSource, new PostTransformMatrix
            {
                Value = float4x4.identity
            });

            entityManager.SetComponentData(lightSource, new URPMaterialPropertyBaseColor());
            RenderMeshUtility.AddComponents(
                lightSource,
                entityManager,
                new(
                    shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.Off,
                    receiveShadows: false
                ),
                RenderMeshArrayCreator.RenderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(RenderMeshArrayCreator.LightSourceIndex, RenderMeshArrayCreator.LightSourceIndex)
            );
        }
    }
}
