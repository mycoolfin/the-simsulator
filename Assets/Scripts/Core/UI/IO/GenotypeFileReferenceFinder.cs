using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    public interface IGenotypeFileReferenceProvider
    {
        IEnumerable<string> GetReferencedGenotypePaths(); // May be relative or absolute.
    }

    public static class GenotypeFileReferenceFinder
    {
        public static HashSet<string> FindAllInScene(string root)
        {
            // Normalize root to absolute and ensure trailing separator for safety.
            root = NormalizeRoot(root);

            bool isWindows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
            StringComparer cmp = isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            // Collect and normalise all refs to absolute under root.
            HashSet<string> refs = UnityEngine.Object
                .FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<IGenotypeFileReferenceProvider>()
                .SelectMany(p => p.GetReferencedGenotypePaths() ?? Array.Empty<string>())
                .Select(p => ToAbsoluteUnderRoot(p, root))
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet(cmp);

            return refs;
        }

        private static string NormalizeRoot(string root)
        {
            if (string.IsNullOrEmpty(root)) return root;
            try { root = Path.GetFullPath(root); } catch { /* keep original */ }
            // Ensure trailing separator so later Path.Combine logic is predictable.
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                root += Path.DirectorySeparatorChar;
            return root;
        }

        public static string ToAbsoluteUnderRoot(string relOrAbs, string root)
        {
            if (string.IsNullOrEmpty(relOrAbs)) return null;

            string path = Path.IsPathRooted(relOrAbs)
                ? relOrAbs
                : Path.Combine(root, relOrAbs);

            try { return Path.GetFullPath(path); }
            catch { return path; }
        }
    }
}
