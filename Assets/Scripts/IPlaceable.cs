using UnityEngine;

public interface IPlaceable
{
    GameObject gameObject { get; }
    Bounds GetBounds();
}
