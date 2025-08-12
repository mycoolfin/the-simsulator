using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API
{
    using Core.ECS.API;
    using Systems.Presentation;

    public class SimsPresentationSettings : PresentationSettingsBase
    {
        public override void SetColorByFitness(World world, bool enabled)
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

        public override void SetFilterBySurvivors(World world, bool enabled, int maxSurvivors = 0)
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
