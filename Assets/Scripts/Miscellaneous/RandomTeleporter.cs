using UnityEngine;

public class RandomTeleporter : MonoBehaviour
{
    public Vector3 radii;
    public float waitTime;

    private Vector3 originPosition;
    public float elapsedTime;

    private void OnEnable()
    {
        originPosition = transform.position;
        elapsedTime = 0f;
        RandomTeleport();
    }

    private void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        if (elapsedTime >= waitTime)
        {
            RandomTeleport();
            elapsedTime = 0f;
        }
    }

    private void RandomTeleport()
    {
        transform.position = originPosition + Vector3.Scale(Random.insideUnitSphere, radii);
    }
}
