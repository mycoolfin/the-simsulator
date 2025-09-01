using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using IO;
    using Player;

    [RequireComponent(typeof(AudioSource))]
    public class CreatureSaveStation : MonoBehaviour
    {
        [SerializeField] private AudioClip savedSound;
        [SerializeField] private AudioClip loadedSound;
        [SerializeField] private GameObject capsulePrefab;
        [SerializeField] private CapsuleDock dock;
        [SerializeField] private PushButton saveButton;
        [SerializeField] private PushButton loadButton;
        [SerializeField] private FPSPlayerController playerController;

        private AudioSource audioSource;

        private ICreatureCapsule dockedCapsule;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            dock.Socket.selectEntered.AddListener(OnSelectEntered);
            dock.Socket.selectExited.AddListener(OnSelectExited);

            saveButton.SetActive(true);
            loadButton.SetActive(true);
            saveButton.OnButtonPressed += (_) => SaveDockedCreature();
            loadButton.OnButtonPressed += (_) => LoadDockedCreature();
        }

        private void Update()
        {
            saveButton.SetDisabled(dockedCapsule == null);
            loadButton.SetDisabled(dockedCapsule != null);
        }

        private void OnDisable()
        {
            dock.Socket.selectEntered.RemoveListener(OnSelectEntered);
            dock.Socket.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if ((args.interactableObject as Component).TryGetComponent(out XRGrabInteractable grabInteractable))
            {
                if (grabInteractable.gameObject.TryGetComponent(out ICreatureCapsule capsule))
                    dockedCapsule = capsule;
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if ((args.interactableObject as Component).TryGetComponent(out XRGrabInteractable grabInteractable))
                if (grabInteractable.gameObject.TryGetComponent(out ICreatureCapsule capsule))
                    dockedCapsule = null;
        }

        private void SaveDockedCreature()
        {
            if (playerController != null)
                playerController.SetCursorLock(false);

            dockedCapsule.Creature?.SaveGenotypeToFile((result) =>
            {
                if (result == FileOperationResult.Success)
                    audioSource.PlayOneShot(savedSound);

                if (playerController != null)
                    playerController.SetCursorLock(true);
            });
        }

        private void LoadDockedCreature()
        {
            if (playerController != null)
                playerController.SetCursorLock(false);

            GameObject capsuleObject = Instantiate(capsulePrefab, dock.Socket.transform.position, dock.Socket.transform.rotation);
            ICreatureCapsule capsule = capsuleObject.GetComponent<ICreatureCapsule>();
            capsule.InitialiseFromGenotypeFilePathDialog(CapsuleEnvironment.Aquatic, (result) =>
            {
                if (result == FileOperationResult.Success)
                    audioSource.PlayOneShot(loadedSound);
                else
                    Destroy(capsuleObject);

                if (playerController != null)
                    playerController.SetCursorLock(true);
            });
        }
    }
}
