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
    using ECS.API;
    using ECS.Rendering;
    using ECS.Systems.Simulation.SimulationRate;
    using UI.IO;

    public enum CapsuleState
    {
        Empty,
        Loading,
        Loaded,
        FileMissingError,
        InvalidGenotypeError
    }

    public enum CapsuleEnvironment
    {
        Terrestrial,
        Aquatic
    }

    public interface ICreatureCapsule
    {
        void InitialiseFromCreature(ICreature creature, CapsuleEnvironment environment);
        void InitialiseFromGenotypeFilePath(string filePath, CapsuleEnvironment environment);
    }

    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody), typeof(AudioSource))]
    public abstract class CreatureCapsule<TGenotype, TPhenotype, TPhenotypeFactory, TECSAPI> : MonoBehaviour, ICreatureCapsule
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
        where TPhenotypeFactory : IPhenotypeFactory<TGenotype, TPhenotype>
        where TECSAPI : IECSAPI<TPhenotype>
    {
        private const string TerrestrialCapsuleWorld = "TerrestrialCapsuleWorld";
        private const string AquaticCapsuleWorld = "AquaticCapsuleWorld";

        [SerializeField] private PhenotypeCompanionObject companionObject;
        [SerializeField] private TextMeshPro fileMissingError;
        [SerializeField] private TextMeshPro invalidGenotypeError;
        [SerializeField] private PushButton environmentToggleButton;
        [SerializeField] private GameObject groundEnvironment;
        [SerializeField] private GameObject aquaticEnvironment;

        protected abstract TPhenotypeFactory PhenotypeFactory { get; }
        protected abstract TECSAPI ECSAPI { get; }

        public CapsuleState State { get; private set; } = CapsuleState.Empty;

        private CapsuleEnvironment environment = CapsuleEnvironment.Aquatic;
        public CapsuleEnvironment Environment
        {
            get => environment;
            private set
            {
                environment = value;

                // Toggle display gameobjects.
                environmentToggleButton.SetActive(value == CapsuleEnvironment.Aquatic);
                groundEnvironment.SetActive(value == CapsuleEnvironment.Terrestrial);
                aquaticEnvironment.SetActive(value == CapsuleEnvironment.Aquatic);
            }
        }

        private string genotypeFilePath;
        private TGenotype genotype;
        private TPhenotype phenotype;

        private void Start()
        {
            environmentToggleButton.OnButtonPressed += (_) =>
            {
                ToggleEnvironment();
                environmentToggleButton.SetActive(environment == CapsuleEnvironment.Aquatic);
            };
        }

        public void InitialiseFromCreature(ICreature creature, CapsuleEnvironment environment)
        {
            // Save creature genotype to application data storage.
            string filePath = Path.Combine(Application.persistentDataPath, $"{creature.Name}.genotype");
            creature.SaveGenotypeToFile((result) =>
            {
                if (result == FileOperationResult.Success)
                {
                    genotypeFilePath = filePath;
                    InitialiseFromGenotypeFilePath(genotypeFilePath, environment);
                }
                else
                {
                    SetState(CapsuleState.FileMissingError);
                }
            }, filePath);
        }

        public void InitialiseFromGenotypeFilePath(string filePath, CapsuleEnvironment environment)
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
                    StartCoroutine(InitialiseFromGenotype(genotype, environment));
                }
                else
                {
                    SetState(CapsuleState.FileMissingError);
                }
            });
        }

        private IEnumerator InitialiseFromGenotype(TGenotype genotype, CapsuleEnvironment environment)
        {
            this.genotype = genotype;
            Environment = environment;

            World world = GetWorld(Environment);

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

        private void ToggleEnvironment()
        {
            // Destroy current phenotype entities.
            DestroyPhenotype(GetWorld(Environment));

            CapsuleEnvironment newEnvironment = Environment == CapsuleEnvironment.Terrestrial ? CapsuleEnvironment.Aquatic : CapsuleEnvironment.Terrestrial;

            // Recreate entities in selected environment world.
            StartCoroutine(InitialiseFromGenotype(genotype, newEnvironment));
        }

        private void DestroyPhenotype(World world)
        {
            ECSAPI.Phenotype.MarkPhenotypeEntitiesForDestruction(world, phenotype);
        }

        private void SetState(CapsuleState state)
        {
            State = state;

            fileMissingError.enabled = state == CapsuleState.FileMissingError;
            invalidGenotypeError.enabled = state == CapsuleState.InvalidGenotypeError;
        }

        private World GetWorld(CapsuleEnvironment environment)
        {
            World world = ECSAPI.World.GetOrCreateWorld(GetWorldName(environment), (world) =>
            {
                ECSAPI.Simulation.SetSimulationRateControllerMode(world, SimulationRateMode.RealTime);

                if (environment == CapsuleEnvironment.Terrestrial)
                {
                    ECSAPI.Simulation.SetGravity(world, float3.zero);
                    ECSAPI.Simulation.SetFluidSimulation(world, false);
                    ECSAPI.Simulation.CreateGroundPlane(world);
                }

                if (environment == CapsuleEnvironment.Aquatic)
                {
                    ECSAPI.Simulation.SetGravity(world, Physics.gravity);
                    ECSAPI.Simulation.SetFluidSimulation(world, true, 1f);
                }
            });
            return world;
        }

        private string GetWorldName(CapsuleEnvironment environment)
        {
            return environment == CapsuleEnvironment.Terrestrial ? TerrestrialCapsuleWorld : AquaticCapsuleWorld;
        }

        private void OnDestroy()
        {
            World world = ECSAPI.World.GetWorld(GetWorldName(environment));
            if (world != null && world.IsCreated)
                DestroyPhenotype(world);

            // TODO: Delete saved file from temp storage?
            // Or delegate to the system that initialises from config?
            // I.e. load in all specified files, move unreferenced ones to trash/temp.
        }
    }
}
