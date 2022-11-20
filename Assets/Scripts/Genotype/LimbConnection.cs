using UnityEngine;

[System.Serializable]
public struct LimbConnection
{
    public readonly int childNodeId;
    public readonly int parentFace;
    public readonly Vector2 position;
    public readonly Vector3 orientation;
    public readonly Vector3 scale;
    public readonly bool reflection;
    public readonly bool terminalOnly;

    public LimbConnection(int childNodeId, int parentFace, Vector2 position, Vector3 orientation, Vector3 scale, bool reflection, bool terminalOnly)
    {
        this.childNodeId = childNodeId;
        this.parentFace = Mathf.Clamp(parentFace, 0, 5);;
        this.position = new Vector2(Mathf.Clamp(position.x, -1f, 1f), Mathf.Clamp(position.y, -1f, 1f));
        this.orientation =  new Vector3(
            Mathf.Clamp(orientation.x, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle),
            Mathf.Clamp(orientation.y, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle),
            Mathf.Clamp(orientation.z, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle)
        );
        this.scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        this.reflection = reflection;
        this.terminalOnly = terminalOnly;
    }

    public LimbConnection CreateCopy(int newChildNodeId)
    {
        return new(
            newChildNodeId,
            parentFace,
            position,
            orientation,
            scale,
            reflection,
            terminalOnly
        );
    }

    public static LimbConnection CreateRandom(int childNodeId)
    {
        int parentFace = Random.Range(0, 5);
        Vector2 position = new (Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        Vector3 orientation = new (
            Random.Range(LimbConnectionGenerationParameters.MinAngle, LimbConnectionGenerationParameters.MaxAngle),
            Random.Range(LimbConnectionGenerationParameters.MinAngle, LimbConnectionGenerationParameters.MaxAngle),
            Random.Range(LimbConnectionGenerationParameters.MinAngle, LimbConnectionGenerationParameters.MaxAngle)
        );
        Vector3 scale = new (
            Random.Range(LimbConnectionGenerationParameters.MinScale, LimbConnectionGenerationParameters.MaxScale),
            Random.Range(LimbConnectionGenerationParameters.MinScale, LimbConnectionGenerationParameters.MaxScale),
            Random.Range(LimbConnectionGenerationParameters.MinScale, LimbConnectionGenerationParameters.MaxScale)
        );
        bool reflection = Random.value > 0.5f;
        bool terminalOnly = Random.value > 0.5f;

        return new LimbConnection(childNodeId, parentFace, position, orientation, scale, reflection, terminalOnly);
    }
}
