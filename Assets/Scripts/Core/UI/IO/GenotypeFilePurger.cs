using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    public static class GenotypeFilePurger
    {
        public static void TryPurgeIfOverCap(string genotypeDir, int minFilesToKeep = 20, int maxFilesToKeep = 50)
        {
            try
            {
                DirectoryInfo dir = new(genotypeDir);
                if (!dir.Exists) return;

                // Stop early if not over cap.
                int count = dir.EnumerateFiles("*.genotype", SearchOption.AllDirectories)
                               .Take(maxFilesToKeep + 1).Count();
                if (count <= maxFilesToKeep) return;

                PurgeOldGenotypeFiles(genotypeDir, minFilesToKeep);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Genotype File Purger] Purge check failed: {ex.Message}");
            }
        }

        private static void PurgeOldGenotypeFiles(string genotypeDir, int keepCount, bool recurse = true, bool preferCreationTime = false)
        {
            DirectoryInfo dir = new(genotypeDir);
            if (!dir.Exists) return;

            List<FileInfo> files = dir.GetFiles("*.genotype", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                       .Where(f => f.Exists)
                                       .ToList();

            int total = files.Count;
            int toDelete = Math.Max(0, total - keepCount);
            if (toDelete == 0) return;

            // Build normalised reference set (absolute paths under root).
            HashSet<string> refs = GenotypeFileReferenceFinder.FindAllInScene(genotypeDir);

            // Oldest-first sort.
            DateTime Key(FileInfo fi) => preferCreationTime ? fi.CreationTimeUtc : fi.LastWriteTimeUtc;
            files.Sort((a, b) => Key(a).CompareTo(Key(b)));

            // Only delete unreferenced files.
            List<FileInfo> candidates = files.Where(fi => !refs.Contains(fi.FullName)).ToList();

            if (candidates.Count < toDelete)
            {
                Debug.LogWarning($"[Genotype File Purger] Only found {candidates.Count} unreferenced files, need {toDelete} to meet cap {keepCount}.");
                toDelete = candidates.Count;
            }

            int deleted = 0;
            foreach (FileInfo fi in candidates)
            {
                if (deleted >= toDelete) break;
                try
                {
                    fi.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Genotype File Purger] Failed to delete {fi.FullName}: {ex.Message}");
                }
            }

            Debug.Log($"[Genotype File Purger] Deleted {deleted} genotype files.");
        }
    }
}
