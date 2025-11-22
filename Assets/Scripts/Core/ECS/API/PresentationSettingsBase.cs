using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using Systems.Presentation;

    public abstract class PresentationSettingsBase : IPresentationSettings
    {
        public void SetWorldVisualOffset(World world, float4x4 transformMatrix)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(WorldVisualOffset));
            WorldVisualOffset visualOffset = new() { TransformMatrix = transformMatrix };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(visualOffset);
            else query.SetSingleton(visualOffset);
        }

        public abstract void SetColorByFitness(World world, bool enabled);

        public abstract void SetFilterBySurvivors(World world, bool enabled, int maxSurvivors = 0);
    }
}
