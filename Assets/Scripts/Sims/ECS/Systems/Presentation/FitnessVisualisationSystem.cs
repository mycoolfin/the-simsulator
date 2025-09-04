using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Presentation
{
    using Core.ECS.Math;
    using Core.ECS.Components.Shared;
    using Components.Evolution;
    using Components.Phenotype;

    public struct FitnessVisualisationSystemSettings : IComponentData
    {
        public bool ColorByFitness;
        public int ShowMaxSurvivors;
    }

    public struct PotentialSurvivorTag : IComponentData { }

    public struct PhenotypeFitnessPair
    {
        public Entity PhenotypeEntity;
        public float Fitness;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct FitnessVisualisationSystem : ISystem
    {
        private ComponentLookup<Fitness> fitnessLookup;
        private ComponentLookup<PotentialSurvivorTag> potentialSurvivorLookup;
        private NativeList<PhenotypeFitnessPair> phenotypeFitnessPairs;
        private bool coloredByFitness;
        private bool filteringBySurvivors;

        public void OnCreate(ref SystemState state)
        {
            fitnessLookup = SystemAPI.GetComponentLookup<Fitness>(isReadOnly: true);
            potentialSurvivorLookup = SystemAPI.GetComponentLookup<PotentialSurvivorTag>(isReadOnly: true);

            phenotypeFitnessPairs = new NativeList<PhenotypeFitnessPair>(Allocator.Persistent);

            coloredByFitness = false;

            state.RequireForUpdate<LimbColor>();
            state.RequireForUpdate<URPMaterialPropertyBaseColor>();
            state.RequireForUpdate<RootPhenotypeEntity>();
            state.RequireForUpdate<PhenotypeEntitiesMetadata>();
            state.RequireForUpdate<FitnessVisualisationSystemSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            FitnessVisualisationSystemSettings settings = SystemAPI.GetSingleton<FitnessVisualisationSystemSettings>();

            if (!settings.ColorByFitness && coloredByFitness) ResetToDefaultColors(ref state);
            if (settings.ShowMaxSurvivors <= 0 && filteringBySurvivors) ResetToNoFiltering(ref state);
            if (!settings.ColorByFitness && settings.ShowMaxSurvivors <= 0)
                return;

            UpdateSortedPairs(ref state);
            float maxFitness = phenotypeFitnessPairs.IsEmpty ? 0f : phenotypeFitnessPairs[0].Fitness;

            if (settings.ColorByFitness) ColorByFitness(ref state, maxFitness);
            if (settings.ShowMaxSurvivors > 0 && maxFitness > 0f) FilterBySurvivors(ref state, settings.ShowMaxSurvivors, phenotypeFitnessPairs);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (phenotypeFitnessPairs.IsCreated)
                phenotypeFitnessPairs.Dispose();
        }

        private void UpdateSortedPairs(ref SystemState state)
        {
            PhenotypeEntitiesMetadata metadata = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>();
            int requiredPhenotypeCapacity = metadata.TotalRootPhenotypeCount.NextPowerOfTwo();
            if (requiredPhenotypeCapacity > phenotypeFitnessPairs.Capacity)
            {
                phenotypeFitnessPairs.Dispose();
                phenotypeFitnessPairs = new(requiredPhenotypeCapacity, Allocator.Persistent);
            }
            phenotypeFitnessPairs.Clear();

            new PopulatePhenotypeFitnessPairsJob
            {
                PhenotypeFitnessPairs = phenotypeFitnessPairs.AsParallelWriter()
            }.ScheduleParallel(state.Dependency).Complete();

            phenotypeFitnessPairs.Sort(new DescendingFitnessComparer());
        }

        private void ColorByFitness(ref SystemState state, float maxFitness)
        {
            fitnessLookup.Update(ref state);

            state.Dependency = new ColorLimbsByFitnessJob
            {
                MaxFitness = maxFitness,
                FitnessLookup = fitnessLookup
            }.ScheduleParallel(state.Dependency);

            coloredByFitness = true;
        }

        private void ResetToDefaultColors(ref SystemState state)
        {
            state.Dependency = new SetLimbColorsToDefaultJob().ScheduleParallel(state.Dependency);

            coloredByFitness = false;
        }

        private void ResetToNoFiltering(ref SystemState state)
        {
            if (!filteringBySurvivors) return;

            using EntityCommandBuffer ecb = new(Allocator.TempJob);

            new RemovePotentialSurvivorTagsJob()
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency).Complete();

            new RemoveDisableRenderingTagsJob
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency).Complete();

            ecb.Playback(state.EntityManager);

            filteringBySurvivors = false;
        }

        private void FilterBySurvivors(ref SystemState state, int maxSurvivors, NativeList<PhenotypeFitnessPair> phenotypeFitnessPairs)
        {
            using EntityCommandBuffer ecb1 = new(Allocator.TempJob);
            new MarkPotentialSurvivorsJob
            {
                Ecb = ecb1.AsParallelWriter(),
                PhenotypeFitnessPairs = phenotypeFitnessPairs,
                MaxSurvivors = maxSurvivors
            }.ScheduleParallel(phenotypeFitnessPairs.Length, 64, state.Dependency).Complete();
            ecb1.Playback(state.EntityManager);

            potentialSurvivorLookup.Update(ref state);

            using EntityCommandBuffer ecb2 = new(Allocator.TempJob);
            new FilterBySurvivorsJob
            {
                Ecb = ecb2.AsParallelWriter(),
                PotentialSurvivorLookup = potentialSurvivorLookup
            }.ScheduleParallel(state.Dependency).Complete();
            ecb2.Playback(state.EntityManager);

            filteringBySurvivors = true;
        }

        private struct DescendingFitnessComparer : IComparer<PhenotypeFitnessPair>
        {
            public readonly int Compare(PhenotypeFitnessPair x, PhenotypeFitnessPair y)
            {
                return y.Fitness.CompareTo(x.Fitness);
            }
        }
    }

    [BurstCompile]
    public partial struct PopulatePhenotypeFitnessPairsJob : IJobEntity
    {
        public NativeList<PhenotypeFitnessPair>.ParallelWriter PhenotypeFitnessPairs;

        public void Execute(Entity entity, in Fitness fitness)
        {
            PhenotypeFitnessPairs.AddNoResize(new PhenotypeFitnessPair { PhenotypeEntity = entity, Fitness = fitness.Value });
        }
    }

    [BurstCompile]
    public partial struct ColorLimbsByFitnessJob : IJobEntity
    {
        [ReadOnly] public float MaxFitness;
        [ReadOnly] public ComponentLookup<Fitness> FitnessLookup;

        private readonly static float4 invalidFitnessColor = new(0f, 0f, 0f, 1f);
        private readonly static float4 minFitnessColor = new(0.5f, 0f, 0f, 1f);
        private readonly static float4 maxFitnessColor = new(0f, 1f, 0f, 1f);
        private readonly static float4 bestFitnessColor = new(0f, 1f, 1f, 1f);

        public void Execute(ref URPMaterialPropertyBaseColor baseColor, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            if (MaxFitness < 1e-6f || rootPhenotypeEntity.Value == Entity.Null || !FitnessLookup.HasComponent(rootPhenotypeEntity.Value))
            {
                baseColor.Value = invalidFitnessColor;
                return;
            }

            float fitness = FitnessLookup[rootPhenotypeEntity.Value].Value;
            if (math.abs(fitness - MaxFitness) < 1e-6f)
            {
                baseColor.Value = bestFitnessColor;
            }
            else
            {
                float normalizedFitness = math.clamp(fitness / MaxFitness, 0f, 1f);
                baseColor.Value = math.lerp(minFitnessColor, maxFitnessColor, normalizedFitness);
            }
        }
    }

    [BurstCompile]
    public partial struct SetLimbColorsToDefaultJob : IJobEntity
    {
        public void Execute(in LimbColor limbColor, ref URPMaterialPropertyBaseColor baseColor)
        {
            baseColor.Value = limbColor.Value;
        }
    }

    [BurstCompile]
    public partial struct MarkPotentialSurvivorsJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeList<PhenotypeFitnessPair> PhenotypeFitnessPairs;
        [ReadOnly] public int MaxSurvivors;

        public void Execute(int index)
        {
            if (index < MaxSurvivors) Ecb.AddComponent<PotentialSurvivorTag>(0, PhenotypeFitnessPairs[index].PhenotypeEntity);
            else Ecb.RemoveComponent<PotentialSurvivorTag>(1, PhenotypeFitnessPairs[index].PhenotypeEntity);
        }
    }

    [BurstCompile]
    [WithAll(typeof(LimbIndex))]
    public partial struct FilterBySurvivorsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public ComponentLookup<PotentialSurvivorTag> PotentialSurvivorLookup;

        public void Execute(Entity limbEntity, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            bool onSurvivor = PotentialSurvivorLookup.HasComponent(rootPhenotypeEntity.Value);
            if (onSurvivor) Ecb.RemoveComponent<DisableRendering>(0, limbEntity);
            else Ecb.AddComponent<DisableRendering>(0, limbEntity);
        }
    }

    [BurstCompile]
    public partial struct RemovePotentialSurvivorTagsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(Entity entity, in PotentialSurvivorTag potentialSurvivorTag)
        {
            Ecb.RemoveComponent<PotentialSurvivorTag>(0, entity);
        }
    }

    [BurstCompile]
    [WithAll(typeof(DisableRendering), typeof(LimbIndex))]
    public partial struct RemoveDisableRenderingTagsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(Entity entity)
        {
            Ecb.RemoveComponent<DisableRendering>(0, entity);
        }
    }
}
