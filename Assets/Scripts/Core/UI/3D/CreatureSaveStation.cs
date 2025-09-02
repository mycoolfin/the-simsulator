using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using IO;
    using Player;

    [RequireComponent(typeof(AudioSource))]
    public class CreatureSaveStation : MonoBehaviour
    {
        [SerializeField] private AudioClip savedSound;
        [SerializeField] private AudioClip loadedSound;
        [SerializeField] private CapsuleDock dock;
        [SerializeField] private PushButton saveButton;
        [SerializeField] private PushButton loadButton;
        [SerializeField] private FPSPlayerController playerController;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            saveButton.SetActive(true);
            loadButton.SetActive(true);
            saveButton.OnButtonPressed += (_) => SaveDockedCreature();
            loadButton.OnButtonPressed += (_) => LoadDockedCreature();
        }

        private void Update()
        {
            saveButton.SetDisabled(dock.DockedCapsule == null);
            loadButton.SetDisabled(dock.DockedCapsule != null);
        }

        private void SaveDockedCreature()
        {
            if (dock.DockedCapsule == null) return;

            if (playerController != null)
                playerController.SetCursorLock(false);

            dock.DockedCapsule.Creature?.SaveGenotypeToFile((result) =>
            {
                if (result == FileOperationResult.Success)
                    audioSource.PlayOneShot(savedSound);

                if (playerController != null)
                    playerController.SetCursorLock(true);
            });
        }

        private void LoadDockedCreature()
        {
            if (dock.DockedCapsule != null) return;

            if (playerController != null)
                playerController.SetCursorLock(false);

            dock.CreateAndDockEmptyCapsule(silent: false);
            dock.DockedCapsule.InitialiseFromGenotypeFilePathDialog(CapsuleEnvironment.Aquatic, (result) =>
            {
                if (result == FileOperationResult.Success)
                    audioSource.PlayOneShot(loadedSound);
                else
                {
                    ICreatureCapsule capsule = dock.DockedCapsule;
                    dock.EjectCapsule();
                    Destroy(capsule.GameObject);
                }

                if (playerController != null)
                    playerController.SetCursorLock(true);
            });
        }
    }
}
