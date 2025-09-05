using UnityEngine;
using UnityEngine.InputSystem;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player.FPS
{
    public class FPSPlayerController : MonoBehaviour
    {
        public UnityEngine.Camera playerCamera;
        public CharacterController characterController;

        [Header("Movement Settings")]
        public float movementSpeed = 5f;
        public float sprintMultiplier = 2f;
        public float jumpHeight = 2f;
        public float gravity = -9.81f;

        [Header("Look Settings")]
        public float mouseSensitivity = 2f;

        [Header("Look Constraints")]
        public float minLookAngle = -90f;
        public float maxLookAngle = 90f;

        [Header("Interaction")]
        public float interactionRange = 5f;
        public LayerMask interactionLayerMask = -1;

        public bool LookingAtSomething { get; private set; }
        private RaycastHit raycastHit;
        public RaycastHit RaycastHit => raycastHit;

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
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            LookingAtSomething = Physics.Raycast(ray, out raycastHit, interactionRange, interactionLayerMask);
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
            if (cursorSwitchedToLocked)
            {
                cursorSwitchedToLocked = false;
                return; // Skip first frame after locking cursor.
            }

            float horizontalRotation = lookDeltas.x * mouseSensitivity;
            transform.Rotate(Vector3.up, horizontalRotation);

            currentPitch -= lookDeltas.y * mouseSensitivity;
            currentPitch = Mathf.Clamp(currentPitch, minLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
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
                    if (LookingAtSomething && RaycastHit.collider.TryGetComponent<ISelectable>(out var selectable))
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
    }
}
