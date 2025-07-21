using System;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Presentation.Interaction
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(EmitterController))]
    public class PushButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private float inactiveEmissivity = 0.1f;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private float activeEmissivity = 1f;
        public bool IsActive { get; private set; } = false;
        public event Action<bool> OnButtonPressed;

        private AudioSource audioSource;
        private EmitterController emitterController;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            emitterController = GetComponent<EmitterController>();
            SetActive(false);
        }

        private void Push()
        {
            audioSource.PlayOneShot(audioSource.clip);
            OnButtonPressed?.Invoke(IsActive);
        }

        public void SetActive(bool active)
        {
            IsActive = active;

            emitterController.SetEmissiveColor(active ? activeColor : inactiveColor);
            emitterController.SetEmissiveIntensity(active ? activeEmissivity : inactiveEmissivity);
        }

        public void Interact()
        {
            Push();
        }
    }
}
