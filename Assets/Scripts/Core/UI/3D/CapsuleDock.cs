using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Newtonsoft.Json.Linq;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using IO;

    [RequireComponent(typeof(AudioSource))]
    public class CapsuleDock : SaveableBehaviour
    {
        [SerializeField] private GameObject capsulePrefab;
        [SerializeField] private AudioClip enterSocketSound;
        [SerializeField] private XRSocketInteractor socket;
        public XRSocketInteractor Socket => socket;
        [SerializeField] private DockNameplate nameplate;

        private bool disableFirstEnterSound = false;

        private AudioSource audioSource;

        public ICreatureCapsule DockedCapsule { get; private set; }

        [SerializeField] private string saveId = string.Empty;
        public override string SaveId => saveId;
        public override int SaveVersion => 1;
        [Serializable]
        private class CapsuleDockData
        {
            public string GenotypeFilePath;
            public CapsuleEnvironment Environment;
        }
        public override object CaptureState()
        {
            return DockedCapsule == null ? null : new CapsuleDockData
            {
                GenotypeFilePath = DockedCapsule.GenotypeFilePath,
                Environment = DockedCapsule.Environment
            };
        }
        public override void RestoreState(JObject payload, int version)
        {
            CapsuleDockData data = payload.ToObject<CapsuleDockData>();
            if (data == null) return;

            if (!string.IsNullOrEmpty(data.GenotypeFilePath))
            {
                EjectCapsule();
                CreateAndDockEmptyCapsule();
                DockedCapsule.InitialiseFromGenotypeFilePath(data.GenotypeFilePath, data.Environment);
            }
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (socket == null)
            {
                Debug.LogError("XRSocketInteractor component not found on CapsuleDock.");
                return;
            }

            socket.selectEntered.AddListener(args =>
            {
                if ((args.interactableObject as Component).TryGetComponent(out ICreatureCapsule capsule))
                {
                    DockedCapsule = capsule;
                    Refresh();
                }
            });
            socket.selectExited.AddListener(args =>
            {
                if (DockedCapsule == (args.interactableObject as Component).GetComponent<ICreatureCapsule>())
                {
                    DockedCapsule = null;
                    Refresh();
                }
            });
        }

        public void Refresh()
        {
            nameplate.AssignCreatureCapsule(DockedCapsule);
            NotifyChanged();
        }

        public void CreateAndDockEmptyCapsule(bool silent = true)
        {
            if (silent) disableFirstEnterSound = true;
            GameObject capsuleObject = Instantiate(capsulePrefab, Socket.transform.position, Socket.transform.rotation);
            ICreatureCapsule capsule = capsuleObject.GetComponent<ICreatureCapsule>();
            DockedCapsule = capsule;
        }

        public void EjectCapsule(float pushVelocity = 1.5f, float randomSpin = 0.5f)
        {
            DockedCapsule = null;

            if (socket == null || socket.interactionManager == null)
                return;

            List<IXRSelectInteractable> selected = socket.interactablesSelected;
            if (selected == null || selected.Count == 0)
                return;

            IXRSelectInteractable interactable = selected[0];
            socket.interactionManager.SelectExit(socket, interactable);

            Transform tr = interactable.transform;
            if (tr != null && tr.TryGetComponent<Rigidbody>(out var rb) && rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = socket.transform.forward * pushVelocity;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * randomSpin;
            }
        }

        public void DestroyCapsule()
        {
            if (DockedCapsule != null && DockedCapsule.GameObject != null && !DockedCapsule.GameObject.IsDestroyed())
            {
                Destroy(DockedCapsule.GameObject);
                DockedCapsule = null;
            }
        }

        public void SetNameplateEnabled(bool enabled)
        {
            nameplate.gameObject.SetActive(enabled);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);

            // Destroy socketed objects when the dock is disabled.
            if (DockedCapsule != null && !(DockedCapsule as Component).IsDestroyed())
                Destroy((DockedCapsule as Component).gameObject);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (disableFirstEnterSound)
            {
                disableFirstEnterSound = false;
                return;
            }
            if (enterSocketSound) audioSource.PlayOneShot(enterSocketSound);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            MonoBehaviour mb = args.interactableObject as MonoBehaviour;
            if (mb != null && mb.TryGetComponent(out Rigidbody rigidbody))
                rigidbody.isKinematic = false;
        }
    }
}
