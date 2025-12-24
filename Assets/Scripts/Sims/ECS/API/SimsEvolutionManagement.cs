using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API
{
    using Core.ECS.API;
    using Core.Evolution;
    using Core.ECS.Systems.Simulation;
    using Core.ECS.Systems.Simulation.SimulationRate;
    using Core.ECS.Components.Phenotype;
    using Components.Evolution;
    using Systems.Simulation.Evolution.Assessment;
    using Systems.Simulation.Limbs;

    public class SimsEvolutionManagement : IEvolutionManagement
    {
        private readonly SimsSimulationSettings simulationSettings;
        private readonly ObjectEntityManagement objectEntityManagement;

        public SimsEvolutionManagement(SimsSimulationSettings simulationSettings, ObjectEntityManagement objectEntityManagement)
        {
            this.simulationSettings = simulationSettings;
            this.objectEntityManagement = objectEntityManagement;
        }

        public IEnumerator InitialiseTrial(World world, TrialType trialType, Func<SimulationRateMode> GetSimulationRateModeCallback)
        {
            if (!world.IsCreated)
                yield break;

            EntityManager entityManager = world.EntityManager;

            // Reset the physical world.
            simulationSettings.SetGravity(world, float3.zero);
            simulationSettings.SetFluidSimulation(world, false, 1f);
            objectEntityManagement.DestroyGroundPlane(world);
            objectEntityManagement.DestroyLightSource(world);

            yield return SettleJoints(world, 2f, GetSimulationRateModeCallback); // Necessary as long as the joint flip bug exists.

            if (!world.IsCreated)
                yield break;

            // Set trial-specific environment settings.
            if (trialType == TrialType.GroundDistance || trialType == TrialType.GroundLightFollowing)
            {
                simulationSettings.SetToTerrestrialDefaults(world);
                objectEntityManagement.CreateGroundPlane(world);
            }
            else if (trialType == TrialType.WaterDistance || trialType == TrialType.WaterLightFollowing)
            {
                simulationSettings.SetToAquaticDefaults(world);
                objectEntityManagement.DestroyGroundPlane(world);
            }

            if (trialType == TrialType.GroundLightFollowing || trialType == TrialType.WaterLightFollowing)
                objectEntityManagement.CreateLightSource(world);
            else
                objectEntityManagement.DestroyLightSource(world);
        }

        private IEnumerator SettleJoints(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            if (!world.IsCreated)
                yield break;

            EntityManager entityManager = world.EntityManager;

            // Freeze the root limbs for all phenotypes.
            entityManager.CreateSingleton<FreezeRootLimbsRequest>();

            // Switch to zero gravity.
            simulationSettings.SetGravity(world, float3.zero);

            // Disable the JointBreakSystem to prevent joint breaks during settling.
            simulationSettings.SetJointBreakSystemEnabled(world, false);

            // Run the FixedStepSimulationSystemGroup for the specified time.
            yield return SimulateForSeconds(world, settleSeconds, GetSimulationRateModeCallback, progress);

            // Zero all limb velocities.
            entityManager.CreateSingleton<ZeroAllLimbVelocitiesRequest>();

            // Unfreeze the root limbs.
            EntityQuery freezeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FreezeRootLimbsRequest>());
            if (!freezeQuery.IsEmpty)
                entityManager.DestroyEntity(freezeQuery.GetSingletonEntity());
        }

        public IEnumerator SettlePhenotypes(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            if (!world.IsCreated)
                yield break;

            EntityManager entityManager = world.EntityManager;

            simulationSettings.SetJointBreakSystemEnabled(world, true);

            yield return SimulateForSeconds(world, settleSeconds, GetSimulationRateModeCallback, progress);

            entityManager.CreateSingleton<ZeroAllLimbVelocitiesRequest>();
            // Simulate one extra physics frame to ensure the zero velocity request is processed.
            world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Update();
        }

        public IEnumerator AssessPhenotypes(World world, TrialType trialType, float assessmentSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            if (!world.IsCreated)
                yield break;

            EntityManager entityManager = world.EntityManager;
            entityManager.CreateSingleton(new BeginAssessmentRequest { TrialType = trialType });
            yield return SimulateForSeconds(world, assessmentSeconds, GetSimulationRateModeCallback, progress);
        }

        public Dictionary<ulong, float> GetAssessmentResults(World world)
        {
            if (!world.IsCreated)
                return new Dictionary<ulong, float>();

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(PhenotypeGid), typeof(Fitness));

            if (query.IsEmptyIgnoreFilter)
                return new Dictionary<ulong, float>();

            int individualCount = query.CalculateEntityCount();
            using NativeParallelHashMap<ulong, float> resultsMap = new(individualCount * 2, Allocator.TempJob);
            var job = new GetIndividualAssessmentResultsChunkJob
            {
                Results = resultsMap.AsParallelWriter(),
                GidHandle = entityManager.GetComponentTypeHandle<PhenotypeGid>(isReadOnly: true),
                FitnessHandle = entityManager.GetComponentTypeHandle<Fitness>(isReadOnly: true),
            };
            job.ScheduleParallel(query, default).Complete();

            Dictionary<ulong, float> results = new(individualCount);
            foreach (var kvp in resultsMap)
                results[kvp.Key] = kvp.Value;
            return results;
        }

        private IEnumerator SimulateForSeconds(World world, float seconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            if (!world.IsCreated)
                yield break;

            EntityManager entityManager = world.EntityManager;
            SimulationRateMode mode = GetSimulationRateModeCallback?.Invoke() ?? SimulationRateMode.RealTime;
            simulationSettings.SetSimulationRateControllerMode(world, mode);

            Entity timerSingleton = entityManager.CreateSingleton(new FixedStepPauseAfterTimerRequest() { TimerSeconds = seconds });
            world.Unmanaged.GetExistingSystemState<FixedStepSimulationSystemGroup>().Enabled = true;
            while (entityManager.HasComponent<FixedStepPauseAfterTimerRequest>(timerSingleton))
            {
                if (!world.IsCreated)
                    yield break;

                SimulationRateMode newMode = GetSimulationRateModeCallback?.Invoke() ?? SimulationRateMode.RealTime;
                if (newMode != mode)
                {
                    mode = newMode;
                    simulationSettings.SetSimulationRateControllerMode(world, mode);
                }

                if (progress != null)
                {
                    float timerSeconds = entityManager.GetComponentData<FixedStepPauseAfterTimerRequest>(timerSingleton).TimerSeconds;
                    progress.Report((seconds - timerSeconds) / seconds); // [0, 1].
                }

                yield return null;
            }
        }
    }

    [BurstCompile]
    public struct GetIndividualAssessmentResultsChunkJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<PhenotypeGid> GidHandle;
        [ReadOnly] public ComponentTypeHandle<Fitness> FitnessHandle;

        public NativeParallelHashMap<ulong, float>.ParallelWriter Results;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<PhenotypeGid> phenotypeGids = chunk.GetNativeArray(ref GidHandle);
            NativeArray<Fitness> fitnesses = chunk.GetNativeArray(ref FitnessHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                PhenotypeGid phenotypeGid = phenotypeGids[i];
                Fitness fitness = fitnesses[i];

                Results.TryAdd(phenotypeGid.Value, fitness.Value);
            }
        }
    }
}
