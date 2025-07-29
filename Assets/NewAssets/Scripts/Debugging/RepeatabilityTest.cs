using System;
using System.Numerics;
using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;
using Unity.Entities;
using mycoolfin.TheSimsulator.UnityIntegration.ECS.API;
using mycoolfin.TheSimsulator.Core.Genotype;
using System.Collections;

public class RepeatabilityTest : UnityEngine.MonoBehaviour
{
    public int face = 0;
    public bool saveToFile = false;
    public bool loadFromFile = false;
    public string filePath = "test.genotype";
    [UnityEngine.SerializeField] public Node node;
    [UnityEngine.SerializeField] public Connection connection;

    private SimsGenotype genotype;
    SimsGenotypeFactory genotypeFactory = new(0.4f, 0.3f, 0.3f, 2);
    SimsPhenotypeFactory phenotypeFactory = new();

    private void Update()
    {
        if (saveToFile)
        {
            GenotypeIO.SerializeAsync(genotype, filePath, () =>
            {
                UnityEngine.Debug.Log("Genotype saved to " + filePath);
            });
            saveToFile = false;
        }

        if (loadFromFile)
        {
            GenotypeIO.DeserializeAsync<SimsGenotype>(filePath, (deserializedGenotype) =>
            {
                genotype = deserializedGenotype;
                UnityEngine.Debug.Log("Genotype loaded from " + filePath);
                UnityEngine.Debug.Log($"Nodes: {genotype.Nodes.Count}, Connections: {genotype.Connections.Count}, Neurons: {genotype.NeuronDefinitions.Count}");
                node = genotype.Nodes[0];
                connection = genotype.Connections[0];
                StartCoroutine(CreatePhenotype(genotype, phenotypeFactory));
            });
            loadFromFile = false;
        }
    }

    private void Start()
    {
        CreateRandomGenotype(genotypeFactory, face);

        StartCoroutine(CreatePhenotype(genotype, phenotypeFactory));
    }

    public void CreateRandomGenotype(SimsGenotypeFactory genotypeFactory, int i)
    {
        SignalEmitterAddress biasAddy = new(RelativeSignalPort.Bias, 0);
        SignalEmitterAddress addyA = new(RelativeSignalPort.Brain, 0);
        SignalEmitterAddress addyB = new(); // new(RelativeSignalPort.ThisLimb, 1);
        SignalEmitterAddress addyC = new(); // new(RelativeSignalPort.ThisLimb, 2);

        List<Node> nodes = new()
            {
                new Node(
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new(mycoolfin.TheSimsulator.Sims.Genotype.JointType.Revolute, new Vector3((float)Math.PI / 4f, (float)Math.PI / 4f, (float)Math.PI / 4f),
                        new InputSetDefinition(new(addyA, 1f), new(addyB, 0f), new(addyC, 0f)),
                        new InputSetDefinition(new(addyA, 1f), new(addyB, 0f), new(addyC, 0f)),
                        new InputSetDefinition(new(addyA, 1f), new(addyB, 0f), new(addyC, 0f))
                    ),
                    1,
                    new NodeColor(0f, 0f, 0f, 0f, 0f, 0f)
                )
            };
        List<Connection> connections = new()
            {
                new Connection(
                    nodes[0].Gid,
                    nodes[0].Gid,
                    i % 6,
                    new Vector2(0f, 0f),
                    // new Vector2(0.8f, 0.8f),
                    new Vector3(0, 0, 0),
                    // new Vector3((float)Math.PI / 15f, (float)Math.PI / 15f, (float)Math.PI / 15f),
                    new Vector3(0.5f, 0.5f, 1f),
                    false,
                    false,
                    false,
                    false
                )
            };
        mycoolfin.TheSimsulator.Sims.Genotype.NeuronDefinition nd = new(
            mycoolfin.TheSimsulator.Sims.Genotype.SimsGenotype.BRAIN_GID,
            ActivationFunction.Sin,
            new(new(biasAddy, 1f), new(biasAddy, 1f), new(biasAddy, 1f))
        );
        genotype = new(nodes, connections, new() { nd });
        // SimsGenotype genotype = genotypeFactory.CreateInitialisedGenotype();
    }

    public IEnumerator CreatePhenotype(SimsGenotype genotype, SimsPhenotypeFactory phenotypeFactory)
    {
        yield return EntityManagement.DestroyAllPhenotypeEntities(World.DefaultGameObjectInjectionWorld);

        SimsPhenotype phenotype = phenotypeFactory.ConstructPhenotype(genotype);
        List<SimsPhenotype> phenotypes = new() { phenotype };

        List<PhenotypeEntityCreationInfo> phenotypeCreationInfoList = new();
        float spacing = 5f;
        int gridSize = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Sqrt(phenotypes.Count));
        for (int i = 0; i < phenotypes.Count; i++)
        {
            SimsPhenotype p = phenotypes[i];

            int row = i / gridSize;
            int col = i % gridSize;

            float x = col * spacing;
            float z = row * spacing;

            PhenotypeEntityCreationInfo info = new()
            {
                Phenotype = p,
                VisualOffset = new(x, 0, z),
                AllowInterPhenotypeCollisions = false
            };
            phenotypeCreationInfoList.Add(info);
        }

        yield return EntityManagement.CreateEntitiesFromPhenotypes(World.DefaultGameObjectInjectionWorld, phenotypeCreationInfoList);
    }
}
