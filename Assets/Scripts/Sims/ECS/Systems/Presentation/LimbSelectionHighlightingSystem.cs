using Unity.Burst;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Presentation
{
    using Components.Phenotype;

    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct LimbSelectionHighlightingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SetSelectedOutlineJob().ScheduleParallel();
            new ClearUnselectedOutlineJob().ScheduleParallel();
        }
    }

    [BurstCompile]
    partial struct SetSelectedOutlineJob : IJobEntity
    {
        void Execute(ref LimbOutline outline, in SelectedLimbTag _)
        {
            outline.Value = 1f;
        }
    }

    [BurstCompile]
    [WithNone(typeof(SelectedLimbTag))]
    partial struct ClearUnselectedOutlineJob : IJobEntity
    {
        void Execute(ref LimbOutline outline)
        {
            outline.Value = 0f;
        }
    }
}
