using System.Collections;
using System.IO;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.Evolution
{
    using Utilities;

    public class HeadlessSimulationRunner : MonoBehaviour
    {
        private IEvolutionSimulator evolutionSimulator;
        private EvolutionParameters evolutionParameters;
        private string outputDir;
        private int saveBestEvery = 0;
        private string runName;
        private bool pendingGenotypeSave = false;

        private void Awake()
        {
            evolutionSimulator = GetComponent<IEvolutionSimulator>();
            if (evolutionSimulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on this GameObject.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            ParseOutputParameters();
            evolutionParameters = ParseEvolutionParameters();

            runName = GenerateRunNameFromParameters(evolutionParameters);

            evolutionSimulator.SetEvolutionParameters(evolutionParameters);
            evolutionSimulator.OnGenerationComplete += FinishGeneration;
            evolutionSimulator.OnEvolutionComplete += FinishSimulation;
            evolutionSimulator.SetSimulationHeadless();
            evolutionSimulator.StartEvolution();
        }

        private void OnDestroy()
        {
            if (evolutionSimulator != null)
            {
                evolutionSimulator.OnGenerationComplete -= FinishGeneration;
                evolutionSimulator.OnEvolutionComplete -= FinishSimulation;
            }
        }

        private void FinishGeneration(EvolutionStatistics stats, IAssessableCreature bestIndividual)
        {
            AppendEvolutionStatistics(runName, stats);
            SaveBestGenotype(runName, stats, bestIndividual);
        }

        private void FinishSimulation()
        {
            StartCoroutine(QuitWhenDone());
        }

        private IEnumerator QuitWhenDone()
        {
            while (pendingGenotypeSave)
                yield return null;

            Debug.Log("Headless simulation completed.");

            Application.Quit(0);
        }

        private void ParseOutputParameters()
        {
            if (CommandLineArgs.TryGet("outputDir", out string outputDir))
                this.outputDir = outputDir;
            if (CommandLineArgs.TryGet("saveBestEvery", out string saveBestEveryStr) && int.TryParse(saveBestEveryStr, out int saveBestEvery))
                this.saveBestEvery = saveBestEvery;

            if (this.outputDir == null && this.saveBestEvery > 0)
            {
                Debug.LogError("Argument Error: 'saveBestEvery' is greater than 0 but no outputDir was specified.");
                Application.Quit(1);
            }

            Debug.Log($"Run statistics will be saved to: {this.outputDir}.");
            if (this.saveBestEvery > 0)
                Debug.Log($"Best genotypes will be saved every {this.saveBestEvery} generations.");
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

        private void AppendEvolutionStatistics(string runName, EvolutionStatistics stats)
        {
            if (outputDir == null)
                return;

            string filePath = Path.Combine(outputDir, $"{runName}.csv");
            bool shouldWriteHeader = !File.Exists(filePath);
            using StreamWriter writer = new(filePath, append: true);
            if (shouldWriteHeader)
                writer.WriteLine("Generation,Best Fitness,Average Fitness,Elapsed Time");
            writer.WriteLine($"{stats.Generation},{stats.BestFitness},{stats.AverageFitness},{stats.ElapsedTime}");
        }

        private void SaveBestGenotype(string runName, EvolutionStatistics stats, IAssessableCreature bestIndividual)
        {
            if (outputDir == null || saveBestEvery <= 0)
                return;

            string genotypeDir = Path.Combine(outputDir, $"{runName}_genotypes");

            if (!Directory.Exists(genotypeDir))
                Directory.CreateDirectory(genotypeDir);

            if (stats.Generation % saveBestEvery == 0)
            {
                if (bestIndividual == null)
                {
                    Debug.LogWarning($"Cannot save genotype for generation {stats.Generation}: bestIndividual is null");
                    return;
                }

                string genotypePath = Path.Combine(genotypeDir, $"G{stats.Generation}.genotype");
                pendingGenotypeSave = true;
                bestIndividual.SaveGenotypeToFile((result, filePath) =>
                {
                    Debug.Log($"Saved genotype to {filePath}");
                    pendingGenotypeSave = false;
                }, genotypePath);
            }
        }

        private static string GenerateRunNameFromParameters(EvolutionParameters s)
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"evolution_P{s.PopulationSize}_G{s.MaxGenerations}_S{s.SurvivalRate}_M{s.MutationRate}_SS{s.SettleSeconds:F1}_AS{s.AssessmentSeconds:F1}_T{s.TrialType}_{timestamp}";
        }
    }
}
