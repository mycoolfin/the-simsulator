using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    public sealed class SaveManager : MonoBehaviour
    {
        [SerializeField] private string saveGraphFileName = "savegraph.json";
        [SerializeField, Min(0f)] private float debounceSeconds = 0.2f;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool saveOnQuit = true;
        [SerializeField] private bool verboseLogging = false;

        public string SaveGraphPath => Path.Combine(Application.persistentDataPath, saveGraphFileName);

        [Serializable]
        private sealed class NodeRecord
        {
            public string type;
            public int version;
            public JObject payload;
        }

        private const int SaveGraphFileVersion = 1;

        [Serializable]
        private sealed class SaveFile
        {
            public int fileVersion = SaveGraphFileVersion;
            public Dictionary<string, NodeRecord> nodes = new();
        }

        private readonly Dictionary<string, ISaveable> registry = new();
        private float pendingWriteAt = -1f;

        private void Start()
        {
            if (loadOnStart) Load();
        }

        private void Update()
        {
            if (pendingWriteAt > 0f && Time.unscaledTime >= pendingWriteAt)
            {
                pendingWriteAt = -1f;
                Save();
            }
        }

        private void OnDisable()
        {
            foreach (ISaveable s in registry.Values.ToArray())
                s.OnChange -= OnNodeChanged;

            registry.Clear();
        }

        private void OnApplicationQuit()
        {
            if (saveOnQuit)
            {
                // Best-effort final save; ignore debounce.
                TrySaveSilently();
            }
        }

        public void Register(ISaveable s)
        {
            if (s == null) return;

            if (string.IsNullOrEmpty(s.SaveId))
                return; // Ignore.

            // Handle duplicates deterministically (keep first).
            if (registry.TryGetValue(s.SaveId, out ISaveable existing) && !ReferenceEquals(existing, s))
            {
                Debug.LogWarning($"[SaveManager] Duplicate SaveId '{s.SaveId}' on {s} — keeping first ({existing}).");
                return;
            }

            registry[s.SaveId] = s;
            s.OnChange -= OnNodeChanged; // Avoid double-subscribe if re-registered.
            s.OnChange += OnNodeChanged;
        }

        public void Unregister(ISaveable s)
        {
            if (s == null) return;

            if (registry.TryGetValue(s.SaveId, out var existing) && ReferenceEquals(existing, s))
                registry.Remove(s.SaveId);

            s.OnChange -= OnNodeChanged;
        }

        private void OnNodeChanged(ISaveable _)
        {
            // Debounce/batch multiple object changes.
            pendingWriteAt = Time.unscaledTime + debounceSeconds;
        }

        [ContextMenu("Save Now")]
        public void Save()
        {
#if UNITY_WEBGL
        Debug.LogWarning("[SaveManager] File I/O is not supported on WebGL. Implement a WebGL-specific storage path.");
        return;
#else
            try
            {
                SaveFile file = BuildSaveFile();
                WriteAtomicJson(SaveGraphPath, file);
                if (verboseLogging)
                    Debug.Log($"[SaveManager] Saved {file.nodes.Count} nodes → {SaveGraphPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex}");
            }
#endif
        }

        [ContextMenu("Load Now")]
        public void Load()
        {
#if UNITY_WEBGL
        Debug.LogWarning("[SaveManager] File I/O is not supported on WebGL. Implement a WebGL-specific storage path.");
        return;
#else
            if (!File.Exists(SaveGraphPath))
            {
                Debug.Log($"[SaveManager] No save file at {SaveGraphPath}");
                return;
            }

            try
            {
                var json = File.ReadAllText(SaveGraphPath);
                var file = JsonConvert.DeserializeObject<SaveFile>(json);

                if (file == null || file.nodes == null)
                {
                    Debug.LogWarning("[SaveManager] Save file empty or unreadable.");
                    return;
                }

                if (file.fileVersion != SaveGraphFileVersion)
                {
                    Debug.LogError($"[SaveManager] Unsupported save file version {file.fileVersion}");
                    return;
                }

                int applied = 0;
                foreach (var (id, record) in file.nodes)
                {
                    if (record?.payload == null) continue;
                    if (!registry.TryGetValue(id, out var node)) continue; // Node not present in scene.

                    // Let each node handle its own schema version migration.
                    node.RestoreState(record.payload, record.version);
                    applied++;
                }

                if (verboseLogging)
                    Debug.Log($"[SaveManager] Loaded {applied}/{file.nodes.Count} nodes from {SaveGraphPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex}");
            }
#endif
        }

        private SaveFile BuildSaveFile()
        {
            SaveFile file = new();

            foreach (var (id, node) in registry)
            {
                // Capture arbitrary serializable state and convert to JObject.
                object state = node.CaptureState();
                if (state == null) continue;
                JToken payloadToken = JToken.FromObject(state);
                file.nodes[id] = new()
                {
                    type = node.GetType().AssemblyQualifiedName,
                    version = node.SaveVersion,
                    payload = (JObject)payloadToken
                };
            }

            return file;
        }

        private void TrySaveSilently()
        {
#if !UNITY_WEBGL
            try
            {
                SaveFile file = BuildSaveFile();
                WriteAtomicJson(SaveGraphPath, file);
            }
            catch { }
#endif
        }

        private static void WriteAtomicJson(string path, SaveFile file)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            JsonSerializerSettings settings = new()
            {
                Culture = System.Globalization.CultureInfo.InvariantCulture,
                NullValueHandling = NullValueHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(file, Formatting.Indented, settings);
            string tmp = path + ".tmp";

            File.WriteAllText(tmp, json);

            try
            {
                if (File.Exists(path))
                {
                    // Atomic overwrite.
                    File.Replace(tmp, path, null);
                }
                else
                {
                    // First time write.
                    File.Move(tmp, path);
                }
            }
            catch (PlatformNotSupportedException)
            {
                // Fallback for platforms that don't support Replace (e.g., Android).
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch
            {
                // Best-effort cleanup on unexpected errors.
                if (File.Exists(tmp)) File.Delete(tmp);
                throw;
            }
        }
    }
}
