using Unity.Entities;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.ThreeD
{
    using TheSimsulator.Sims.Genotype;
    using TheSimsulator.Sims.Phenotype;
    using Core.UI.ThreeD;
    using Sims.UI.TwoD;
    using ECS.Builders;
    using ECS.API;

    public class SimsCreatureEditor : CreatureEditor<SimsGenotype, SimsPhenotype>
    {        
        private SimsGenotypeEditor simsGenotypeEditor;
        private readonly SimsPhenotypeEntityManagement phenotypeManagement = new();

        protected override void Awake()
        {
            base.Awake();
            simsGenotypeEditor = new SimsGenotypeEditor(
                genotypeEditorDocument,
                (updatedGenotype) => DisplayEdits(updatedGenotype)
            );
            genotypeEditor = simsGenotypeEditor;

            // Wire up bidirectional selection between graph and visualiser
            simsGenotypeEditor.OnNodeSelectedByUser += OnNodeSelectedInGraph;

            // Wire up player controller UI click events to raycast against creature
            if (playerController != null)
            {
                playerController.OnUIMouseClicked += (mousePos) =>
                {
                    Ray ray = playerController.PlayerCamera.ScreenPointToRay(mousePos);
                    
                    // Check if ray hits the UI panel's collider first
                    bool hitUIPanel = false;
                    if (genotypeEditorCollider != null)
                    {
                        hitUIPanel = genotypeEditorCollider.Raycast(ray, out _, 1000f);
                    }
                    
                    // Only raycast to creature if we didn't hit the UI panel
                    if (!hitUIPanel)
                    {
                        HandleCreatureRaycast(ray);
                    }
                };
            }
        }

        protected override void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (simsGenotypeEditor != null)
                simsGenotypeEditor.OnNodeSelectedByUser -= OnNodeSelectedInGraph;
            
            base.OnDestroy();
        }

        private void HandleCreatureRaycast(Ray ray)
        {
            if (WorkingCopyCapsule == null || 
                WorkingCopyCapsule.ECSWorld == null || 
                !WorkingCopyCapsule.ECSWorld.IsCreated)
            {
                OnLimbClicked(0); // Clear selection.
                return;
            }

            if (phenotypeManagement.TryRaycastToLimb(
                WorkingCopyCapsule.ECSWorld, 
                ray, 
                1000f, 
                out ulong _, 
                out ulong nodeGid
            ))
            {
                // Hit a limb - trigger selection.
                OnLimbClicked(nodeGid);
            }
            else
            {
                // Didn't hit a limb - clear selection.
                OnLimbClicked(0);
            }
        }

        private void OnNodeSelectedInGraph(ulong nodeGid)
        {
            if (nodeGid == 0)
            {
                // Graph empty space clicked - clear limb selection.
                if (WorkingCopyCapsule != null && 
                    WorkingCopyCapsule.ECSWorld != null && 
                    WorkingCopyCapsule.ECSWorld.IsCreated)
                {
                    phenotypeManagement.ClearLimbSelection(WorkingCopyCapsule.ECSWorld);
                }
            }
            else
            {
                // Highlight limbs in ECS when user selects a node in the graph.
                if (WorkingCopyCapsule != null && 
                    WorkingCopyCapsule.ECSWorld != null && 
                    WorkingCopyCapsule.ECSWorld.IsCreated)
                {
                    phenotypeManagement.SelectLimbsByNodeGid(WorkingCopyCapsule.ECSWorld, nodeGid);
                }
            }
        }

        private void OnLimbClicked(ulong nodeGid)
        {
            if (nodeGid == 0)
            {
                simsGenotypeEditor.ClearSelection();
                return;
            }

            simsGenotypeEditor.SelectNodeByGid(nodeGid);
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
