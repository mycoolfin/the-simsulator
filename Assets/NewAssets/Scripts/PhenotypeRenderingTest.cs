using System.Collections.Generic;
using UnityEngine;
using mycoolfin.TheSimsulator.Sims;

public class PhenotypeRenderingTest : MonoBehaviour
{
    public GameObject PhenotypeRendererPrefab;
    public GameObject LimbRendererPrefab;

    private void Start()
    {
        if (PhenotypeRendererPrefab == null)
        {
            Debug.LogError("PhenotypeRendererPrefab is not assigned.");
            return;
        }

        SimsGenotypeFactory genotypeFactory = new(0.4f, 0.3f, 0.3f, 2);
        SimsPhenotypeFactory phenotypeFactory = new();

        for (int i = 0; i < 6; i++)
        {
            List<Node> nodes = new()
            {
                new Node(
                    new mycoolfin.TheSimsulator.Vector3(1f, 1f, 1f),
                    new(),
                    2
                )
            };
            List<Connection> connections = new()
            {
                new Connection(
                    nodes[0].Gid,
                    nodes[0].Gid,
                    i,
                    new mycoolfin.TheSimsulator.Vector2(1f, 1f),
                    new mycoolfin.TheSimsulator.Vector3(0, 0, 0),
                    // new mycoolfin.TheSimsulator.Vector3(30f, 30f, 30f),
                    new mycoolfin.TheSimsulator.Vector3(0.5f, 0.5f, 0.5f),
                    false,
                    false,
                    true,
                    false
                )
            };
            SimsGenotype genotype = new(nodes, connections, new());
            // SimsGenotype genotype = genotypeFactory.CreateInitialisedGenotype();

            SimsPhenotype phenotype = phenotypeFactory.ConstructPhenotype(genotype);

            PhenotypeRenderer phenotypeRenderer = Instantiate(PhenotypeRendererPrefab).GetComponent<PhenotypeRenderer>();
            phenotypeRenderer.Initialise(phenotype, LimbRendererPrefab);
            phenotypeRenderer.transform.position = new Vector3(i * 4, 0, 0);
        }
    }
}
