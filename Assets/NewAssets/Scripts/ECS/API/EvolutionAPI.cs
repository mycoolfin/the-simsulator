using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

public static class EvolutionAPI
{
    public static void InitialiseTrial(World world, NewAssets.TrialType trialType)
    {
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateSingleton(new InitialiseTrialRequest { TrialType = trialType });
    }

    public static bool IsTrialInitialised(World world)
    {
        EntityManager entityManager = world.EntityManager;
        return !entityManager.HasComponent<InitialiseTrialRequest>(entityManager.CreateEntity())
            && !entityManager.HasComponent<InitialisingTrialTag>(entityManager.CreateEntity());
    }

    public static void ZeroAllLimbVelocities(World world)
    {
        EntityManager entityManager = world.EntityManager;
        Entity singletonEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(singletonEntity, new ZeroAllLimbVelocitiesRequest()); // Zero-sized, so have to do it this way.
    }

    public static void BeginAssessment(World world, NewAssets.TrialType trialType)
    {
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateSingleton(new BeginAssessmentRequest { TrialType = trialType });
    }

    public static Dictionary<ulong, float> GetAssessmentResults(World world)
    {
        EntityManager entityManager = world.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(PhenotypeGid), typeof(Fitness), typeof(LimbStatus));

        if (query.IsEmptyIgnoreFilter)
            return new Dictionary<ulong, float>();

        int individualCount = query.CalculateEntityCount();
        using NativeParallelHashMap<ulong, float> resultsMap = new(individualCount, Allocator.TempJob);
        var job = new GetIndividualAssessmentResultsChunkJob
        {
            Results = resultsMap.AsParallelWriter(),
            GidHandle = entityManager.GetComponentTypeHandle<PhenotypeGid>(isReadOnly: true),
            FitnessHandle = entityManager.GetComponentTypeHandle<Fitness>(isReadOnly: true),
            LimbStatusHandle = entityManager.GetBufferTypeHandle<LimbStatus>(isReadOnly: true)
        };
        job.ScheduleParallel(query, default).Complete();

        Dictionary<ulong, float> results = new(individualCount);
        foreach (var kvp in resultsMap)
            results[kvp.Key] = kvp.Value;
        return results;
    }
}

[BurstCompile]
public struct GetIndividualAssessmentResultsChunkJob : IJobChunk
{
    [ReadOnly] public ComponentTypeHandle<PhenotypeGid> GidHandle;
    [ReadOnly] public ComponentTypeHandle<Fitness> FitnessHandle;
    [ReadOnly] public BufferTypeHandle<LimbStatus> LimbStatusHandle;

    public NativeParallelHashMap<ulong, float>.ParallelWriter Results;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        NativeArray<PhenotypeGid> phenotypeGids = chunk.GetNativeArray(ref GidHandle);
        NativeArray<Fitness> fitnesses = chunk.GetNativeArray(ref FitnessHandle);
        BufferAccessor<LimbStatus> limbStatusBuffers = chunk.GetBufferAccessor(ref LimbStatusHandle);

        for (int i = 0; i < chunk.Count; i++)
        {
            PhenotypeGid phenotypeGid = phenotypeGids[i];
            Fitness fitness = fitnesses[i];
            DynamicBuffer<LimbStatus> limbStatuses = limbStatusBuffers[i];

            bool detached = false;
            for (int j = 0; j < limbStatuses.Length; j++)
            {
                if (limbStatuses[j].AttachmentState == AttachmentState.Detached)
                {
                    detached = true;
                    break;
                }
            }

            Results.TryAdd(phenotypeGid.Value, detached ? 0f : fitness.Value);
        }
    }
}
