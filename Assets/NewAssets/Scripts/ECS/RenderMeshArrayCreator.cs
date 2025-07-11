using Unity.Rendering;
using UnityEngine;

public class RenderMeshArrayCreator : MonoBehaviour
{
    public Mesh[] Meshes;
    public Material[] Materials;

    void Start()
    {
        RenderMeshArray renderMeshArray = new(Materials, Meshes);

        LimbEntityBuilder.RenderMeshArray = renderMeshArray;
    }
}
