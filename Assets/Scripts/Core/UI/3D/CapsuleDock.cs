using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    [RequireComponent(typeof(AudioSource))]
    public class CapsuleDock : MonoBehaviour
    {
        [SerializeField] private AudioClip enterSocketSound;
        [SerializeField] private XRSocketInteractor socket;

        public bool DisableFirstEnterSound = false;

        private AudioSource audioSource;

        private IXRSelectInteractable current;


        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (socket == null)
            {
                Debug.LogError("XRSocketInteractor component not found on CapsuleDock.");
                return;
            }

            socket.selectEntered.AddListener(args => current = args.interactableObject);
            socket.selectExited.AddListener(args =>
            {
                if (current == args.interactableObject) current = null;
            });
        }

        private void OnEnable()
        {
            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);

            // Destroy socketed objects when the dock is disabled.
            if (current != null)
            {
                Destroy((current as Component).gameObject);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (DisableFirstEnterSound)
            {
                DisableFirstEnterSound = false;
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

        // TODO: When a capsule docks or un-docks, save to state config.
    }
}
