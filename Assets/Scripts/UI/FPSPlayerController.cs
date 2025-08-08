using UnityEngine;
using UnityEngine.InputSystem;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI
{
    using Interactable;

    public class FPSPlayerController : MonoBehaviour
    {
        public Camera playerCamera;
        public CharacterController characterController;

        [Header("Movement Settings")]
        public float movementSpeed = 5f;

        [Header("Look Settings")]
        public float mouseSensitivity = 2f;

        [Header("Look Constraints")]
        public float minLookAngle = -90f;
        public float maxLookAngle = 90f;

        [Header("Interaction")]
        public float interactionRange = 5f;
        public LayerMask interactionLayerMask = -1;

        private Vector2 horizontalMovement;
        private float verticalMovement;
        private Vector2 lookDeltas;
        private float currentPitch = 0f;
        private bool isCursorLocked = true;
        private bool cursorSwitchedToLocked = true;

        private void Start()
        {
            SetCursorLock(true);
        }

        private void Update()
        {
            HandleMovement();
            HandleLook();
        }

        private void HandleMovement()
        {
            Vector3 movement = Vector3.zero;
            movement += transform.forward * horizontalMovement.y;
            movement += transform.right * horizontalMovement.x;
            movement += transform.up * verticalMovement;

            characterController.Move(movementSpeed * Time.deltaTime * movement);
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

        public void OnMoveHorizontal(InputAction.CallbackContext context)
        {
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
                    TryInteract();
                }
            }
        }

        private void TryInteract()
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(UnityEngine.Screen.width / 2f, UnityEngine.Screen.height / 2f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayerMask))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    interactable.Interact();
                }
            }
        }

        public void OnEscape(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                ToggleCursorLock();
            }
        }
    }
}
