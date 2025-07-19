using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public struct ColorByFitnessSystemSettings : IComponentData
{
    public bool Enabled;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct ColorByFitnessSystem : ISystem
{
    private ComponentLookup<Fitness> fitnessLookup;
    private bool coloredByFitness;

    public void OnCreate(ref SystemState state)
    {
        fitnessLookup = SystemAPI.GetComponentLookup<Fitness>(isReadOnly: true);

        coloredByFitness = false;

        state.RequireForUpdate<LimbColor>();
        state.RequireForUpdate<URPMaterialPropertyBaseColor>();
        state.RequireForUpdate<RootPhenotypeEntity>();
        state.RequireForUpdate<ColorByFitnessSystemSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ColorByFitnessSystemSettings settings = SystemAPI.GetSingleton<ColorByFitnessSystemSettings>();
        if (settings.Enabled)
        {
            fitnessLookup.Update(ref state);

            using NativeReference<float> maxFitness = new(0f, Allocator.TempJob);

            new CalculateMaxFitnessJob()
            {
                MaxFitness = maxFitness
            }.Schedule(state.Dependency).Complete();

            state.Dependency = new ColorLimbsByFitnessJob
            {
                MaxFitness = maxFitness.Value,
                FitnessLookup = fitnessLookup
            }.ScheduleParallel(state.Dependency);

            coloredByFitness = true;
        }
        else if (coloredByFitness) // Reset to default colors if not coloring by fitness.
        {
            state.Dependency = new SetLimbColorsToDefaultJob { }.ScheduleParallel(state.Dependency);

            coloredByFitness = false;
        }
    }
}

[BurstCompile]
public partial struct CalculateMaxFitnessJob : IJobEntity
{
    public NativeReference<float> MaxFitness;

    public void Execute(in Fitness fitness)
    {
        MaxFitness.Value = math.max(MaxFitness.Value, fitness.Value);
    }
}

[BurstCompile]
public partial struct ColorLimbsByFitnessJob : IJobEntity
{
    public float MaxFitness;
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
            float normalizedFitness = MaxFitness < 1e-6 ? 0f : math.clamp(fitness / MaxFitness, 0f, 1f);
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
