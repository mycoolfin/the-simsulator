using UnityEngine;

[System.Serializable]
public class LimbConnection
{
    [SerializeField] private int childNodeId;
    [SerializeField] private int parentFace;
    [SerializeField] private Vector2 position;
    [SerializeField] private Vector3 orientation;
    [SerializeField] private Vector3 scale;
    [SerializeField] private bool reflectionX;
    [SerializeField] private bool reflectionY;
    [SerializeField] private bool reflectionZ;
    [SerializeField] private bool terminalOnly;

    public int ChildNodeId => childNodeId;
    public int ParentFace => parentFace;
    public Vector2 Position => position;
    public Vector3 Orientation => orientation;
    public Vector3 Scale => scale;
    public bool ReflectionX => reflectionX;
    public bool ReflectionY => reflectionY;
    public bool ReflectionZ => reflectionZ;
    public bool TerminalOnly => terminalOnly;

    public LimbConnection(int childNodeId, int parentFace, Vector2 position, Vector3 orientation, Vector3 scale,
                           bool reflectionX, bool reflectionY, bool reflectionZ, bool terminalOnly)
    {
        this.childNodeId = childNodeId;
        this.parentFace = parentFace;
        this.position = position;
        this.orientation = orientation;
        this.scale = scale;
        this.reflectionX = reflectionX;
        this.reflectionY = reflectionY;
        this.reflectionZ = reflectionZ;
        this.terminalOnly = terminalOnly;
    }

    public void Validate()
    {
        bool validChildNodeId = childNodeId >= 0;
        if (!validChildNodeId)
            throw new System.ArgumentException("Child node ID must be greater than 0. Specified: " + childNodeId);

        bool validParentFace = parentFace >= 0 && parentFace <= 5;
        if (!validParentFace)
            throw new System.ArgumentException("Parent face must be between 0 and 5. Specified: " + parentFace);

        bool validPosition = position.x >= -1f && position.x <= 1f && position.y >= -1f && position.y <= 1f;
        if (!validPosition)
            throw new System.ArgumentException("Position out of bounds. Specified: " + position);

        bool validOrientation = orientation.x >= ParameterManager.Instance.LimbConnection.MinAngle && orientation.x <= ParameterManager.Instance.LimbConnection.MaxAngle
                     && orientation.y >= ParameterManager.Instance.LimbConnection.MinAngle && orientation.y <= ParameterManager.Instance.LimbConnection.MaxAngle
                     && orientation.z >= ParameterManager.Instance.LimbConnection.MinAngle && orientation.z <= ParameterManager.Instance.LimbConnection.MaxAngle;
        if (!validOrientation)
            throw new System.ArgumentException("Orientation out of bounds. Specified: " + orientation);

        bool validScale = scale.x >= ParameterManager.Instance.LimbConnection.MinAngle && scale.x <= ParameterManager.Instance.LimbConnection.MaxAngle
             && scale.y >= ParameterManager.Instance.LimbConnection.MinAngle && scale.y <= ParameterManager.Instance.LimbConnection.MaxAngle
             && scale.z >= ParameterManager.Instance.LimbConnection.MinAngle && scale.z <= ParameterManager.Instance.LimbConnection.MaxAngle;
        if (!validScale)
            throw new System.ArgumentException("Scale out of bounds. Specified: " + scale);
    }

    public LimbConnection CreateCopy(int newChildNodeId)
    {
        return new(
            newChildNodeId,
            parentFace,
            position,
            orientation,
            scale,
            reflectionX,
            reflectionY,
            reflectionZ,
            terminalOnly
        );
    }

    public static LimbConnection CreateRandom(int childNodeId)
    {
        int parentFace = Random.Range(0, 6);
        Vector2 position = new(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        Vector3 orientation = new(
            Random.Range(ParameterManager.Instance.LimbConnection.MinAngle, ParameterManager.Instance.LimbConnection.MaxAngle),
            Random.Range(ParameterManager.Instance.LimbConnection.MinAngle, ParameterManager.Instance.LimbConnection.MaxAngle),
            Random.Range(ParameterManager.Instance.LimbConnection.MinAngle, ParameterManager.Instance.LimbConnection.MaxAngle)
        );
        Vector3 scale = new(
            Random.Range(ParameterManager.Instance.LimbConnection.MinScale, ParameterManager.Instance.LimbConnection.MaxScale),
            Random.Range(ParameterManager.Instance.LimbConnection.MinScale, ParameterManager.Instance.LimbConnection.MaxScale),
            Random.Range(ParameterManager.Instance.LimbConnection.MinScale, ParameterManager.Instance.LimbConnection.MaxScale)
        );
        bool reflectionX = Random.value > 0.5f;
        bool reflectionY = Random.value > 0.5f;
        bool reflectionZ = Random.value > 0.5f;
        bool terminalOnly = Random.value > 0.5f;

        return new(
            childNodeId,
            parentFace,
            position,
            orientation,
            scale,
            reflectionX,
            reflectionY,
            reflectionZ,
            terminalOnly
        );
    }
}
