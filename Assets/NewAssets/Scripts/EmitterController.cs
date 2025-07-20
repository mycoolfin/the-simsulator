using System.Collections.Generic;
using UnityEngine;

public class EmitterController : MonoBehaviour
{
    public float EmissiveIntensity { get; private set; } = 1f;
    private List<Renderer> renderers;
    private Color emissiveColor;

    private void Awake()
    {
        renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
    }

    public void SetEmissiveColor(Color color)
    {
        emissiveColor = color;
        UpdateMaterials();
    }

    public void SetEmissiveIntensity(float intensity)
    {
        EmissiveIntensity = intensity;
        UpdateMaterials();
    }

    private void UpdateMaterials()
    {
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", emissiveColor * EmissiveIntensity);
            }
        }
    }
}
