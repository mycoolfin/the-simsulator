using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.API
{
    using Systems.Presentation;

    public static class PresentationSettings
    {
        public static void SetWorldVisualOffset(World world, float4x4 transformMatrix)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(WorldVisualOffset));
            WorldVisualOffset visualOffset = new() { TransformMatrix = transformMatrix };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(visualOffset);
            else query.SetSingleton(visualOffset);
        }

        public static void SetColorByFitness(World world, bool enabled)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(FitnessVisualisationSystemSettings));
            if (query.IsEmptyIgnoreFilter)
                entityManager.CreateSingleton(new FitnessVisualisationSystemSettings { ColorByFitness = enabled });
            else
            {
                FitnessVisualisationSystemSettings settings = query.GetSingleton<FitnessVisualisationSystemSettings>();
                settings.ColorByFitness = enabled;
                query.SetSingleton(settings);
            }
        }

        public static void SetFilterBySurvivors(World world, bool enabled, int maxSurvivors = 0)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(FitnessVisualisationSystemSettings));
            if (query.IsEmptyIgnoreFilter)
                entityManager.CreateSingleton(new FitnessVisualisationSystemSettings { ShowMaxSurvivors = enabled ? maxSurvivors : 0 });
            else
            {
                FitnessVisualisationSystemSettings settings = query.GetSingleton<FitnessVisualisationSystemSettings>();
                settings.ShowMaxSurvivors = enabled ? maxSurvivors : 0;
                query.SetSingleton(settings);
            }
        }
    }
}
