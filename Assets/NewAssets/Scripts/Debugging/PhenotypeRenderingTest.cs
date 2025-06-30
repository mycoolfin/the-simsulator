using System.Collections.Generic;
using UnityEngine;
using mycoolfin.TheSimsulator.Sims;

public class PhenotypeRenderingTest : MonoBehaviour
{
    public GameObject PhenotypeRendererPrefab;
    public GameObject LimbRendererPrefab;
    public GameObject JointRendererPrefab;

    private void Start()
    {
        if (PhenotypeRendererPrefab == null)
        {
            Debug.LogError("PhenotypeRendererPrefab is not assigned.");
            return;
        }

        SimsGenotypeFactory genotypeFactory = new(0.4f, 0.3f, 0.3f, 2);
        SimsPhenotypeFactory phenotypeFactory = new();

        List<SimsPhenotype> phenotypes = new();
        for (int i = 0; i < 1; i++)
        {
            List<Node> nodes = new()
            {
                new Node(
                    new mycoolfin.TheSimsulator.Vector3(1f, 1f, 1f),
                    new(mycoolfin.TheSimsulator.Sims.JointType.Revolute, new mycoolfin.TheSimsulator.Vector3(30f, 30f, 30f),
                        new InputSetDefinition(new SignalInputDefinition(), new SignalInputDefinition(), new SignalInputDefinition()),
                        new InputSetDefinition(new SignalInputDefinition(), new SignalInputDefinition(), new SignalInputDefinition()),
                        new InputSetDefinition(new SignalInputDefinition(), new SignalInputDefinition(), new SignalInputDefinition())
                    ),
                    2
                )
            };
            List<Connection> connections = new()
            {
                new Connection(
                    nodes[0].Gid,
                    nodes[0].Gid,
                    5,
                    new mycoolfin.TheSimsulator.Vector2(0.8f, 0.8f),
                    new mycoolfin.TheSimsulator.Vector3(0, 0, 0),
                    // new mycoolfin.TheSimsulator.Vector3(30f, 30f, 30f),
                    new mycoolfin.TheSimsulator.Vector3(0.5f, 0.5f, 1f),
                    true,
                    true,
                    true,
                    false
                )
            };
            SimsGenotype genotype = new(nodes, connections, new());
            // SimsGenotype genotype = genotypeFactory.CreateInitialisedGenotype();

            SimsPhenotype phenotype = phenotypeFactory.ConstructPhenotype(genotype);

            for (int j = 0; j < phenotype.Limbs.Count; j++)
            {
                mycoolfin.TheSimsulator.Sims.Limb limb = phenotype.Limbs[j];
                limb.SetPositionAndRotation(limb.Position, limb.Rotation);
                limb.Color = new mycoolfin.TheSimsulator.Vector4(limb.debugMirroredX ? 1f : 0f,
                                                   limb.debugMirroredY ? 1f : 0f,
                                                   limb.debugMirroredZ ? 1f : 0f,
                                                   1f);
            }

            phenotypes.Add(phenotype);
        }

        for (int i = 0; i < phenotypes.Count; i++)
        {
            SimsPhenotype phenotype = phenotypes[i];
            PhenotypeRenderer phenotypeRenderer = Instantiate(PhenotypeRendererPrefab).GetComponent<PhenotypeRenderer>();
            phenotypeRenderer.Initialise(phenotype, LimbRendererPrefab, JointRendererPrefab);
            phenotypeRenderer.transform.position = new Vector3(0, 0, 5);
        }

        List<PhenotypeEntityCreationInfo> phenotypeCreationInfoList = new();
        float spacing = 2f;
        float spiralStep = 0.5f;
        float radiusStep = spacing * 0.5f;
        for (int i = 0; i < phenotypes.Count; i++)
        {
            SimsPhenotype phenotype = phenotypes[i];

            float angle = i * spiralStep;
            float radius = radiusStep * angle;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            PhenotypeEntityCreationInfo info = new()
            {
                Phenotype = phenotype,
                PhysicsPositionOffset = new(x, 0, z),
                PhysicsWorldIndex = 0
            };
            phenotypeCreationInfoList.Add(info);
        }

        EntityCreationAPI.CreateEntitiesFromPhenotypes(phenotypeCreationInfoList);
    }
}
