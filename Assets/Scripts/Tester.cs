using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    // private Genotype Jointed()
    // {
    //     LimbConnection connection = new(0, 5, Vector2.zero, Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), false, false);
    //     LimbNode segment = new(new Vector3(10f, 10f, 20f), new JointDefinition(JointType.TwistBend, new List<float> { 90f, 90f }, new List<List<float>> { new List<float> { Random.Range(0f, 1f) }, new List<float> { Random.Range(0f, 1f) } }, new List<List<float>> { new List<float> { 1f }, new List<float> { 1f } }), 1, new List<NeuronDefinition> { }, new LimbConnection[] { connection });
    //     List<LimbNode> limbNodes = new() { segment };
    //     List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition> { };
    //     return new Genotype(brainNeuronDefinitions, limbNodes);
    // }

    private Genotype Worm()
    {

        NeuronDefinition wave = new(NeuronType.OscillateWave,
         (new List<float> { 1f, 1f, 1f }),
         (new List<float> { 2f, 1f, 1f })
         );

        LimbConnection connection = new(0, 5, Vector2.zero, Vector3.zero, new Vector3(0.9f, 1f, 1f), false, false);
        LimbNode segment = new(new Vector3(2f, 0.1f, 0.7f),
        new JointDefinition(
            JointType.Revolute,
            (new List<float> { 45f }).AsReadOnly(),
             (new List<ReadOnlyCollection<float>> { (new List<float> { 0.40f } ).AsReadOnly() } ).AsReadOnly(),
             (new List<ReadOnlyCollection<float>> { (new List<float> { 1f } ).AsReadOnly() } ).AsReadOnly()),
              15,
               (new List<NeuronDefinition> {  }).AsReadOnly(),
                (new List<LimbConnection> { connection }).AsReadOnly());
        List<LimbNode> limbNodes = new() { segment };

        List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition> { wave };

        return new Genotype(brainNeuronDefinitions.AsReadOnly(), limbNodes.AsReadOnly(), null);
    }

    //     private Genotype Human()
    //     {
    //         LimbConnection headConnection = new(1, 4, Vector2.zero, Vector3.zero, Vector3.one, false, false);
    //         LimbConnection leftArmConnection = new(2, 0, new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);
    //         LimbConnection rightArmConnection = new(2, 0, new Vector2(0.8f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, true, false);
    //         LimbConnection leftLegConnection = new(2, 1, new Vector2(0f, -0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
    //         LimbConnection rightLegConnection = new(2, 1, new Vector2(0f, 0.8f), new Vector3(-30f, -90f, 0f), Vector3.one, false, false);
    //         LimbConnection limbConnection = new(2, 5, new Vector2(0f, 0f), new Vector3(30f, 0f, 0f), Vector3.one, false, false);

    //         LimbNode body = new(new Vector3(0.5f, 0.5f, 0.5f), new JointDefinition(JointType.Rigid, null, null, null), 0, null, new LimbConnection[] { headConnection, leftArmConnection, rightArmConnection, leftLegConnection, rightLegConnection });
    //         LimbNode head = new(new Vector3(0.3f, 0.3f, 0.4f), new JointDefinition(JointType.Twist, new List<float> { 90f }, new List<List<float>> { new List<float> { Random.Range(0f, 1f) } }, new List<List<float>> { new List<float> { Random.Range(-10f, 10f) } }), 0, null, null);
    //         LimbNode limb = new(new Vector3(0.1f, 0.1f, 0.4f), new JointDefinition(JointType.TwistBend, new List<float> { 10f, 10f }, new List<List<float>> { new List<float> { Random.Range(0f, 1f) }, new List<float> { Random.Range(0f, 1f) } }, new List<List<float>> { new List<float> { Random.Range(-10f, 10f) }, new List<float> { Random.Range(-10f, 10f) } }), 1, null, new() { limbConnection });
    //         List<LimbNode> limbNodes = new() { body, head, limb };

    //         NeuronDefinition testNeuron = new(NeuronType.OscillateSaw, new float[3] { Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f) }, new float[3] { Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f) });
    //         List<NeuronDefinition> brainNeuronDefinitions = new() { testNeuron };

    //         return new Genotype(brainNeuronDefinitions, limbNodes);
    //     }
}
