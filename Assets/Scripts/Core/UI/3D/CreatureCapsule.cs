using System.Collections;
using System.IO;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using TheSimsulator.Core.Genotype;
    using TheSimsulator.Core.Phenotype;
    using Core.ECS.API;
    using Core.ECS.Rendering;
    using Core.UI.IO;

    public interface ICreatureCapsule
    {
        void InitialiseFromCreature(ICreature creature);
        void InitialiseFromGenotypeFilePath(string filePath);
    }

    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody), typeof(AudioSource))]
    public abstract class CreatureCapsule<TGenotype, TPhenotype, TPhenotypeFactory, TECSAPI> : MonoBehaviour, ICreatureCapsule
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
        where TPhenotypeFactory : IPhenotypeFactory<TGenotype, TPhenotype>
        where TECSAPI : IECSAPI<TPhenotype>
    {
        private const string CapsuleWorld = "CapsuleWorld";

        [SerializeField] private PhenotypeCompanionObject companionObject;
        [SerializeField] private TextMeshPro fileMissingError;
        [SerializeField] private TextMeshPro invalidGenotypeError;

        protected abstract TPhenotypeFactory PhenotypeFactory { get; }
        protected abstract TECSAPI ECSAPI { get; }

        private enum CapsuleState
        {
            Empty,
            Loading,
            Loaded,
            FileMissingError,
            InvalidGenotypeError
        }
        private CapsuleState capsuleState = CapsuleState.Empty;

        private string genotypeFilePath;
        private TPhenotype phenotype;

        public void InitialiseFromCreature(ICreature creature)
        {
            // Save creature genotype to application data storage.
            string filePath = Path.Combine(Application.persistentDataPath, $"{creature.Name}.genotype");
            creature.SaveGenotypeToFile((result) =>
            {
                if (result == FileOperationResult.Success)
                {
                    genotypeFilePath = filePath;
                    InitialiseFromGenotypeFilePath(genotypeFilePath);
                }
                else
                {
                    SetState(CapsuleState.FileMissingError);
                }
            }, filePath);
        }

        public void InitialiseFromGenotypeFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                SetState(CapsuleState.FileMissingError);
                return;
            }

            SetState(CapsuleState.Loading);
            genotypeFilePath = filePath;

            GenotypeDiskOperations.LoadGenotypeFromFilePath<TGenotype>(filePath, (result, genotype) =>
            {
                if (result == FileOperationResult.Success && genotype != null)
                {
                    StartCoroutine(InitialiseFromGenotype(genotype));
                }
                else
                {
                    SetState(CapsuleState.FileMissingError);
                }
            });
        }

        private IEnumerator InitialiseFromGenotype(TGenotype genotype)
        {
            World world = ECSAPI.World.GetOrCreateWorld(CapsuleWorld);
            ECSAPI.Simulation.SetGravity(world, float3.zero);

            if (phenotype != null)
                DestroyPhenotype(world);

            try
            {
                phenotype = PhenotypeFactory.ConstructPhenotype(genotype);
            }
            catch
            {
                SetState(CapsuleState.InvalidGenotypeError);
                yield break;
            }
            PhenotypeEntityCreationInfo<TPhenotype> creationInfo = new()
            {
                Phenotype = phenotype,
                AllowInterPhenotypeCollisions = false
            };
            yield return ECSAPI.Phenotype.CreateEntitiesFromPhenotypes(world, new() { creationInfo });

            ECSAPI.Phenotype.SetPhenotypeCompanionObject(world, phenotype, companionObject);

            // TODO: Add tag to reset position if goes out of bounds.

            SetState(CapsuleState.Loaded);
        }

        private void DestroyPhenotype(World world)
        {
            ECSAPI.Phenotype.MarkPhenotypeEntitiesForDestruction(world, phenotype);
        }

        private void SetState(CapsuleState capsuleState)
        {
            this.capsuleState = capsuleState;

            fileMissingError.enabled = capsuleState == CapsuleState.FileMissingError;
            invalidGenotypeError.enabled = capsuleState == CapsuleState.InvalidGenotypeError;
        }

        private void OnDestroy()
        {
            World world = ECSAPI.World.GetWorld(CapsuleWorld);
            if (world != null && world.IsCreated)
                DestroyPhenotype(world);

            // TODO: Delete saved file from temp storage?
            // Or delegate to the system that initialises from config?
            // I.e. load in all specified files, move unreferenced ones to trash/temp.
        }
    }
}
