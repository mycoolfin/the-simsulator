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
        [SerializeField] private int fabricateCount = 1;

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
                List<IAssessableCreature> bestCreatures = simulator.GetBestCreatures(fabricateCount);
                for (int i = 0; i < bestCreatures.Count; i++)
                    FabricateDockedCapsule(bestCreatures[i]);
            };
        }

        private void Update()
        {
            // Remove null docks from the queue.
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

            if (maxDistance > 0.001f)
            {
                float movementThisFrame = conveyorSpeed * Time.deltaTime;

                for (int i = 0; i < docksOnConveyor.Count; i++)
                {
                    CapsuleDock dock = docksOnConveyor[i];
                    Vector3 targetPosition = GetPositionOnConveyor(i);
                    Vector3 currentPosition = dock.transform.position;
                    float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);

                    // Calculate synchronized speed: all objects move proportionally to their distance.
                    float synchronizedSpeed = (distanceToTarget / maxDistance) * movementThisFrame;

                    dock.transform.position = Vector3.MoveTowards(currentPosition, targetPosition, synchronizedSpeed);
                }
            }

            Vector3 newestDockPosition = docksOnConveyor.Count > 0 ? docksOnConveyor[0].transform.position : Vector3.zero;
            fabricatorLeftGate.SetOpen(ShouldGateOpen(fabricatorLeftGate.transform.position, newestDockPosition));
            fabricatorRightGate.SetOpen(ShouldGateOpen(fabricatorRightGate.transform.position, newestDockPosition));

            Vector3 oldestDockPosition = docksOnConveyor.Count > 0 ? docksOnConveyor[^1].transform.position : Vector3.zero;
            incineratorLeftGate.SetOpen(ShouldGateOpen(incineratorLeftGate.transform.position, oldestDockPosition));
            incineratorRightGate.SetOpen(ShouldGateOpen(incineratorRightGate.transform.position, oldestDockPosition));
        }

        public void FabricateDockedCapsule(ICreature creature)
        {
            GameObject dockObject = Instantiate(dockPrefab, spawnPoint.position, spawnPoint.rotation);
            GameObject capsuleObject = Instantiate(capsulePrefab, spawnPoint.position, spawnPoint.rotation);

            CapsuleDock dock = dockObject.GetComponent<CapsuleDock>();
            ICreatureCapsule capsule = capsuleObject.GetComponent<ICreatureCapsule>();

            capsule.InitialiseFromCreature(creature);

            docksOnConveyor.Insert(0, dock);
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
    }
}
