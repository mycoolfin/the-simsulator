using UnityEngine;

public class Tester : MonoBehaviour
{
    void Start()
    {
        PhenotypeConstructionTest();
    }

    private void PhenotypeConstructionTest()
    {
        Genotype human = Human();
        Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(human);
        phenotype.transform.position = new Vector3(0, 5f, 0);

        // Physics.gravity = Vector3.zero;
    }

    private Genotype Human()
    {
        LimbNode body = new (new Vector3(0.5f, 1f, 0.5f), JointType.Rigid, null, null, null, 0, null);
        LimbNode head = new (new Vector3(0.3f, 0.5f, 0.3f), JointType.Twist, new float[] { 90f }, new float[][] { new float[] { 1f } }, new float[][] { new float[] { 1f } }, 0, null);
        LimbNode limb = new (new Vector3(0.1f, 0.1f, 0.1f), JointType.TwistBend, new float[] { 10f, 10f }, new float[][] { new float[] { 1f }, new float[] { 1f } }, new float[][] { new float[] { 1f }, new float[] { 1f } }, 0, null);

        LimbConnection headConnection = new (head, 4, Vector2.zero, Vector3.zero, Vector3.one, false, false);
        LimbConnection leftArmConnection = new (limb, 0,  new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);
        LimbConnection rightArmConnection = new(limb, 0,  new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, true, false);
        LimbConnection leftLegConnection = new(limb, 1,  new Vector2(0f, -0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
        LimbConnection rightLegConnection = new(limb, 1,  new Vector2(0f, 0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
        LimbConnection limbConnection = new(limb, 5,  new Vector2(0f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);

        body.connections = new[] { headConnection, leftArmConnection, rightArmConnection, leftLegConnection, rightLegConnection };
        limb.connections = new[] { limbConnection };

        LimbNode[] limbNodes = new[] { body, limb };

        NeuronNode testNode = new (NeuronType.OscillateSaw, new float[3] { 0.25f, 0.5f, 0.75f }, new float[3] { 1f, 1f, 1f });
        NeuronNode[] brainNeuronNodes = new[] { testNode };

        return new Genotype(limbNodes, brainNeuronNodes);
    }
}
