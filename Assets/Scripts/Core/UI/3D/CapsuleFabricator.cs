using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Core.Evolution;

    public class CapsuleFabricator : MonoBehaviour
    {
        [SerializeField] private GameObject simulatorContainer;
        [SerializeField] private GameObject capsulePrefab;
        [SerializeField] private GameObject dockPrefab;
        [SerializeField] private ButtonGroup choiceTypeGroup;
        [SerializeField] private ButtonGroup fabricateCountGroup;
        [SerializeField] private HingedPanel fabricatorLeftGate;
        [SerializeField] private HingedPanel fabricatorRightGate;
        [SerializeField] private HingedPanel incineratorLeftGate;
        [SerializeField] private HingedPanel incineratorRightGate;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform conveyorStart;
        [SerializeField] private Transform conveyorEnd;
        [SerializeField] private int conveyorCapacity = 5;
        [SerializeField] private float conveyorSpeed = 1.0f;
        [SerializeField] private float generationStep = 1;
        private enum ChoiceType
        {
            Best,
            Random
        }

        private Queue<Func<CapsuleDock>> fabricationQueue = new();
        private List<CapsuleDock> docksOnConveyor = new();
        private IEvolutionSimulator simulator;


        private void Start()
        {
            simulator = simulatorContainer.GetComponent<IEvolutionSimulator>();
            if (simulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on simulatorContainer.");
                return;
            }

            simulator.OnGenerationComplete += (stats) =>
            {
                int generation = stats.Count;
                if (generation % generationStep != 0)
                    return;

                int fabCount = GetFabricateCount();
                IReadOnlyList<IAssessableCreature> creatures = GetChoiceType() == ChoiceType.Random ? GetRandomCreatures(simulator, fabCount) : GetBestCreatures(simulator, fabCount);
                for (int i = 0; i < creatures.Count; i++)
                {
                    CapsuleEnvironment environment = simulator.TrialType == TrialType.WaterDistance ? CapsuleEnvironment.Aquatic : CapsuleEnvironment.Terrestrial;
                    FabricateDockedCapsule(creatures[i], environment);
                }
            };

            ResetToDefaults();
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
                float movementThisFrame = conveyorSpeed * (fabricationQueue.Count + 1) * Time.deltaTime;

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

            Vector3 newestDockPosition = docksOnConveyor.Count > 0 ? docksOnConveyor[0].transform.position : Vector3.zero;
            fabricatorLeftGate.SetOpen(ShouldGateOpen(fabricatorLeftGate.transform.position, newestDockPosition));
            fabricatorRightGate.SetOpen(ShouldGateOpen(fabricatorRightGate.transform.position, newestDockPosition));

            Vector3 oldestDockPosition = docksOnConveyor.Count > 0 ? docksOnConveyor[^1].transform.position : Vector3.zero;
            incineratorLeftGate.SetOpen(ShouldGateOpen(incineratorLeftGate.transform.position, oldestDockPosition));
            incineratorRightGate.SetOpen(ShouldGateOpen(incineratorRightGate.transform.position, oldestDockPosition));
        }

        private void ResetToDefaults()
        {
            SetChoiceTypeToDefaultValue();
            SetFabricateCountToDefaultValue();
        }

        private readonly ChoiceType[] ChoiceTypeOptions = { ChoiceType.Best, ChoiceType.Random };
        private ChoiceType GetChoiceType() => ChoiceTypeOptions[choiceTypeGroup.ActiveButtonIndex];
        private void SetChoiceTypeToDefaultValue() => choiceTypeGroup.SetActiveButton(0);

        private readonly int[] FabricateCountOptions = { 0, 1, 2, 5 };
        private int GetFabricateCount() => FabricateCountOptions[fabricateCountGroup.ActiveButtonIndex];
        private void SetFabricateCountToDefaultValue() => fabricateCountGroup.SetActiveButton(1);


        private void FabricateDockedCapsule(ICreature creature, CapsuleEnvironment environment)
        {
            fabricationQueue.Enqueue(() =>
            {
                GameObject dockObject = Instantiate(dockPrefab, spawnPoint.position, spawnPoint.rotation);
                CapsuleDock dock = dockObject.GetComponent<CapsuleDock>();
                dock.DisableFirstEnterSound = true;

                GameObject capsuleObject = Instantiate(capsulePrefab, dock.Socket.transform.position, dock.Socket.transform.rotation);
                ICreatureCapsule capsule = capsuleObject.GetComponent<ICreatureCapsule>();
                capsule.InitialiseFromCreature(creature, environment);

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
            return Vector3.Distance(gatePosition, closestDockPosition) < 0.5f;
        }

        private Vector3 GetPositionOnConveyor(int waypointIndex)
        {
            Vector3 start = conveyorStart.position;
            Vector3 end = conveyorEnd.position;

            float t = Mathf.Clamp01((float)waypointIndex / conveyorCapacity);
            return Vector3.Lerp(start, end, t);
        }

        private IReadOnlyList<IAssessableCreature> GetBestCreatures(IEvolutionSimulator simulator, int count)
        {
            return simulator.Population?
                .OrderByDescending(i => i.Fitness)
                .Take(count)
                .Cast<IAssessableCreature>()
                .ToList() ?? new();
        }

        private IReadOnlyList<IAssessableCreature> GetRandomCreatures(IEvolutionSimulator simulator, int count)
        {
            return simulator.Population?
                .OrderBy(i => Guid.NewGuid())
                .Take(count)
                .Cast<IAssessableCreature>()
                .ToList() ?? new();
        }
    }
}
