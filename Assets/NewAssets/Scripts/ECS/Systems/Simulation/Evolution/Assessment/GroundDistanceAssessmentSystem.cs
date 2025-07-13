using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(AssessmentSystemGroup))]
[UpdateAfter(typeof(BeginAssessmentSystem))]
public partial struct GroundDistanceAssessmentSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Fitness>();
        state.RequireForUpdate<GroundDistanceAssessmentData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        new GroundDistanceAssessmentJob
        {
        }.ScheduleParallel(state.Dependency).Complete();
    }
}

[BurstCompile]
public partial struct GroundDistanceAssessmentJob : IJobEntity
{
    public void Execute(in PhenotypeBoundingBox boundingBox, ref Fitness fitness, ref GroundDistanceAssessmentData data)
    {
        float3 currentPosition = (boundingBox.MinBounds + boundingBox.MaxBounds) * 0.5f; // Centroid.

        if (fitness.Value < 0f) // Assessment hasn't started yet.
            data.StartPosition = currentPosition;

        if (currentPosition.y < 0f)
        {
            // If the entity is below ground level, we consider it a failure.
            fitness.Value = 0f;
            return;
        }

        float xzDisplacement = math.length(new float2(currentPosition.x - data.StartPosition.x,
                                                        currentPosition.z - data.StartPosition.z));

        fitness.Value = xzDisplacement;
    }
}
