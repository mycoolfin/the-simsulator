using System;
using System.Numerics;
using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;
using Unity.Entities;

public class PhenotypeRenderingTest : UnityEngine.MonoBehaviour
{
    public UnityEngine.GameObject PhenotypeRendererPrefab;
    public UnityEngine.GameObject LimbRendererPrefab;
    public UnityEngine.GameObject JointRendererPrefab;

    public int count;

    private void Start()
    {
        if (PhenotypeRendererPrefab == null)
        {
            UnityEngine.Debug.LogError("PhenotypeRendererPrefab is not assigned.");
            return;
        }

        SimsGenotypeFactory genotypeFactory = new(0.4f, 0.3f, 0.3f, 2);
        SimsPhenotypeFactory phenotypeFactory = new();

        List<SimsPhenotype> phenotypes = new();
        SignalEmitterAddress biasAddy = new(RelativeSignalPort.Bias, 0);
        SignalEmitterAddress addyA = new(RelativeSignalPort.Brain, 0);
        SignalEmitterAddress addyB = new(); // new(RelativeSignalPort.ThisLimb, 1);
        SignalEmitterAddress addyC = new(); // new(RelativeSignalPort.ThisLimb, 2);

        for (int i = 0; i < count; i++)
        {
            List<Node> nodes = new()
            {
                new Node(
                    new Vector3(1f, 1f, 1f),
                    new(mycoolfin.TheSimsulator.Sims.Genotype.JointType.Revolute, new Vector3((float)Math.PI / 4f, (float)Math.PI / 4f, (float)Math.PI / 4f),
                        new InputSetDefinition(new(addyA, 1f), new(addyB, 0f), new(addyC, 0f)),
                        new InputSetDefinition(new(addyA, 1f), new(addyB, 0f), new(addyC, 0f)),
                        new InputSetDefinition(new(addyA, 1f), new(addyB, 0f), new(addyC, 0f))
                    ),
                    2
                )
            };
            List<Connection> connections = new()
            {
                new Connection(
                    nodes[0].Gid,
                    nodes[0].Gid,
                    i % 6,
                    new Vector2(0.8f, 0.8f),
                    // new Vector3(0, 0, 0),
                    new Vector3((float)Math.PI / 15f, (float)Math.PI / 15f, (float)Math.PI / 15f),
                    new Vector3(0.5f, 0.5f, 1f),
                    true,
                    true,
                    true,
                    false
                )
            };
            mycoolfin.TheSimsulator.Sims.Genotype.NeuronDefinition nd = new(
                mycoolfin.TheSimsulator.Sims.Genotype.SimsGenotype.BRAIN_GID,
                ActivationFunction.Sin,
                new(new(biasAddy, 1f), new(biasAddy, 1f), new(biasAddy, 1f))
            );
            // SimsGenotype genotype = new(nodes, connections, new() { nd });
            SimsGenotype genotype = genotypeFactory.CreateInitialisedGenotype();

            SimsPhenotype phenotype = phenotypeFactory.ConstructPhenotype(genotype);

            for (int j = 0; j < phenotype.Limbs.Count; j++)
            {
                mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb = phenotype.Limbs[j];
                limb.SetPositionAndRotation(limb.Position, limb.Rotation);
            }

            phenotypes.Add(phenotype);
        }

        // for (int i = 0; i < phenotypes.Count; i++)
        // {
        //     SimsPhenotype phenotype = phenotypes[i];
        //     PhenotypeRenderer phenotypeRenderer = Instantiate(PhenotypeRendererPrefab).GetComponent<PhenotypeRenderer>();
        //     phenotypeRenderer.Initialise(phenotype, LimbRendererPrefab, JointRendererPrefab);
        //     phenotypeRenderer.transform.position = new UnityEngine.Vector3(0, 0, 10);
        // }

        List<PhenotypeEntityCreationInfo> phenotypeCreationInfoList = new();
        float spacing = 5f;
        int gridSize = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Sqrt(phenotypes.Count));
        for (int i = 0; i < phenotypes.Count; i++)
        {
            SimsPhenotype phenotype = phenotypes[i];

            int row = i / gridSize;
            int col = i % gridSize;

            float x = col * spacing;
            float z = row * spacing;

            PhenotypeEntityCreationInfo info = new()
            {
                Phenotype = phenotype,
                VisualOffset = new(x, 0, z),
                AllowInterPhenotypeCollisions = false
            };
            phenotypeCreationInfoList.Add(info);
        }

        EntityCreationAPI.CreateEntitiesFromPhenotypes(World.DefaultGameObjectInjectionWorld, phenotypeCreationInfoList);
    }
}
