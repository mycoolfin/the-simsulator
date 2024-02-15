using UnityEngine;

public class MainMenuTitle : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Color emissionColor;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.EnableKeyword("_EMISSION");
        emissionColor = meshRenderer.material.GetColor("_EmissionColor");
    }

    public void ToggleLit(bool lit)
    {
        meshRenderer.material.SetColor("_EmissionColor", emissionColor * (lit ? 1f : 0f));
    }
}
