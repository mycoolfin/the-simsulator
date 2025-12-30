using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Core.Evolution;
    using IO;

    public class CapsuleFabricator : SaveableBehaviour
    {
        [SerializeField] private GameObject simulatorContainer;
        [SerializeField] private GameObject dockPrefab;
        [SerializeField] private AudioClip conveyorSound;
        [SerializeField] private AudioSource conveyorAudioSource;
        [SerializeField] private ButtonGroup generationStepGroup;
        [SerializeField] private HingedPanel fabricatorLeftGate;
        [SerializeField] private HingedPanel fabricatorRightGate;
        [SerializeField] private HingedPanel incineratorLeftGate;
        [SerializeField] private HingedPanel incineratorRightGate;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform conveyorStart;
        [SerializeField] private Transform conveyorEnd;
        [SerializeField] private Transform destroyPoint;
        [SerializeField] private int conveyorCapacity = 5;
        [SerializeField] private float conveyorSpeed = 1.0f;

        private readonly Queue<Func<CapsuleDock>> fabricationQueue = new();
        private readonly List<CapsuleDock> docksOnConveyor = new();
        private IEvolutionSimulator simulator;

        public override string SaveId => "CapsuleFabricator";
        public override int SaveVersion => 1;
        [Serializable]
        private class CapsuleFabricatorData
        {
            public int GenerationStepIndex;
        }
        public override object CaptureState()
        {
            return new CapsuleFabricatorData
            {
                GenerationStepIndex = generationStepGroup.ActiveButtonIndex
            };
        }
        public override void RestoreState(JObject payload, int version)
        {
            CapsuleFabricatorData data = payload.ToObject<CapsuleFabricatorData>();
            if (data == null)
            {
                ResetToDefaults();
                return;
            }

            generationStepGroup.SetActiveButton(data.GenerationStepIndex);
        }

        private void Awake()
        {
            simulator = simulatorContainer.GetComponent<IEvolutionSimulator>();
            if (simulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on simulatorContainer.");
                return;
            }

            simulator.OnGenerationComplete += (stats, bestIndividual) =>
            {
                if (stats.Generation % GenerationStep != 0)
                    return;

                if (bestIndividual != null)
                {
                    bool isAquatic = simulator.TrialType == TrialType.WaterDistance || simulator.TrialType == TrialType.WaterLightFollowing;
                    CapsuleEnvironment environment = isAquatic ? CapsuleEnvironment.Aquatic : CapsuleEnvironment.Terrestrial;
                    FabricateDockedCapsule(bestIndividual, environment);
                }
            };

            conveyorAudioSource.clip = conveyorSound;
            conveyorAudioSource.loop = true;

            ResetToDefaults();
        }

        private void Start()
        {
            InitialiseButtons();
        }

        private void Update()
        {
            // Remove null docks from the conveyor.
            docksOnConveyor.RemoveAll(dock => dock == null);

            // Calculate the maximum distance any dock needs to travel.
            float maxDistance = 0f;
            for (int i = 0; i < docksOnConveyor.Count; i++)
            {
                CapsuleDock dock = docksOnConveyor[i];
                Vector3 targetPosition = GetPositionOnConveyor(i);
                float distance = Vector3.Distance(dock.transform.position, targetPosition);
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            bool allDocksAtTarget = true;
            if (maxDistance > 0.001f)
            {
                float movementThisFrame = conveyorSpeed * (fabricationQueue.Count + 1f) * Time.deltaTime;

                for (int i = 0; i < docksOnConveyor.Count; i++)
                {
                    CapsuleDock dock = docksOnConveyor[i];
                    Vector3 targetPosition = GetPositionOnConveyor(i);
                    Vector3 currentPosition = dock.transform.position;
                    float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);

                    // Calculate synchronized speed: all objects move proportionally to their distance.
                    float synchronizedSpeed = (distanceToTarget / maxDistance) * movementThisFrame;

                    dock.transform.position = Vector3.MoveTowards(currentPosition, targetPosition, synchronizedSpeed);

                    if (distanceToTarget > 0.001f)
                        allDocksAtTarget = false;
                }
            }

            if (allDocksAtTarget)
                TryAddNextDockToConveyor();

            if (!allDocksAtTarget && !conveyorAudioSource.isPlaying)
                conveyorAudioSource.Play();
            else if (allDocksAtTarget && conveyorAudioSource.isPlaying)
                conveyorAudioSource.Pause();

            Vector3 newestDockPosition = docksOnConveyor.Count > 0 ? docksOnConveyor[0].transform.position : Vector3.zero;
            fabricatorLeftGate.SetOpen(ShouldGateOpen(fabricatorLeftGate.transform.position, newestDockPosition));
            fabricatorRightGate.SetOpen(ShouldGateOpen(fabricatorRightGate.transform.position, newestDockPosition));

            Vector3 oldestDockPosition = docksOnConveyor.Count > 0 ? docksOnConveyor[^1].transform.position : Vector3.zero;
            incineratorLeftGate.SetOpen(ShouldGateOpen(incineratorLeftGate.transform.position, oldestDockPosition));
            incineratorRightGate.SetOpen(ShouldGateOpen(incineratorRightGate.transform.position, oldestDockPosition));
        }

        private void InitialiseButtons()
        {
            generationStepGroup.OnButtonPressed += (index) => NotifyChanged();
        }

        private void ResetToDefaults()
        {
            SetGenerationStepToDefaultValue();
        }

        private readonly int[] GenerationStepOptions = { 1, 5, 10, 100 };
        private int GenerationStep => GenerationStepOptions[generationStepGroup.ActiveButtonIndex];
        private void SetGenerationStepToDefaultValue() => generationStepGroup.SetActiveButton(0);

        private void FabricateDockedCapsule(ICreature creature, CapsuleEnvironment environment)
        {
            fabricationQueue.Enqueue(() =>
            {
                GameObject dockObject = Instantiate(dockPrefab, spawnPoint.position, spawnPoint.rotation);
                CapsuleDock dock = dockObject.GetComponent<CapsuleDock>();
                dock.SetNameplateEnabled(false);
                dock.CreateAndDockEmptyCapsule(silent: true);
                dock.DockedCapsule.InitialiseFromCreature(creature, environment);

                return dock;
            });
        }

        private void TryAddNextDockToConveyor()
        {
            if (fabricationQueue.Count > 0 && docksOnConveyor.Count <= conveyorCapacity)
            {
                CapsuleDock nextDock = fabricationQueue.Dequeue()();
                docksOnConveyor.Insert(0, nextDock);
            }
        }

        private bool ShouldGateOpen(Vector3 gatePosition, Vector3 closestDockPosition)
        {
            return Vector3.Distance(gatePosition, closestDockPosition) < 1f;
        }

        private Vector3 GetPositionOnConveyor(int waypointIndex)
        {
            if (waypointIndex >= conveyorCapacity)
                return destroyPoint.position;

            Vector3 start = conveyorStart.position;
            Vector3 end = conveyorEnd.position;

            float t = Mathf.Clamp01((float)waypointIndex / (conveyorCapacity - 1));
            return Vector3.Lerp(start, end, t);
        }
    }
}
