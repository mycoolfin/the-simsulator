using UnityEngine;

public interface ISelectable
{
    GameObject gameObject { get; }
    void Select(bool toggle, bool multiselect);
    Bounds GetBounds();
}
