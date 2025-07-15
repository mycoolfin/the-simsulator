using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Scenes;

public static class EvolutionAPI
{
    public static IEnumerator InitialiseTrial(World world, NewAssets.TrialType trialType, EntitySceneReference groundEnvironmentSubScene, EntitySceneReference waterEnvironmentSubScene, Func<SimulationRateMode> GetSimulationRateModeCallback)
    {
        EntityManager entityManager = world.EntityManager;

        yield return SettleJoints(world, 5f, GetSimulationRateModeCallback); // Necessary as long as the joint flip bug exists.

        // Load the trial environment.
        EntitySceneReference environmentSubSceneReference = trialType switch
        {
            NewAssets.TrialType.GroundDistance => groundEnvironmentSubScene,
            NewAssets.TrialType.WaterDistance => waterEnvironmentSubScene,
            _ => throw new ArgumentOutOfRangeException(nameof(trialType), "Unsupported trial type.")
        };
        Entity sceneEntity = SceneSystem.LoadSceneAsync(
            world.Unmanaged,
            environmentSubSceneReference,
            new SceneSystem.LoadParameters
            {
                AutoLoad = true,
                Flags = SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.BlockOnImport
            });
        while (!SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
            yield return null;

        // Set trial-specific physics.
        if (trialType == NewAssets.TrialType.GroundDistance)
        {
            SystemSettingsAPI.SetGravity(world, PhysicsStep.Default.Gravity);
        }
        else if (trialType == NewAssets.TrialType.WaterDistance)
        {
            SystemSettingsAPI.SetGravity(world, float3.zero);
            SystemSettingsAPI.SetFluidSimulation(world, true, 1000f);
        }

        // Reposition phenotype entities if necessary.
        if (trialType == NewAssets.TrialType.GroundDistance)
        {
            entityManager.CreateSingleton(new RepositionPhenotypesRequest() { GroundY = 0f });
            world.GetExistingSystem<RepositionPhenotypesSystem>().Update(world.Unmanaged);
        }
    }

    private static IEnumerator SettleJoints(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback)
    {
        EntityManager entityManager = world.EntityManager;

        // Freeze the root limbs for all phenotypes.
        entityManager.CreateSingleton<FreezeRootLimbsRequest>();

        // Switch to zero gravity.
        SystemSettingsAPI.SetGravity(world, float3.zero);

        // Disable the JointBreakSystem to prevent joint breaks during settling.
        SystemSettingsAPI.SetJointBreakSystemEnabled(world, false);

        // Run the FixedStepSimulationSystemGroup for the specified time.
        yield return SimulateForSeconds(world, settleSeconds, GetSimulationRateModeCallback);

        // Zero all limb velocities.
        entityManager.CreateSingleton<ZeroAllLimbVelocitiesRequest>();

        // Unfreeze the root limbs.
        EntityQuery freezeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FreezeRootLimbsRequest>());
        if (!freezeQuery.IsEmpty)
            entityManager.DestroyEntity(freezeQuery.GetSingletonEntity());
    }

    public static IEnumerator SettlePhenotypes(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback)
    {
        EntityManager entityManager = world.EntityManager;

        SystemSettingsAPI.SetJointBreakSystemEnabled(world, true);

        yield return SimulateForSeconds(world, settleSeconds, GetSimulationRateModeCallback);

        entityManager.CreateSingleton<ZeroAllLimbVelocitiesRequest>();
    }

    private static IEnumerator SimulateForSeconds(World world, float seconds, Func<SimulationRateMode> GetSimulationRateModeCallback)
    {
        EntityManager entityManager = world.EntityManager;
        SimulationRateMode mode = GetSimulationRateModeCallback?.Invoke() ?? SimulationRateMode.RealTime;
        SystemSettingsAPI.SetSimulationRateControllerMode(world, mode);

        Entity timerSingleton = entityManager.CreateSingleton(new FixedStepPauseAfterTimerRequest() { TimerSeconds = seconds });
        world.Unmanaged.GetExistingSystemState<FixedStepSimulationSystemGroup>().Enabled = true;
        while (entityManager.HasComponent<FixedStepPauseAfterTimerRequest>(timerSingleton))
        {
            SimulationRateMode newMode = GetSimulationRateModeCallback?.Invoke() ?? SimulationRateMode.RealTime;
            if (newMode != mode)
            {
                mode = newMode;
                SystemSettingsAPI.SetSimulationRateControllerMode(world, mode);
            }
            yield return null;
        }
    }

    public static IEnumerator AssessPhenotypes(World world, NewAssets.TrialType trialType, float assessmentSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback)
    {
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateSingleton(new BeginAssessmentRequest { TrialType = trialType });
        yield return SimulateForSeconds(world, assessmentSeconds, GetSimulationRateModeCallback);
    }

    public static Dictionary<ulong, float> GetAssessmentResults(World world)
    {
        EntityManager entityManager = world.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(PhenotypeGid), typeof(Fitness), typeof(LimbStatus));

        if (query.IsEmptyIgnoreFilter)
            return new Dictionary<ulong, float>();

        int individualCount = query.CalculateEntityCount();
        using NativeParallelHashMap<ulong, float> resultsMap = new(individualCount * 2, Allocator.TempJob);
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
