
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExperimentSupervisor : MonoBehaviour
{
    public EvolutionSimulator simulator;

    private string outputDir;

    private void Start()
    {
        TrialType? trial = null;
        int? maxIterations = null;
        int? populationSize = null;
        float? survivalPercentage = null;
        float? mutationRate = null;
        int? batchSize = null;

        // Assuming the command line arguments are in the format "-name value".
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-outputDir":
                    outputDir = args[i + 1];
                    break;
                case "-trial":
                    trial = (TrialType)Enum.Parse(typeof(TrialType), args[i + 1]);
                    break;
                case "-maxIterations":
                    maxIterations = int.Parse(args[i + 1]);
                    break;
                case "-populationSize":
                    populationSize = int.Parse(args[i + 1]);
                    break;
                case "-survivalPercentage":
                    survivalPercentage = float.Parse(args[i + 1]);
                    break;
                case "-mutationRate":
                    mutationRate = float.Parse(args[i + 1]);
                    break;
                case "-batchSize":
                    batchSize = int.Parse(args[i + 1]);
                    break;
            }
        }

        if (outputDir == null)
            MissingParameter("outputDir");
        if (trial == null)
            MissingParameter("trial");
        if (maxIterations == null)
            MissingParameter("maxIterations");
        if (populationSize == null)
            MissingParameter("populationSize");
        if (survivalPercentage == null)
            MissingParameter("survivalPercentage");
        if (mutationRate == null)
            MissingParameter("mutationRate");
        if (batchSize == null)
            MissingParameter("batchSize");

        simulator.OnSimulationEnd += FinishExperiment;

        Debug.Log("Running experiment..." + "\n" +
            "Trial: " + trial.ToString() + "\n" +
            "Max Iterations: " + maxIterations.ToString() + "\n" +
            "Population Size: " + populationSize.ToString() + "\n" +
            "Survival Percentage: " + survivalPercentage.ToString() + "\n" +
            "Mutation Rate: " + mutationRate.ToString() + "\n" +
            "Batch Size: " + batchSize.ToString() + "\n"
        );

        // Run as fast as possible.
        WorldManager.Instance.targetTimeScale = 100f;

        simulator.StartSimulation(
            (TrialType)trial,
            (int)maxIterations,
            (int)populationSize,
            (float)survivalPercentage,
            (float)mutationRate,
            null,
            (int)batchSize
        );
    }

    private void MissingParameter(string parameterName)
    {
        Debug.Log("Missing required parameter '" + parameterName + "'.");
        Application.Quit();
    }

    private void FinishExperiment()
    {
        Debug.Log("Finished experiment.");
        Debug.Log("Saving results...");
        List<DataColumn> data = new()
        {
            new() { header = "trial", data = new() { simulator.Trial.ToString() }},
            new() { header = "maxIterations", data = new() { simulator.MaxIterations.ToString() }},
            new() { header = "populationSize", data = new() { simulator.PopulationSize.ToString() }},
            new() { header = "survivalPercentage", data = new() { simulator.SurvivalPercentage.ToString() }},
            new() { header = "mutationRate", data = new() { simulator.MutationRate.ToString() }},
            new() { header = " ", data = new() { }},
            new() { header = "iteration", data = Enumerable.Range(1, simulator.currentIteration).Cast<object>().ToList()},
            new() { header = "bestFitnesses", data = simulator.bestFitnesses.Cast<object>().ToList()},
            new() { header = "averageFitnesses", data = simulator.averageFitnesses.Cast<object>().ToList()},
            new() { header = "iterationElapsedSeconds", data = simulator.iterationElapsedSeconds.Cast<object>().ToList()},
        };

        string identifier = "Run_" +
            DateTime.Now.ToString("s") +
            "_T-" + simulator.Trial.ToString() +
            "_I-" + simulator.MaxIterations.ToString() +
            "_P-" + simulator.PopulationSize.ToString() +
            "_S-" + simulator.SurvivalPercentage.ToString() +
            "_M-" + simulator.MutationRate.ToString();

        string csvPath = Path.Combine(outputDir, identifier + ".csv");
        DataExporter.ExportToCSV(data, csvPath);

        string genotypePath = Path.Combine(outputDir, identifier + ".genotype");
        simulator.bestIndividual?.genotype?.SaveToFile(genotypePath);

        Application.Quit();
    }
}
