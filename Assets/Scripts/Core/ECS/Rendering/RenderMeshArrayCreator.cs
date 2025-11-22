using Unity.Rendering;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Rendering
{
    using System.Collections.Generic;
    using Builders;

    public class RenderMeshArrayCreator : MonoBehaviour
    {
        [SerializeField] private Mesh lightSourceMesh;
        [SerializeField] private Material lightSourceMaterial;
        public int LightSourceIndex { get; private set; }

        private readonly List<Mesh> meshes = new();
        private readonly List<Material> materials = new();
        public RenderMeshArray RenderMeshArray { get; private set; }

        protected virtual void Awake()
        {
            LightSourceIndex = RegisterMeshAndMaterial(lightSourceMesh, lightSourceMaterial);
        }

        protected virtual void Start()
        {
            RenderMeshArray = new(materials.ToArray(), meshes.ToArray());

            LightSourceEntityBuilder.RenderMeshArrayCreator = this;
        }

        protected int RegisterMeshAndMaterial(Mesh mesh, Material material)
        {
            meshes.Add(mesh);
            materials.Add(material);
            return meshes.Count - 1;
        }
    }
}
