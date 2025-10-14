using System;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    [RequireComponent(typeof(AudioSource))]
    public class PushButton : MonoBehaviour, ISelectable
    {
        [SerializeField] private AudioClip buttonPressedSound;
        [SerializeField] private Animator animator;
        [SerializeField] private EmitterController emitterController;
        [SerializeField] private Collider buttonCollider;
        [SerializeField] private float pushCooldown = 0.5f;
        [SerializeField] private Color inactiveColor = Color.lightGray;
        [SerializeField] private float inactiveEmissivity = 0.2f;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private float activeEmissivity = 1f;
        [SerializeField] private float disabledEmissivity = 0.00f;
        public bool IsActive { get; private set; } = false;
        public bool IsDisabled { get; private set; } = false;
        public event Action<bool> OnButtonPressed;
        public bool debugIsActive;
        public bool debugIsDisabled;

        private AudioSource audioSource;

        private float lastPushTime = 0f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = buttonPressedSound;
        }

        private void Start()
        {
            SetActive(false);
            SetDisabled(false);
        }

        private void Push()
        {
            if (Time.time - lastPushTime < pushCooldown) return;

            lastPushTime = Time.time;

            audioSource.PlayOneShot(audioSource.clip);
            animator.Play("PushInOut", 0, 0f);
            OnButtonPressed?.Invoke(IsActive);
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            debugIsActive = active;
            UpdateDisplay();
        }

        public void SetDisabled(bool disabled)
        {
            IsDisabled = disabled;
            debugIsDisabled = disabled;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            emitterController.SetEmissiveColor(IsActive ? activeColor : inactiveColor);

            if (IsDisabled)
            {
                emitterController.SetEmissiveIntensity(disabledEmissivity);
                buttonCollider.enabled = false;
            }
            else
            {
                emitterController.SetEmissiveIntensity(IsActive ? activeEmissivity : inactiveEmissivity);
                buttonCollider.enabled = true;
            }
        }

        public Vector3 WorldPosition => transform.position;
        public Quaternion WorldRotation => transform.rotation;
        public Bounds Bounds => buttonCollider.bounds;

        public void Select()
        {
            Push();
        }
    }
}
