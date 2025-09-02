
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    public interface ISaveable
    {
        string SaveId { get; }
        int SaveVersion { get; }
        object CaptureState();
        void RestoreState(JObject payload, int version);
        event Action<ISaveable> OnChange;
    }

    public abstract class SaveableBehaviour : MonoBehaviour, ISaveable
    {
        public abstract string SaveId { get; }
        public abstract int SaveVersion { get; }
        public abstract object CaptureState();
        public abstract void RestoreState(JObject payload, int version);
        public event Action<ISaveable> OnChange;
        protected void NotifyChanged() => OnChange?.Invoke(this);

        protected virtual void OnEnable()
        {
            SaveManager saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager != null) saveManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            SaveManager saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager != null) saveManager.Unregister(this);
        }
    }
}
