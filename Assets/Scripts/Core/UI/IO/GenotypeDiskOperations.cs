using System;
using System.Collections;
using System.Linq;
using SFB;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    using TheSimsulator.Core.Genotype;

    public static class GenotypeDiskOperations
    {
        public static IEnumerator SaveGenotypeToFile<TGenotype>(TGenotype genotype) where TGenotype : IGenotype<TGenotype>
        {
            if (genotype == null)
            {
                UnityEngine.Debug.LogError("Can't save null genotype.");
                yield break;
            }

            string filePath = StandaloneFileBrowser.SaveFilePanel("Save Genotype", "", $"{genotype.Name}.genotype", "genotype");

            if (string.IsNullOrEmpty(filePath))
                yield break; // User cancelled the save dialog.

            bool? saveSuccess = null;
            GenotypeIO.SerializeAsync(genotype, filePath, (success) => saveSuccess = success);
            while (!saveSuccess.HasValue)
                yield return null; // Wait until the save operation is complete.

            if (saveSuccess.Value) UnityEngine.Debug.Log($"Genotype saved successfully to {filePath}");
            else UnityEngine.Debug.LogError("Failed to save genotype.");
        }

        public static IEnumerator LoadGenotypeFromFile<TGenotype>(Action<TGenotype> OnGenotypeLoaded) where TGenotype : IGenotype<TGenotype>
        {
            string filePath = StandaloneFileBrowser.OpenFilePanel("Load Genotype", "", "genotype", false).FirstOrDefault();

            if (string.IsNullOrEmpty(filePath))
                yield break; // User cancelled the load dialog.

            bool? loadSuccess = null;
            TGenotype genotype = default;
            GenotypeIO.DeserializeAsync<TGenotype>(filePath, (success, g) =>
            {
                genotype = g;
                loadSuccess = success;
            });

            while (!loadSuccess.HasValue)
                yield return null; // Wait until the load operation is complete.

            if (loadSuccess.Value)
            {
                UnityEngine.Debug.Log($"Genotype loaded successfully from {filePath}");
                OnGenotypeLoaded?.Invoke(genotype);
            }
            else
                UnityEngine.Debug.LogError("Failed to load genotype.");
        }
    }
}
