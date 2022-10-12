using UnityEngine;

public class LimbConnection
{
    public readonly LimbNode childNode;
    public readonly int parentFace;
    public readonly Vector2 position;
    public readonly Vector3 orientation;
    public readonly Vector3 scale;
    public readonly bool reflection;
    public readonly bool terminalOnly;

    public LimbConnection(LimbNode childNode, int parentFace, Vector2 position, Vector3 orientation, Vector3 scale, bool reflection, bool terminalOnly)
    {
        if (parentFace < 0 || parentFace > 5)
            throw new System.ArgumentOutOfRangeException("parentFace must be in range [0, 5]");
        if (position.x < -1 || position.x > 1 || position.y < -1 || position.y > 1)
            throw new System.ArgumentOutOfRangeException("position must be in range Vector2([-1f, 1f], [-1f, 1f])");
        
        this.childNode = childNode;
        this.parentFace = parentFace;
        this.position = position;
        this.orientation = orientation;
        this.scale = scale;
        this.reflection = reflection;
        this.terminalOnly = terminalOnly;
    }
}
