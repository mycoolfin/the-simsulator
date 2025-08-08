using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation.Evolution.Assessment
{
    using UnityIntegration.Evolution;
    using Components.Evolution;
    using Components.Phenotype;

    public struct BeginAssessmentRequest : IComponentData
    {
        public TrialType TrialType;
    }

    [UpdateInGroup(typeof(AssessmentSystemGroup))]
    public partial struct BeginAssessmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginAssessmentRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity requestEntity = SystemAPI.GetSingletonEntity<BeginAssessmentRequest>();
            TrialType trialType = SystemAPI.GetComponent<BeginAssessmentRequest>(requestEntity).TrialType;

            using EntityCommandBuffer ecb = new(Allocator.TempJob);
            new AddAssessmentComponentsToPhenotypesJob
            {
                Ecb = ecb.AsParallelWriter(),
                trialType = trialType,
            }.ScheduleParallel(state.Dependency).Complete();

            ecb.DestroyEntity(requestEntity);

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    [WithAll(typeof(PhenotypeGid))]
    public partial struct AddAssessmentComponentsToPhenotypesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public TrialType trialType;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity rootPhenotypeEntity)
        {
            Ecb.AddComponent(chunkIndex, rootPhenotypeEntity, new Fitness() { Value = -1f }); // -1 indicates that assessment hasn't started yet.
            switch (trialType)
            {
                case TrialType.GroundDistance:
                    Ecb.AddComponent(chunkIndex, rootPhenotypeEntity, new GroundDistanceAssessmentData());
                    break;
                case TrialType.WaterDistance:
                    Ecb.AddComponent(chunkIndex, rootPhenotypeEntity, new WaterDistanceAssessmentData());
                    break;
            }
        }
    }
}
