using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public PlayerInput playerInput;
    public Camera playerCamera;
    public float movementMultiplier;
    public float lookMultiplier;
    public float scrollMultiplier;
    public float panMultiplier;
    [SerializeField] private Vector2 horizontalMovement;
    [SerializeField] private float verticalMovement;
    [SerializeField] private Vector2 lookDeltas;
    [SerializeField] private Vector2 scrollDeltas;
    [SerializeField] private Vector2 cursorPosition;
    [SerializeField] private bool primarySelectActive;
    [SerializeField] private bool secondarySelectActive;
    [SerializeField] private bool tertiarySelectActive;
    [SerializeField] private bool isPointerOverUI;

    private void Update() // Using Update so that we can still move when time is stopped.
    {
        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        HandleLook();
        HandleMove();

        if (PickAndPlaceManager.Instance.Held != null)
            VisualisePlacement();
    }

    private void HandleLook()
    {
        if (secondarySelectActive)
        {
            transform.rotation *= Quaternion.Euler(0f, lookDeltas.x * lookMultiplier, 0f);
            float verticalLook = -lookDeltas.y * lookMultiplier;
            playerCamera.transform.localRotation *= Quaternion.Euler(verticalLook, 0f, 0f);

            float currentVerticalAngle = playerCamera.transform.localRotation.eulerAngles.x;
            if (currentVerticalAngle > 180)
                currentVerticalAngle -= 360;

            if (currentVerticalAngle > 80f)
                playerCamera.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            else if (currentVerticalAngle < -80f)
                playerCamera.transform.localRotation = Quaternion.Euler(-80f, 0f, 0f);
        }
    }

    private void HandleMove()
    {
        Vector3 movementVector;
        if (tertiarySelectActive)
            movementVector = playerCamera.transform.localRotation * new Vector3(-lookDeltas.x, -lookDeltas.y, 0f) * panMultiplier;
        else
            movementVector = new Vector3(horizontalMovement.x, verticalMovement, horizontalMovement.y) * movementMultiplier;

        if (!isPointerOverUI && horizontalMovement == Vector2.zero && verticalMovement == 0f) // Can use scrollDeltas instead.
            movementVector += playerCamera.transform.localRotation * Vector3.forward * scrollDeltas.y * scrollMultiplier;

        transform.position += transform.rotation * movementVector;
    }

    private void VisualisePlacement()
    {
        if (isPointerOverUI)
            PickAndPlaceManager.Instance.ShowCursor(false);
        else
        {
            RaycastHit hit;
            Ray ray = playerCamera.ScreenPointToRay(cursorPosition);
            if (Physics.Raycast(ray, out hit))
            {
                PickAndPlaceManager.Instance.ShowCursor(true);
                PickAndPlaceManager.Instance.MoveCursorTo(hit.point);
            }
        }
    }

    private void AttemptPlacement()
    {
        if (isPointerOverUI)
            return;

        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(cursorPosition);
        if (Physics.Raycast(ray, out hit))
            PickAndPlaceManager.Instance.Place(hit.point);
    }

    private void AttemptSelection()
    {
        if (isPointerOverUI)
            return;

        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(cursorPosition);
        if (Physics.Raycast(ray, out hit))
        {
            ISelectable selected = hit.transform.GetComponentInParent<ISelectable>();
            if (selected != null)
            {
                selected.Select();
                return;
            }
        }
        SelectionManager.Instance.Selected = null;
    }

    public void MoveHorizontal(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>();
    }

    public void MoveVertical(InputAction.CallbackContext context)
    {
        verticalMovement = context.ReadValue<float>();
    }
    public void Look(InputAction.CallbackContext context)
    {
        lookDeltas = context.ReadValue<Vector2>();
    }

    public void Point(InputAction.CallbackContext context)
    {
        cursorPosition = context.ReadValue<Vector2>();
    }

    public void Click(InputAction.CallbackContext context)
    {
        primarySelectActive = context.ReadValue<float>() == 1f;
        if (primarySelectActive)
        {
            if (PickAndPlaceManager.Instance.Held != null)
                AttemptPlacement();
            else
                AttemptSelection();
        }
    }

    public void ScrollWheel(InputAction.CallbackContext context)
    {
        scrollDeltas = context.ReadValue<Vector2>();
    }

    public void MiddleClick(InputAction.CallbackContext context)
    {
        tertiarySelectActive = context.ReadValue<float>() == 1f;
    }

    public void RightClick(InputAction.CallbackContext context)
    {
        secondarySelectActive = context.ReadValue<float>() == 1f;
    }
}