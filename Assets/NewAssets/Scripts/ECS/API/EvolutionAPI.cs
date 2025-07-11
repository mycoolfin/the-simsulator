using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class EvolutionAPI
{
    public static void FreezeSimulationTime(World world)
    {
        SystemSettingsAPI.SetSimulationRateControllerSettings(world, SimulationRateMode.Paused);
    }

    public static IEnumerator RunSimulationForSeconds(World world, float runSeconds, bool fullSpeed = false)
    {
        SystemSettingsAPI.SetSimulationRateControllerSettings(world, fullSpeed ? SimulationRateMode.FullSpeed : SimulationRateMode.RealTime, runSeconds);
        while (SystemSettingsAPI.GetSimulationRateControllerSettings(world).StopAfterSeconds > 0f)
            yield return new WaitForSecondsRealtime(0.1f); // Poll every 100ms.
        yield break;
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
        entityManager.CreateSingleton(new InitialiseAssessmentRequest { TrialType = trialType });
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

    public void Execute(
        in ArchetypeChunk chunk,
        int unfilteredChunkIndex,
        bool useEnabledMask,
        in v128 chunkEnabledMask
    )
    {
        var gids = chunk.GetNativeArray(ref GidHandle);
        var fitnesses = chunk.GetNativeArray(ref FitnessHandle);
        var limbBuffers = chunk.GetBufferAccessor(ref LimbStatusHandle);

        for (int i = 0; i < chunk.Count; i++)
        {
            var gid = gids[i];
            var fitness = fitnesses[i];
            var limbStatus = limbBuffers[i];

            bool detached = false;
            for (int j = 0; j < limbStatus.Length; j++)
            {
                if (limbStatus[j].AttachmentState == AttachmentState.Detached)
                {
                    detached = true;
                    break;
                }
            }

            Results.TryAdd(gid.Value, detached ? 0f : fitness.Value);
        }
    }
}
