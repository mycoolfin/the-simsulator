using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class EmitterController : MonoBehaviour
{
    [SerializeField] private Color emissiveColor = Color.white;
    public float EmissiveIntensity { get; private set; } = 1f;
    private Material material;

    private void Awake()
    {
        material = GetComponent<Renderer>().sharedMaterial;
    }

    public void SetEmissiveIntensity(float intensity)
    {
        EmissiveIntensity = intensity;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissiveColor * EmissiveIntensity);
    }
}
