using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody), typeof(AudioSource))]
    public class CreatureCapsule : MonoBehaviour
    {
        [SerializeField] private AudioClip enterSocketSound;

        private XRGrabInteractable grab;
        private Rigidbody rb;
        private AudioSource audioSource;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            // If grabbed by a socket.
            if (IsSocket(args.interactorObject))
            {
                audioSource.PlayOneShot(enterSocketSound);
            }
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            // If released from a socket.
            if (IsSocket(args.interactorObject))
            {
                rb.isKinematic = false;
            }
        }

        static bool IsSocket(IXRInteractor it) => it is XRSocketInteractor;
    }
}
