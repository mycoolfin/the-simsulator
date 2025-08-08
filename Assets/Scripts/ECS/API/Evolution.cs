using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.API
{
    using UnityIntegration.Evolution;
    using Components.Evolution;
    using Components.Phenotype;
    using Systems.Simulation;
    using Systems.Simulation.Evolution.Assessment;
    using Systems.Simulation.Limbs;
    using Systems.Simulation.SimulationRate;

    public struct GroundPlaneTag : IComponentData { }

    public static class Evolution
    {
        public static IEnumerator InitialiseTrial(World world, TrialType trialType, Func<SimulationRateMode> GetSimulationRateModeCallback)
        {
            EntityManager entityManager = world.EntityManager;

            // Reset the physical world.
            SimulationSettings.SetGravity(world, float3.zero);
            SimulationSettings.SetFluidSimulation(world, false, 1f);
            DestroyGroundPlane(world);

            yield return SettleJoints(world, 2f, GetSimulationRateModeCallback); // Necessary as long as the joint flip bug exists.

            // Set trial-specific environment settings.
            if (trialType == TrialType.GroundDistance)
            {
                SimulationSettings.SetGravity(world, PhysicsStep.Default.Gravity);
                // Create ground plane.
                CreateGroundPlane(world);
            }
            else if (trialType == TrialType.WaterDistance)
            {
                SimulationSettings.SetGravity(world, float3.zero);
                SimulationSettings.SetFluidSimulation(world, true, 1f);
            }

            // Reposition phenotype entities if necessary.
            if (trialType == TrialType.GroundDistance)
            {
                entityManager.CreateSingleton(new RepositionLimbsRequest() { GroundY = 0f });
                world.GetExistingSystem<RepositionLimbsSystem>().Update(world.Unmanaged);
            }
        }

        private static void CreateGroundPlane(World world)
        {
            float groundSize = 1000f; // Large but not infinite to avoid physics issues
            float groundThickness = 1f;

            EntityManager entityManager = world.EntityManager;

            Entity groundPlane = entityManager.CreateEntity();

            entityManager.AddComponentData(groundPlane, new GroundPlaneTag());

            entityManager.AddComponentData(groundPlane, new LocalTransform
            {
                Position = new float3(0f, -groundThickness * 0.5f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            BoxGeometry boxGeometry = new()
            {
                Center = float3.zero,
                Size = new float3(groundSize, groundThickness, groundSize),
                Orientation = quaternion.identity
            };

            CollisionFilter groundFilter = new()
            {
                BelongsTo = 1u, // Ground layer.
                CollidesWith = ~0u, // Collide with everything.
                GroupIndex = 0
            };

            BlobAssetReference<Collider> groundCollider = BoxCollider.Create(boxGeometry, groundFilter);

            entityManager.AddComponentData(groundPlane, new PhysicsCollider { Value = groundCollider });
            entityManager.AddSharedComponent(groundPlane, new PhysicsWorldIndex(0));
        }

        private static void DestroyGroundPlane(World world)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery groundPlaneQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GroundPlaneTag>());
            if (!groundPlaneQuery.IsEmptyIgnoreFilter)
            {
                Entity groundPlane = groundPlaneQuery.GetSingletonEntity();
                entityManager.DestroyEntity(groundPlane);
            }
        }

        private static IEnumerator SettleJoints(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            EntityManager entityManager = world.EntityManager;

            // Freeze the root limbs for all phenotypes.
            entityManager.CreateSingleton<FreezeRootLimbsRequest>();

            // Switch to zero gravity.
            SimulationSettings.SetGravity(world, float3.zero);

            // Disable the JointBreakSystem to prevent joint breaks during settling.
            SimulationSettings.SetJointBreakSystemEnabled(world, false);

            // Run the FixedStepSimulationSystemGroup for the specified time.
            yield return SimulateForSeconds(world, settleSeconds, GetSimulationRateModeCallback, progress);

            // Zero all limb velocities.
            entityManager.CreateSingleton<ZeroAllLimbVelocitiesRequest>();

            // Unfreeze the root limbs.
            EntityQuery freezeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FreezeRootLimbsRequest>());
            if (!freezeQuery.IsEmpty)
                entityManager.DestroyEntity(freezeQuery.GetSingletonEntity());
        }

        public static IEnumerator SettlePhenotypes(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            EntityManager entityManager = world.EntityManager;

            SimulationSettings.SetJointBreakSystemEnabled(world, true);

            yield return SimulateForSeconds(world, settleSeconds, GetSimulationRateModeCallback, progress);

            entityManager.CreateSingleton<ZeroAllLimbVelocitiesRequest>();
        }

        public static IEnumerator AssessPhenotypes(World world, TrialType trialType, float assessmentSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            EntityManager entityManager = world.EntityManager;
            entityManager.CreateSingleton(new BeginAssessmentRequest { TrialType = trialType });
            yield return SimulateForSeconds(world, assessmentSeconds, GetSimulationRateModeCallback, progress);
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

        private static IEnumerator SimulateForSeconds(World world, float seconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null)
        {
            EntityManager entityManager = world.EntityManager;
            SimulationRateMode mode = GetSimulationRateModeCallback?.Invoke() ?? SimulationRateMode.RealTime;
            SimulationSettings.SetSimulationRateControllerMode(world, mode);

            Entity timerSingleton = entityManager.CreateSingleton(new FixedStepPauseAfterTimerRequest() { TimerSeconds = seconds });
            world.Unmanaged.GetExistingSystemState<FixedStepSimulationSystemGroup>().Enabled = true;
            while (entityManager.HasComponent<FixedStepPauseAfterTimerRequest>(timerSingleton))
            {
                SimulationRateMode newMode = GetSimulationRateModeCallback?.Invoke() ?? SimulationRateMode.RealTime;
                if (newMode != mode)
                {
                    mode = newMode;
                    SimulationSettings.SetSimulationRateControllerMode(world, mode);
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
}
