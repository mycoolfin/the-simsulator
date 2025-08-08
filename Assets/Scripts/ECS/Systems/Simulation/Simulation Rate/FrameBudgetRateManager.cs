using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation.SimulationRate
{
    public class FrameBudgetRateManager : IRateManager
    {
        private double fixedTimestep;
        private readonly double frameBudget;
        private readonly double? maxUpdateRate; // Optional rate cap (updates per second)

        private double lastFixedUpdateTime = 0.0;
        private double frameStartTime = -1;
        private int lastFrameCount = -1;
        private double lastUpdateTime = -1; // Track last update time for rate limiting

        private bool didPushTime;
        private unsafe DoubleRewindableAllocators* oldGroupAllocators;

        public float Timestep
        {
            get => (float)fixedTimestep;
            set => fixedTimestep = math.clamp(value, 0.0001f, 10f);
        }

        /// <summary>
        /// Creates a new FrameBudgetRateManager.
        /// </summary>
        /// <param name="fixedTimestep">The fixed timestep in seconds (e.g., 1/60 = 0.0166...)</param>
        /// <param name="frameBudget">The frame budget in seconds (e.g., 1/60 = 0.0166...)</param>
        /// <param name="maxUpdateRate">Optional maximum update rate in Hz (updates per second, e.g., 60.0 for 60 Hz)</param>
        public FrameBudgetRateManager(double fixedTimestep, double frameBudget, double? maxUpdateRate = null)
        {
            this.fixedTimestep = math.clamp(fixedTimestep, 0.0001f, 10f);
            this.frameBudget = math.clamp(frameBudget, 0.0001f, 10f);
            this.maxUpdateRate = maxUpdateRate > 0 ? maxUpdateRate : null;
        }

        public unsafe bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            int currentFrame = Time.frameCount;

            // Reset frame tracking on new frame.
            if (frameStartTime < 0 || currentFrame != lastFrameCount)
            {
                frameStartTime = now;
                lastFrameCount = currentFrame;
            }

            // Clean up previous time push.
            CleanupTimePush(group);

            // Check if we've exceeded the frame budget.
            if (now - frameStartTime >= frameBudget)
                return false;

            // Check if we're exceeding the maximum update rate.
            if (maxUpdateRate.HasValue && lastUpdateTime > 0)
            {
                double minTimeBetweenUpdates = 1.0 / maxUpdateRate.Value;
                if (now - lastUpdateTime < minTimeBetweenUpdates)
                    return false;
            }

            // Push new simulation time.
            PushSimulationTime(group);
            lastUpdateTime = now;
            return true;
        }

        private unsafe void CleanupTimePush(ComponentSystemGroup group)
        {
            if (!didPushTime) return;

            group.World.PopTime();
            didPushTime = false;

            if (oldGroupAllocators != null)
            {
                group.World.RestoreGroupAllocator(oldGroupAllocators);
                oldGroupAllocators = null;
            }
        }

        private unsafe void PushSimulationTime(ComponentSystemGroup group)
        {
            // Push simulated time to ECS world.
            group.World.PushTime(new TimeData(lastFixedUpdateTime, (float)fixedTimestep));
            lastFixedUpdateTime += fixedTimestep;

            didPushTime = true;

            // Set up group allocators.
            oldGroupAllocators = group.World.CurrentGroupAllocators;
            if (oldGroupAllocators != null)
            {
                group.World.SetGroupAllocator(group.RateGroupAllocators);
            }
        }
    }
}
