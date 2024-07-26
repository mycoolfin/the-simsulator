using UnityEngine;
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
    }

    public void ResetScene()
    {
        SceneManager.LoadScene("XRDemo");
    }

    public void ToggleShrink()
    {
        LerpToSize(Vector3.one * (shrunk ? 1f : shrinkFactor));
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
