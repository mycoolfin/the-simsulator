using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public Transform orbitTarget;
    public float movementMultiplier;
    public float lookMultiplier;
    public float scrollMultiplier;
    public float panMultiplier;
    private Vector2 horizontalMovement;
    private float verticalMovement;
    private Vector2 lookDeltas;
    private Vector2 scrollDeltas;
    private Vector2 cursorPosition;
    private bool primarySelectActive;
    private bool secondarySelectActive;
    private bool tertiarySelectActive;
    private bool isPointerOverUI;

    private void Update() // Using Update so that we can still move when time is stopped.
    {
        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        if (orbitTarget != null)
            OrbitTarget();

        HandleLook();
        HandleMove();
        HandleZoom();

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

        transform.position += transform.rotation * movementVector;
    }

    private void HandleZoom()
    {
        if (!isPointerOverUI && horizontalMovement == Vector2.zero && verticalMovement == 0f)
        {
            Vector3 movementVector = playerCamera.transform.localRotation * Vector3.forward * scrollDeltas.y * scrollMultiplier;
            transform.position += transform.rotation * movementVector;
        }
    }

    private void OrbitTarget()
    {
        Vector3 toTarget = orbitTarget.position - transform.position;
        Vector3 facingDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        Quaternion facingRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
        Quaternion lookingRotation = Quaternion.LookRotation(toTarget, Vector3.up);
        transform.position = orbitTarget.position - Quaternion.Euler(0f, 5f * Time.unscaledDeltaTime, 0f) * toTarget;
        transform.rotation = Quaternion.Slerp(transform.rotation, facingRotation, Time.unscaledDeltaTime * 1f);
        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, lookingRotation, Time.unscaledDeltaTime * 1f);
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
