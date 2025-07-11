using Unity.Entities;
using Unity.Core;
using Unity.Collections;
using Unity.Mathematics;

public unsafe class FullSpeedRateManager : IRateManager
{
    readonly double fixedStep;
    readonly double frameBudget;
    double simulationTime;
    double backlog;
    int frameCount;
    bool didPush;
    DoubleRewindableAllocators* oldAllocs;

    public FullSpeedRateManager(float fixedStep, double frameBudget, double initialSimTime)
    {
        this.fixedStep = math.clamp((double)fixedStep, 0.0001, 10);
        this.frameBudget = frameBudget;
        simulationTime = initialSimTime;
        backlog = 0;
        frameCount = -1; // Initialize to -1 to ensure first frame is detected
        didPush = false;
        oldAllocs = null;
        Timestep = (float)this.fixedStep;
    }

    public float Timestep { get; set; }

    public unsafe bool ShouldGroupUpdate(ComponentSystemGroup group)
    {
        if (didPush)
        {
            group.World.PopTime();
            group.World.RestoreGroupAllocator(oldAllocs);
            didPush = false;
        }

        // Check if we've moved to a new frame
        int currentFrame = UnityEngine.Time.frameCount;
        if (currentFrame != frameCount)
        {
            frameCount = currentFrame;
            backlog = frameBudget; // Reset backlog for new frame
        }

        // Check if we have enough budget for another step
        if (backlog < fixedStep)
        {
            return false;
        }

        // Consume budget and advance simulation
        backlog -= fixedStep;
        simulationTime += fixedStep;

        group.World.PushTime(new TimeData(simulationTime, (float)fixedStep));
        oldAllocs = group.World.CurrentGroupAllocators;
        group.World.SetGroupAllocator(group.RateGroupAllocators);
        didPush = true;

        return true;
    }
}
