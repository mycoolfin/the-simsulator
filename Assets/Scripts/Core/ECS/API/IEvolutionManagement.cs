using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using Core.Evolution;
    using Core.ECS.Systems.Simulation.SimulationRate;

    public interface IEvolutionManagement
    {
        IEnumerator InitialiseTrial(World world, TrialType trialType, Func<SimulationRateMode> GetSimulationRateModeCallback);

        IEnumerator SettlePhenotypes(World world, float settleSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null);

        IEnumerator AssessPhenotypes(World world, TrialType trialType, float assessmentSeconds, Func<SimulationRateMode> GetSimulationRateModeCallback, IProgress<float> progress = null);

        Dictionary<ulong, float> GetAssessmentResults(World world);
    }
}
