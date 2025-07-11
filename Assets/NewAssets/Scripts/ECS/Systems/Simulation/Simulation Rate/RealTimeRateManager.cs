using Unity.Entities;
using Unity.Core;
using Unity.Collections;
using Unity.Mathematics;

public unsafe class RealTimeRateManager : IRateManager
{
    readonly float maxStep;
    double simulationTime;
    bool didPush;
    DoubleRewindableAllocators* oldAllocs;

    public RealTimeRateManager(float maxStep, double initialSimTime)
    {
        this.maxStep = math.clamp(maxStep, 0.0001f, 10f);
        simulationTime = initialSimTime;
        didPush = false;
        oldAllocs = null;
        Timestep = this.maxStep;
    }

    public float Timestep { get; set; }

    public unsafe bool ShouldGroupUpdate(ComponentSystemGroup group)
    {
        if (didPush)
        {
            group.World.PopTime();
            group.World.RestoreGroupAllocator(oldAllocs);
            didPush = false;
            return false;
        }

        float dt = group.World.Time.DeltaTime;

        float step = math.min(dt, maxStep);
        Timestep = step;

        simulationTime += step;

        group.World.PushTime(new TimeData(simulationTime, step));
        oldAllocs = group.World.CurrentGroupAllocators;
        group.World.SetGroupAllocator(group.RateGroupAllocators);
        didPush = true;

        return true;
    }
}
