using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class EmitterController : MonoBehaviour
{
    public float EmissiveIntensity { get; private set; } = 1f;
    private Material material;
    private Color emissiveColor;

    private void Awake()
    {
        material = GetComponent<Renderer>().material;
    }

    public void SetEmissiveColor(Color color)
    {
        emissiveColor = color;
        UpdateMaterial();
    }

    public void SetEmissiveIntensity(float intensity)
    {
        EmissiveIntensity = intensity;
        UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissiveColor * EmissiveIntensity);
    }
}
