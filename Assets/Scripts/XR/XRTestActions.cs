using UnityEngine;

public class XRTestActions : MonoBehaviour
{
    public Spawner spawner;
    public float shrinkFactor = 0.1f;
    public float shrinkSpeed = 1f;

    public void Shrink()
    {
        LerpToSize(Vector3.one * shrinkFactor);
    }

    public void ReturnToNormalSize()
    {
        LerpToSize(Vector3.one);
    }

    private void LerpToSize(Vector3 desiredSize)
    {
        transform.localScale = Vector3.Lerp(transform.localScale, desiredSize, Time.deltaTime * shrinkSpeed);
    }

    public void SwitchToSurfaceEnvironment()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Surface);
        spawner.spawnType = SpawnType.Surface;
    }

    public void SwitchToUnderwaterEnvironment()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Underwater);
        spawner.spawnType = SpawnType.Underwater;
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
