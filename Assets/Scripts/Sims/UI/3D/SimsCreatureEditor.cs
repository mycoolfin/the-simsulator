using Unity.Entities;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.ThreeD
{
    using TheSimsulator.Sims.Genotype;
    using TheSimsulator.Sims.Phenotype;
    using Core.UI.ThreeD;
    using Sims.UI.TwoD;
    using ECS.Builders;

    public class SimsCreatureEditor : CreatureEditor<SimsGenotype, SimsPhenotype>
    {
        protected override void Awake()
        {
            base.Awake();
            genotypeEditor = new SimsGenotypeEditor(
                genotypeEditorDocument,
                (updatedGenotype) => DisplayEdits(updatedGenotype)
            );
        }

        protected override void ExportCreatureModel(ICreatureCapsule dockedCapsule)
        {
            if (dockedCapsule == null) return;

            World capsuleWorld = dockedCapsule.ECSWorld;
            static Mesh getBaseMesh()
            {
                int limbIndex = LimbEntityBuilder.RenderMeshArrayCreator.LimbIndex;
                return LimbEntityBuilder.RenderMeshArrayCreator.RenderMeshArray.MeshReferences[limbIndex].Value;
            }

            dockedCapsule.Creature.SavePhenotypeModelToFile(
                capsuleWorld,
                getBaseMesh,
                0.01f,
                (result, filePath) => Debug.Log($"Export Phenotype Model Result: {result}, File Path: {filePath}")
            );
        }
    }
}
