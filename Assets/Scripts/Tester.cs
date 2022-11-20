using UnityEngine;

public class Tester : MonoBehaviour
{
    void Start()
    {
        // Phenotype j = PhenotypeBuilder.ConstructPhenotype(Jointed());
        // j.transform.position = new Vector3(0, 3, 0);
        // j.GetComponentInChildren<Rigidbody>().isKinematic = true;
        // Physics.SyncTransforms();
        // Physics.gravity = Vector3.zero;


        Phenotype parent1Phenotype = PhenotypeBuilder.ConstructPhenotype(Worm());
        parent1Phenotype.transform.position = new Vector3(10, -parent1Phenotype.GetBounds().center.y + parent1Phenotype.GetBounds().extents.y, 0);
        Physics.SyncTransforms();
    }

    private void OffspringTest()
    {

        Genotype parent1 = Worm();
        Genotype parent2 = Genotype.CreateRandom();
        Genotype child = Reproduction.CreateOffspring(parent1, parent2);

        Phenotype parent1Phenotype = PhenotypeBuilder.ConstructPhenotype(parent1);
        parent1Phenotype.transform.position = new Vector3(10, -parent1Phenotype.GetBounds().center.y + parent1Phenotype.GetBounds().extents.y, 0);
        Physics.SyncTransforms();

        Phenotype parent2Phenotype = PhenotypeBuilder.ConstructPhenotype(parent2);
        parent2Phenotype.transform.position = new Vector3(20, -parent2Phenotype.GetBounds().center.y + parent2Phenotype.GetBounds().extents.y, 0);
        Physics.SyncTransforms();

        Phenotype childPhenotype = PhenotypeBuilder.ConstructPhenotype(child);
        childPhenotype.transform.position = new Vector3(30, -childPhenotype.GetBounds().center.y + childPhenotype.GetBounds().extents.y, 0);
        Physics.SyncTransforms();
    }

    private void PhenotypeConstructionTest()
    {
        Genotype genotype = Genotype.CreateRandom();
        Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(genotype);

        if (!phenotype.IsValid())
        {
            Debug.Log("Invalid phenotype");
            Destroy(phenotype.gameObject);
            return;
        }

        phenotype.transform.position = new Vector3(0, -phenotype.GetBounds().center.y + phenotype.GetBounds().extents.y, 0);

        // Physics.gravity = Vector3.zero;
        // Time.timeScale = 10;
    }

    private Genotype Jointed()
    {
        LimbConnection connection = new(0, 5, Vector2.zero, Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), false, false);
        LimbNode segment = new(new Vector3(10f, 10f, 20f), new JointDefinition(JointType.TwistBend, new float[] { 90f, 90f }, new float[][] { new float[] { Random.Range(0f, 1f) }, new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { 1f }, new float[] { 1f } }), 1, new NeuronDefinition[] { }, new LimbConnection[] { connection });
        LimbNode[] limbNodes = new[] { segment };
        NeuronDefinition[] brainNeuronDefinitions = new NeuronDefinition[] { };
        return new Genotype(brainNeuronDefinitions, limbNodes);
    }

    private Genotype Worm()
    {
        NeuronDefinition product = new(NeuronType.Product, new float[2] { Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[2] { Random.Range(-10f, 10f), Random.Range(-10f, 10f) });

        LimbConnection connection = new(0, 5, Vector2.zero, Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), false, false);
        LimbNode segment = new(new Vector3(1f, 1f, 2f), new JointDefinition(JointType.TwistBend, new float[] { 90f, 90f }, new float[][] { new float[] { Random.Range(0f, 1f) }, new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { 1f }, new float[] { 1f } }), 20, new NeuronDefinition[] { product }, new LimbConnection[] { connection });
        LimbNode[] limbNodes = new[] { segment };


        NeuronDefinition oscSaw = new(NeuronType.OscillateSaw, new float[3] { Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[3] { Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
        NeuronDefinition[] brainNeuronDefinitions = new NeuronDefinition[] { oscSaw };

        return new Genotype(brainNeuronDefinitions, limbNodes);
    }

    private Genotype Human()
    {
        LimbConnection headConnection = new(1, 4, Vector2.zero, Vector3.zero, Vector3.one, false, false);
        LimbConnection leftArmConnection = new(2, 0, new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);
        LimbConnection rightArmConnection = new(2, 0, new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, true, false);
        LimbConnection leftLegConnection = new(2, 1, new Vector2(0f, -0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
        LimbConnection rightLegConnection = new(2, 1, new Vector2(0f, 0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
        LimbConnection limbConnection = new(2, 5, new Vector2(0f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);

        LimbNode body = new(new Vector3(0.5f, 0.5f, 0.5f), new JointDefinition(JointType.Rigid, null, null, null), 0, null, new LimbConnection[] { headConnection, leftArmConnection, rightArmConnection, leftLegConnection, rightLegConnection });
        LimbNode head = new(new Vector3(0.3f, 0.3f, 0.4f), new JointDefinition(JointType.Twist, new float[] { 90f }, new float[][] { new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { Random.Range(-10f, 10f) } }), 0, null, null);
        LimbNode limb = new(new Vector3(0.1f, 0.1f, 0.4f), new JointDefinition(JointType.TwistBend, new float[] { 10f, 10f }, new float[][] { new float[] { Random.Range(0f, 1f) }, new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { Random.Range(-10f, 10f) }, new float[] { Random.Range(-10f, 10f) } }), 1, null, new[] { limbConnection });
        LimbNode[] limbNodes = new[] { body, head, limb };

        NeuronDefinition testNeuron = new(NeuronType.OscillateSaw, new float[3] { Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[3] { Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
        NeuronDefinition[] brainNeuronDefinitions = new[] { testNeuron };

        return new Genotype(brainNeuronDefinitions, limbNodes);
    }
}
