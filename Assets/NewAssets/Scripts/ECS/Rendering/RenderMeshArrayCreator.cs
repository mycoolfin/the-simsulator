using Unity.Rendering;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Rendering
{
    using Builders;

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
}
