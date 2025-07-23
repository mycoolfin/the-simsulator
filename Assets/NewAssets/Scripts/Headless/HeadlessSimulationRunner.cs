using System.IO;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Headless
{
    using Evolution;
    using Utilities;
    using TrialType = ECS.Components.Evolution.TrialType;
    using SimulationRateMode = ECS.Systems.Simulation.SimulationRate.SimulationRateMode;

    [RequireComponent(typeof(EvolutionSimulator))]
    public class HeadlessSimulationRunner : MonoBehaviour
    {
        private EvolutionSimulator evolutionSimulator;
        private EvolutionParameters evolutionParameters;
        private string outputDir;

        private void Awake()
        {
            evolutionSimulator = GetComponent<EvolutionSimulator>();
        }

        private void Start()
        {
            evolutionParameters = ParseEvolutionParameters();
            outputDir = ParseOutputDir();
            evolutionSimulator.SetEvolutionParameters(evolutionParameters);
            evolutionSimulator.SimulationRate = SimulationRateMode.Headless;
            evolutionSimulator.OnEvolutionComplete += FinishSimulation;
            evolutionSimulator.StartEvolution();
        }

        private void SaveEvolutionStatistics()
        {
            if (outputDir == null)
                return;

            // Save statistics to a CSV file.
            string filePath = Path.Combine(outputDir, GenerateFilenameFromParameters(evolutionSimulator));
            using StreamWriter writer = new(filePath, true);
            writer.WriteLine("Generation,Best Fitness,Average Fitness,Elapsed Time");
            foreach (EvolutionStatistics stat in evolutionSimulator.Statistics)
                writer.WriteLine($"{stat.Generation},{stat.BestFitness},{stat.AverageFitness},{stat.ElapsedTime}");

            Debug.Log($"Run statistics saved to {filePath}");
        }

        private void FinishSimulation()
        {
            Debug.Log("Headless simulation completed.");
            SaveEvolutionStatistics();
            Application.Quit(0);
        }

        private static EvolutionParameters ParseEvolutionParameters()
        {
            EvolutionParameters evolutionParameters = new();
            if (CommandLineArgs.TryGet("populationSize", out string populationSizeStr) && int.TryParse(populationSizeStr, out int populationSize))
                evolutionParameters.PopulationSize = populationSize;
            if (CommandLineArgs.TryGet("maxGenerations", out string maxGenerationsStr) && int.TryParse(maxGenerationsStr, out int maxGenerations))
                evolutionParameters.MaxGenerations = maxGenerations;
            if (CommandLineArgs.TryGet("survivalRate", out string survivalRateStr) && float.TryParse(survivalRateStr, out float survivalRate))
                evolutionParameters.SurvivalRate = survivalRate;
            if (CommandLineArgs.TryGet("mutationRate", out string mutationRateStr) && float.TryParse(mutationRateStr, out float mutationRate))
                evolutionParameters.MutationRate = mutationRate;
            if (CommandLineArgs.TryGet("settleSeconds", out string settleSecondsStr) && float.TryParse(settleSecondsStr, out float settleSeconds))
                evolutionParameters.SettleSeconds = settleSeconds;
            if (CommandLineArgs.TryGet("assessmentSeconds", out string assessmentSecondsStr) && float.TryParse(assessmentSecondsStr, out float assessmentSeconds))
                evolutionParameters.AssessmentSeconds = assessmentSeconds;
            if (CommandLineArgs.TryGet("trialType", out string trialTypeStr) && System.Enum.TryParse(trialTypeStr, true, out TrialType trialType))
                evolutionParameters.TrialType = trialType;

            return evolutionParameters;
        }

        private static string ParseOutputDir()
        {
            if (CommandLineArgs.TryGet("outputDir", out string outputDir))
                return outputDir;
            else
                return null;
        }

        private static string GenerateFilenameFromParameters(EvolutionSimulator s)
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"evolution_P{s.PopulationSize}_G{s.MaxGenerations}_S{s.SurvivalRate}_M{s.MutationRate}_SS{s.SettleSeconds:F1}_AS{s.AssessmentSeconds:F1}_T{s.TrialType}_{timestamp}.csv";
        }
    }
}
