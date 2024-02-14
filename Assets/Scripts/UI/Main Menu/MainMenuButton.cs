using System;
using UnityEngine;

public class MainMenuButton : MonoBehaviour, ISelectable
{
    public event Action OnSelect = delegate { };

    private MeshRenderer meshRenderer;
    private Color defaultColor;
    public bool hovering;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        defaultColor = meshRenderer.material.color;
    }

    private void Update()
    {
        float a = Mathf.Lerp(meshRenderer.material.color.a, hovering ? 1f : defaultColor.a, Time.deltaTime * 10f);
        meshRenderer.material.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, a);
    }

    public Bounds GetBounds()
    {
        return GetComponent<MeshCollider>().bounds;
    }

    public void Select(bool toggle, bool multiselect)
    {
        OnSelect();
    }
}
