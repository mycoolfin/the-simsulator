using System;
using System.Linq;
using SFB;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    using TheSimsulator.Core.Genotype;

    public enum FileOperationResult
    {
        Success,
        Failure,
        Cancelled
    }

    public static class GenotypeDiskOperations
    {
        public static void SaveGenotypeToFilePathDialog<TGenotype>(TGenotype genotype, Action<FileOperationResult> OnComplete) where TGenotype : IGenotype<TGenotype>
        {
            string filePath = StandaloneFileBrowser.SaveFilePanel("Save Genotype", "", $"{genotype.Name}.genotype", "genotype");

            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled);
                return; // User cancelled the save dialog.
            }

            SaveGenotypeToFilePath(genotype, filePath, OnComplete);
        }

        public static void LoadGenotypeFromFilePathDialog<TGenotype>(Action<FileOperationResult, TGenotype> OnComplete) where TGenotype : IGenotype<TGenotype>
        {
            string filePath = StandaloneFileBrowser.OpenFilePanel("Load Genotype", "", "genotype", false).FirstOrDefault();

            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled, default);
                return; // User cancelled the load dialog.
            }

            LoadGenotypeFromFilePath(filePath, OnComplete);
        }

        public static void SaveGenotypeToFilePath<TGenotype>(TGenotype genotype, string filePath, Action<FileOperationResult> OnComplete) where TGenotype : IGenotype<TGenotype>
        {
            if (genotype == null)
            {
                OnComplete?.Invoke(FileOperationResult.Failure);
                return;
            }

            GenotypeIO.SerializeAsync(genotype, filePath, (success) =>
            {
                if (success)
                    OnComplete?.Invoke(FileOperationResult.Success);
                else
                    OnComplete?.Invoke(FileOperationResult.Failure);
            });
        }

        public static void LoadGenotypeFromFilePath<TGenotype>(string filePath, Action<FileOperationResult, TGenotype> OnComplete) where TGenotype : IGenotype<TGenotype>
        {
            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled, default);
                return;
            }

            GenotypeIO.DeserializeAsync<TGenotype>(filePath, (success, genotype) =>
            {
                if (success)
                    OnComplete?.Invoke(FileOperationResult.Success, genotype);
                else
                    OnComplete?.Invoke(FileOperationResult.Failure, default);
            });
        }
    }
}
