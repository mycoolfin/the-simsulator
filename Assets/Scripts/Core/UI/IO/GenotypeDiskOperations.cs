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

        /// <summary>
        /// Saves a genotype to a file. If no file path is provided, prompts the user with a save file dialog.
        /// </summary>
        /// <typeparam name="TGenotype"></typeparam>
        /// <param name="genotype">The genotype to save.</param>
        /// <param name="OnComplete">Callback invoked upon completion with the result and file path.</param>
        /// <param name="filePath">Optional file path to save to. If null, a save file dialog is shown.</param>
        public static void SaveGenotypeToFilePath<TGenotype>(TGenotype genotype, Action<FileOperationResult, string> OnComplete, string filePath = null) where TGenotype : IGenotype<TGenotype>
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = StandaloneFileBrowser.SaveFilePanel("Save Genotype", "", $"{genotype.Name}.genotype", "genotype");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled, string.Empty);
                return; // User cancelled the save dialog.
            }

            try
            {
                // Normalize the path from the file dialog to ensure consistent separators.
                filePath = Path.GetFullPath(filePath);
            }
            catch (Exception)
            {
                OnComplete?.Invoke(FileOperationResult.Failure, string.Empty);
                return;
            }

            if (genotype == null)
            {
                OnComplete?.Invoke(FileOperationResult.Failure, string.Empty);
                return;
            }

            GenotypeIO.SerializeAsync(genotype, filePath, (success) =>
            {
                if (success)
                    OnComplete?.Invoke(FileOperationResult.Success, filePath);
                else
                    OnComplete?.Invoke(FileOperationResult.Failure, string.Empty);
            });
        }

        /// <summary>
        /// Loads a genotype from a file. If no file path is provided, prompts the user with an open file dialog.
        /// </summary>
        /// <typeparam name="TGenotype"></typeparam>
        /// <param name="OnComplete">Callback invoked upon completion with the result, loaded genotype, and file path.</param>
        /// <param name="filePath">Optional file path to load from. If null, an open file dialog is shown.</param>
        public static void LoadGenotypeFromFilePath<TGenotype>(Action<FileOperationResult, TGenotype, string> OnComplete, string filePath = null) where TGenotype : IGenotype<TGenotype>
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = StandaloneFileBrowser.OpenFilePanel("Load Genotype", "", "genotype", false).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled, default, string.Empty);
                return; // User cancelled the load dialog.
            }

            try
            {
                // Normalize the path from the file dialog to ensure consistent separators.
                filePath = Path.GetFullPath(filePath);
            }
            catch (Exception)
            {
                OnComplete?.Invoke(FileOperationResult.Failure, default, string.Empty);
                return;
            }

            GenotypeIO.DeserializeAsync<TGenotype>(filePath, (success, genotype) =>
            {
                if (success)
                    OnComplete?.Invoke(FileOperationResult.Success, genotype, filePath);
                else
                    OnComplete?.Invoke(FileOperationResult.Failure, default, string.Empty);
            });
        }
    }
}
