using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class XRDemoController : MonoBehaviour
{
    public Spawner spawner;
    public float shrinkFactor = 0.1f;
    public float shrinkSpeed = 1f;
    private bool shrunk = false;

    private void Start()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Underwater);
        RenderSettings.fog = false;
    }

    public InputActionReference trigger;
    public InputActionReference grip;

    private void OnEnable()
    {
        trigger.action.performed += OnTriggerPressed;
        grip.action.performed += OnGripPressed;

        trigger.action.Enable();
        grip.action.Enable();
    }

    private void OnDisable()
    {
        trigger.action.performed -= OnTriggerPressed;
        grip.action.performed -= OnGripPressed;

        trigger.action.Disable();
        grip.action.Disable();
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Trigger Pressed");
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Grip Pressed");
        ToggleShrink();
    }

    private void Update()
    {
        LerpToSize(Vector3.one * (shrunk ? 1f : shrinkFactor));
    }

    public void ResetScene()
    {
        SceneManager.LoadScene("XRDemo");
    }

    public void ToggleShrink()
    {
        shrunk = !shrunk;
    }

    private void LerpToSize(Vector3 desiredSize)
    {
        transform.localScale = Vector3.Lerp(transform.localScale, desiredSize, Time.deltaTime * shrinkSpeed);
    }

    public void SwitchToSurfaceEnvironment()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Surface);
    }

    public void SwitchToUnderwaterEnvironment()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Underwater);
    }

    public void FreezeTime()
    {
        WorldManager.Instance.targetTimeScale = 0f;
    }

    public void UnfreezeTime()
    {
        WorldManager.Instance.targetTimeScale = 1f;
    }

    public void SpawnRandomCreature()
    {
        spawner.SpawnRandomCreature();
    }
}
