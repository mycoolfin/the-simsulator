using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Rendering
{
    using Core.ECS.Rendering;
    using Builders;

    public class SimsRenderMeshArrayCreator : RenderMeshArrayCreator
    {
        [SerializeField] private Mesh limbMesh;
        [SerializeField] private Material limbMaterial;
        public int LimbIndex { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            LimbIndex = RegisterMeshAndMaterial(limbMesh, limbMaterial);
        }

        protected override void Start()
        {
            base.Start();

            LimbEntityBuilder.RenderMeshArrayCreator = this;
        }
    }
}
