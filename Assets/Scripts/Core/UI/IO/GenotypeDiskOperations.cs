using System;
using System.IO;
using System.Linq;
using UnityEngine;
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
        private static readonly string defaultGenotypeStoragePath = Path.Combine(Application.persistentDataPath, "genotypes");

        public static string PrepareGenotypeSavePathInDefaultStorage(string genotypeName)
        {
            Directory.CreateDirectory(defaultGenotypeStoragePath);

            GenotypeFilePurger.TryPurgeIfOverCap(defaultGenotypeStoragePath);

            string safeName = SanitizeFileName(genotypeName);
            return Path.Combine(defaultGenotypeStoragePath, $"{safeName}.genotype");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        public static string RenameGenotypeFile(string oldFilePath, string newName)
        {
            if (string.IsNullOrEmpty(oldFilePath) || string.IsNullOrEmpty(newName))
                return null;

            string newFilePath = Path.Combine(Path.GetDirectoryName(oldFilePath), $"{SanitizeFileName(newName)}.genotype");

            if (File.Exists(newFilePath))
                return null;

            File.Move(oldFilePath, newFilePath);

            return newFilePath;
        }

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

        public static void LoadGenotypeFromFilePathDialog<TGenotype>(Action<FileOperationResult, TGenotype, string> OnComplete) where TGenotype : IGenotype<TGenotype>
        {
            string filePath = StandaloneFileBrowser.OpenFilePanel("Load Genotype", "", "genotype", false).FirstOrDefault();

            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled, default, string.Empty);
                return; // User cancelled the load dialog.
            }

            Action<FileOperationResult, TGenotype> callback = (result, genotype) =>
            {
                OnComplete?.Invoke(result, genotype, filePath);
            };
            LoadGenotypeFromFilePath(filePath, callback);
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
