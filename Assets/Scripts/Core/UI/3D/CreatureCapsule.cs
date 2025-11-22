using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using TheSimsulator.Core.Genotype;
    using TheSimsulator.Core.Phenotype;
    using ECS.API;
    using ECS.Components.Phenotype;
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
        bool IsInitialised { get; }
        ICreature Creature { get; }
        event Action<ICreature> OnCreatureLoaded;
        CapsuleEnvironment Environment { get; }
        string GenotypeFilePath { get; }
        GameObject GameObject { get; }
        void InitialiseFromCreature(ICreature creature, CapsuleEnvironment environment);
        void InitialiseFromGenotypeFilePath(string filePath, CapsuleEnvironment environment);
        void InitialiseFromGenotypeFilePathDialog(CapsuleEnvironment environment, Action<FileOperationResult> OnComplete);
        void SetCompanionObjectOverride(PhenotypeCompanionObject companionObject);
        bool RenameCreature(string newName);
    }

    [RequireComponent(typeof(Rigidbody), typeof(AudioSource), typeof(XRGrabInteractable))]
    public abstract class CreatureCapsule<TGenotype, TPhenotype, TPhenotypeFactory, TECSAPI> : MonoBehaviour, ICreatureCapsule, IGenotypeFileReferenceProvider
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
        where TPhenotypeFactory : IPhenotypeFactory<TGenotype, TPhenotype>
        where TECSAPI : IECSAPI<TPhenotype>
    {
        private const string TerrestrialCapsuleWorld = "TerrestrialCapsuleWorld";
        private const string AquaticCapsuleWorld = "AquaticCapsuleWorld";

        [SerializeField] private PhenotypeCompanionObject terrestrialCompanionObject;
        [SerializeField] private PhenotypeCompanionObject aquaticCompanionObject;
        private PhenotypeCompanionObject companionObjectOverride;
        [SerializeField] private TextMeshPro fileMissingError;
        [SerializeField] private TextMeshPro invalidGenotypeError;
        [SerializeField] private PushButton environmentToggleButton;
        [SerializeField] private GameObject terrestrialDisplay;
        [SerializeField] private GameObject aquaticDisplay;
        [SerializeField] private AudioClip collideSound;

        protected abstract TPhenotypeFactory PhenotypeFactory { get; }
        protected abstract TECSAPI ECSAPI { get; }

        private CapsuleEnvironment environment = CapsuleEnvironment.Aquatic;
        public CapsuleEnvironment Environment
        {
            get => environment;
            private set
            {
                environment = value;

                // Toggle display gameobjects.
                environmentToggleButton.SetActive(value == CapsuleEnvironment.Aquatic);
                terrestrialDisplay.SetActive(value == CapsuleEnvironment.Terrestrial);
                aquaticDisplay.SetActive(value == CapsuleEnvironment.Aquatic);
            }
        }

        public string GenotypeFilePath { get; private set; } = string.Empty;
        private TGenotype genotype;
        private TPhenotype phenotype;

        private Creature<TGenotype, TPhenotype> creature;
        public ICreature Creature => creature;
        public event Action<ICreature> OnCreatureLoaded;

        public GameObject GameObject => gameObject;

        public IEnumerable<string> GetReferencedGenotypePaths() => GenotypeFilePath == null ? Array.Empty<string>() : new[] { GenotypeFilePath };

        public bool IsInitialised { get; private set; } = true;

        private AudioSource audioSource;
        private XRGrabInteractable grabInteractable;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            environmentToggleButton.OnButtonPressed += (_) =>
            {
                ToggleEnvironment();
            };
        }

        public void InitialiseFromCreature(ICreature creature, CapsuleEnvironment environment)
        {
            // Save creature genotype to application data storage.
            GenotypeFilePath = GenotypeDiskOperations.PrepareGenotypeSavePathInDefaultStorage(creature.Name);
            creature.SaveGenotypeToFile((result, filePath) =>
            {
                if (result == FileOperationResult.Success)
                {
                    InitialiseFromGenotypeFilePath(filePath, environment);
                }
                else
                {
                    SetState(CapsuleState.FileMissingError);
                }
            }, GenotypeFilePath);
        }

        public void InitialiseFromGenotypeFilePath(string filePath, CapsuleEnvironment environment)
        {
            GenotypeFilePath = filePath;

            if (string.IsNullOrEmpty(GenotypeFilePath))
            {
                SetState(CapsuleState.FileMissingError);
                return;
            }

            SetState(CapsuleState.Loading);

            GenotypeDiskOperations.LoadGenotypeFromFilePath<TGenotype>(GenotypeFilePath, (result, genotype) =>
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

        public void InitialiseFromGenotypeFilePathDialog(CapsuleEnvironment environment, Action<FileOperationResult> OnComplete)
        {
            SetState(CapsuleState.Loading);

            GenotypeDiskOperations.LoadGenotypeFromFilePathDialog<TGenotype>((result, genotype, filePath) =>
            {
                GenotypeFilePath = filePath;

                if (result == FileOperationResult.Success && genotype != null)
                {
                    StartCoroutine(InitialiseFromGenotype(genotype, environment));
                }
                else
                {
                    SetState(CapsuleState.FileMissingError);
                }

                OnComplete?.Invoke(result);
            });
        }

        public void SetCompanionObjectOverride(PhenotypeCompanionObject companionObject)
        {
            companionObjectOverride = companionObject;
            StartCoroutine(InitialiseFromGenotype(genotype, Environment));
        }

        public bool RenameCreature(string newName)
        {
            if (creature != null)
            {
                string newFilePath = GenotypeDiskOperations.RenameGenotypeFile(GenotypeFilePath, newName);
                if (newFilePath != null)
                {
                    GenotypeFilePath = newFilePath;
                    creature.Name = newName;
                    return true;
                }
            }
            return false;
        }

        private IEnumerator InitialiseFromGenotype(TGenotype genotype, CapsuleEnvironment environment)
        {
            if (!IsInitialised)
                yield break; // Still initializing from a previous call.

            this.genotype = genotype;
            Environment = environment;

            if (genotype == null) yield break;

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

            IsInitialised = false;
            PhenotypeEntityCreationInfo<TPhenotype> creationInfo = new()
            {
                Phenotype = phenotype,
                AllowInterPhenotypeCollisions = false
            };
            yield return ECSAPI.Phenotype.CreateEntitiesFromPhenotypes(world, new() { creationInfo });

            PhenotypeCompanionObject companionObject = companionObjectOverride != null ? companionObjectOverride
                : Environment == CapsuleEnvironment.Terrestrial ? terrestrialCompanionObject : aquaticCompanionObject;
            ECSAPI.Phenotype.SetPhenotypeCompanionObject(world, phenotype, companionObject);

            creature = new()
            {
                Genotype = genotype,
                Phenotype = phenotype
            };

            IsInitialised = true;
            OnCreatureLoaded?.Invoke(creature);

            SetState(CapsuleState.Loaded);
        }

        private void ToggleEnvironment()
        {
            // Destroy current phenotype entities.
            DestroyPhenotype(GetWorld(Environment));

            Environment = Environment == CapsuleEnvironment.Terrestrial ? CapsuleEnvironment.Aquatic : CapsuleEnvironment.Terrestrial;

            // Recreate entities in selected environment world.
            StartCoroutine(InitialiseFromGenotype(genotype, Environment));
        }

        private void DestroyPhenotype(World world)
        {
            ECSAPI.Phenotype.MarkPhenotypeEntitiesForDestruction(world, phenotype);
        }

        private void SetState(CapsuleState state)
        {
            fileMissingError.gameObject.SetActive(state == CapsuleState.FileMissingError);
            invalidGenotypeError.gameObject.SetActive(state == CapsuleState.InvalidGenotypeError);
        }

        private World GetWorld(CapsuleEnvironment environment)
        {
            World world = ECSAPI.World.GetOrCreateWorld(GetWorldName(environment), (world) =>
            {
                ECSAPI.Simulation.SetSimulationRateControllerMode(world, SimulationRateMode.RealTime);

                if (environment == CapsuleEnvironment.Terrestrial)
                {
                    ECSAPI.Simulation.SetToTerrestrialDefaults(world);
                    ECSAPI.Object.CreateGroundPlane(world);
                }
                else if (environment == CapsuleEnvironment.Aquatic)
                {
                    ECSAPI.Simulation.SetToAquaticDefaults(world);
                    ECSAPI.Object.DestroyGroundPlane(world);
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
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collideSound != null && audioSource != null && !grabInteractable.isSelected)
            {
                float impact = collision.relativeVelocity.magnitude;
                float volume = Mathf.Clamp01(impact / 10f);
                audioSource.PlayOneShot(collideSound, volume);
            }
        }
    }
}
