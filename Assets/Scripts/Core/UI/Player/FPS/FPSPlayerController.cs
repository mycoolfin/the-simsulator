using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player.FPS
{
    public class FPSPlayerController : MonoBehaviour, IXRRayProvider
    {
        [SerializeField] private UnityEngine.Camera playerCamera;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private XRRayInteractor desktopHandInteractor;

        [Header("Movement Settings")]
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 1f;
        [SerializeField] private float degreesPerScreenHeight = 240f;

        [Header("Look Constraints")]
        [SerializeField] private float minLookAngle = -90f;
        [SerializeField] private float maxLookAngle = 90f;

        private RaycastHit raycastHit;

        // IXRRayProvider implementation
        public Transform rayEndTransform => raycastHit.collider != null ? raycastHit.collider.transform : null;
        public Vector3 rayEndPoint => raycastHit.point;

        private Vector2 horizontalMovement;
        private Vector2 lookDeltas;
        private float currentPitch = 0f;
        private bool isCursorLocked = true;
        private bool cursorSwitchedToLocked = true;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isSprinting = false;

        public static bool KeyboardControlOverridden = false;

        private void Start()
        {
            SetCursorLock(true);
        }

        private void Update()
        {
            UpdateRaycastHit();
            HandleMovement();
            HandleLook();
        }

        private void UpdateRaycastHit()
        {
            desktopHandInteractor.TryGetCurrent3DRaycastHit(out raycastHit);
        }

        private void HandleMovement()
        {
            // Check if grounded
            isGrounded = characterController.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Keep player grounded
            }

            // Calculate movement direction
            Vector3 movement = Vector3.zero;
            movement += transform.forward * horizontalMovement.y;
            movement += transform.right * horizontalMovement.x;

            // Apply sprint multiplier if sprinting
            float currentSpeed = isSprinting ? movementSpeed * sprintMultiplier : movementSpeed;
            movement *= currentSpeed;

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            movement += velocity;

            characterController.Move(movement * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (!isCursorLocked || lookDeltas.sqrMagnitude < 0.01f) return;
            if (cursorSwitchedToLocked) { cursorSwitchedToLocked = false; return; }

            float yawDeg = NormaliseLookDelta(lookDeltas.x) * mouseSensitivity;
            float pitchDeg = NormaliseLookDelta(-lookDeltas.y) * mouseSensitivity;

            transform.Rotate(0f, yawDeg, 0f, Space.World);

            currentPitch = Mathf.Clamp(currentPitch + pitchDeg, minLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        private float NormaliseLookDelta(float delta)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return delta; // macOS seems to report normalised deltas already.
#else
            // Normalise to current rendering height so a full screen-height swipe
            // always maps to the same degrees, regardless of resolution.
            float renderH = Mathf.Max(1f, GetRenderingDisplayHeight());
            float pxToDeg = degreesPerScreenHeight / renderH;
            return delta * pxToDeg;
#endif
        }

        private int GetActiveDisplayIndex()
        {
            var p = Display.RelativeMouseAt(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            int idx = (int)p.z;
            return (idx >= 0 && idx < Display.displays.Length) ? idx : 0;
        }

        private int GetRenderingDisplayHeight()
        {
            int di = GetActiveDisplayIndex();
            int rh = (di >= 0 && di < Display.displays.Length) ? Display.displays[di].renderingHeight : 0;
            return (rh > 0) ? rh : Screen.height;
        }

        public void SetCursorLock(bool lockCursor)
        {
            isCursorLocked = lockCursor;
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorSwitchedToLocked = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void ToggleCursorLock()
        {
            SetCursorLock(!isCursorLocked);
        }

        public void LockCursor()
        {
            SetCursorLock(true);
        }

        public void UnlockCursor()
        {
            SetCursorLock(false);
        }

        public bool IsCursorLocked => isCursorLocked;
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;

        public void OnMoveHorizontal(InputAction.CallbackContext context)
        {
            if (KeyboardControlOverridden && context.control.device is Keyboard)
            {
                horizontalMovement = Vector2.zero;
                return; // Ignore keyboard input while something is overriding our control.
            }

            horizontalMovement = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            lookDeltas = context.ReadValue<Vector2>();
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (!isCursorLocked)
                {
                    ToggleCursorLock();
                }
                else
                {
                    if (rayEndTransform != null && rayEndTransform.TryGetComponent<ISelectable>(out var selectable))
                        selectable.Select();
                }
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (KeyboardControlOverridden && context.control.device is Keyboard)
                return; // Ignore keyboard input while something is overriding our control.

            if (context.started && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (KeyboardControlOverridden && context.control.device is Keyboard)
                return; // Ignore keyboard input while something is overriding our control.

            if (context.started)
            {
                isSprinting = true;
            }
            else if (context.canceled)
            {
                isSprinting = false;
            }
        }

        public void OnEscape(InputAction.CallbackContext context)
        {
            if (KeyboardControlOverridden && context.control.device is Keyboard)
                return; // Ignore keyboard input while something is overriding our control.

            if (context.started)
            {
                ToggleCursorLock();
            }
        }

        // IXRRayProvider method implementations
        public Transform GetOrCreateAttachTransform()
        {
            // Return the XRRayInteractor's attach transform
            return desktopHandInteractor.attachTransform;
        }

        public void SetAttachTransform(Transform newAttach)
        {
            // Set the XRRayInteractor's attach transform
            desktopHandInteractor.attachTransform = newAttach;
        }

        public Transform GetOrCreateRayOrigin()
        {
            // Return the camera transform as the ray origin for FPS controller
            return playerCamera.transform;
        }

        public void SetRayOrigin(Transform newOrigin)
        {
            // For FPS controller, the ray origin should always be the camera
            Debug.LogWarning("SetRayOrigin called on FPSPlayerController - ray origin should remain as the player camera.");
        }
    }
}
