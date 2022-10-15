using UnityEngine;

public class Tester : MonoBehaviour
{
    void Start()
    {
        PhenotypeConstructionTest();
    }

    private void PhenotypeConstructionTest()
    {
        Genotype genotype = Human();
        Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(genotype);
        phenotype.transform.position = new Vector3(0, 5f, 0);

        Physics.gravity = Vector3.zero;
    }

    private Genotype Worm()
    {
        NeuronNode oscSaw = new(NeuronType.OscillateSaw, new float[3] { Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[3] { Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
        NeuronNode product = new(NeuronType.Product, new float[2] { Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[2] { Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
        // NeuronNode ifNeuron = new(NeuronType.If, new float[3] { Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[3] { Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
        
        LimbNode segment = new(new Vector3(1f, 1f, 2f), JointType.TwistBend, new float[] { 90f, 90f }, new float[][] { new float[] { Random.Range(0f, 1f) }, new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { 1f }, new float[] { 1f } }, 2, new NeuronNode[] { product });
        LimbConnection connection = new(segment, 5, Vector2.zero, new Vector3(Random.Range(-90f, 90f), Random.Range(-90f, 90f), Random.Range(-90f, 90f)), new Vector3(0.8f, 0.8f, 0.8f), false, false);
        segment.connections = new[] { connection };
        LimbNode[] limbNodes = new[] { segment };
        NeuronNode[] brainNeuronNodes = new NeuronNode[] { oscSaw };
        return new Genotype(limbNodes, brainNeuronNodes);
    }

    private Genotype Human()
    {
        LimbNode body = new(new Vector3(0.5f, 0.5f, 0.5f), JointType.Rigid, null, null, null, 0, null);
        LimbNode head = new(new Vector3(0.3f, 0.3f, 0.4f), JointType.Twist, new float[] { 90f }, new float[][] { new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { Random.Range(-10f, 10f) } }, 0, null);
        LimbNode limb = new(new Vector3(0.1f, 0.1f, 0.4f), JointType.TwistBend, new float[] { 10f, 10f }, new float[][] { new float[] { Random.Range(0f, 1f) }, new float[] { Random.Range(0f, 1f) } }, new float[][] { new float[] { Random.Range(-10f, 10f) }, new float[] { Random.Range(-10f, 10f) } }, 1, null);

        LimbConnection headConnection = new(head, 4, Vector2.zero, Vector3.zero, Vector3.one, false, false);
        LimbConnection leftArmConnection = new(limb, 0, new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);
        LimbConnection rightArmConnection = new(limb, 0, new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, true, false);
        LimbConnection leftLegConnection = new(limb, 1, new Vector2(0f, -0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
        LimbConnection rightLegConnection = new(limb, 1, new Vector2(0f, 0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
        LimbConnection limbConnection = new(limb, 5, new Vector2(0f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);

        body.connections = new[] { headConnection, leftArmConnection, rightArmConnection, leftLegConnection, rightLegConnection };
        limb.connections = new[] { limbConnection };

        LimbNode[] limbNodes = new[] { body, limb };

        NeuronNode testNode = new(NeuronType.OscillateSaw, new float[3] { Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[3] { Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
        NeuronNode[] brainNeuronNodes = new[] { testNode };

        return new Genotype(limbNodes, brainNeuronNodes);
    }
}
