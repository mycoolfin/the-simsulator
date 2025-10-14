using System;
using System.Collections.Generic;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.Evolution
{
    using Core.ECS.API;

    public interface IEvolutionSimulator
    {
        // Evolution parameters.
        int PopulationSize { get; }
        int MaxGenerations { get; }
        float SurvivalRate { get; }
        int MaxSurvivors { get; }
        float MutationRate { get; }
        float SettleSeconds { get; }
        float AssessmentSeconds { get; }
        TrialType TrialType { get; }
        string SeedGenotypeName { get; }
        bool LockMorphologies { get; }

        // Runtime status.
        bool IsRunning { get; }
        bool IsEvolutionLoopPaused { get; }
        bool IsSimulationPaused { get; }
        bool IsSimulationRealTime { get; }
        bool IsSimulationFullSpeed { get; }
        bool IsSimulationHeadless { get; }
        int CurrentGeneration { get; }
        float GenotypeCreationProgress { get; }
        float PhenotypeCreationProgress { get; }
        float SettleProgress { get; }
        float AssessmentProgress { get; }
        IReadOnlyList<IAssessableCreature> Population { get; }
        IReadOnlyList<EvolutionStatistics> Statistics { get; }

        // Events.
        event Action OnEvolutionStart;
        event Action OnEvolutionStop;
        event Action<World> OnEcsWorldCreated;
        event Action<int> OnGenerationStart;
        event Action<IReadOnlyList<EvolutionStatistics>> OnGenerationComplete;
        event Action OnEvolutionComplete;

        // Methods.
        void SetEvolutionParameters(EvolutionParameters parameters);
        void StartEvolution();
        void StopEvolution();
        void SetEvolutionLoopPaused(bool paused);
        void PauseSimulation();
        void RealTimeSimulation();
        void FullSpeedSimulation();
        void HeadlessSimulation();
        ISimulationSettings GetSimulationSettingsAPI();
        IPresentationSettings GetPresentationSettingsAPI();
    }
}
