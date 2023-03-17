using System.Collections.Generic;
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
        parent1Phenotype.transform.rotation *= Quaternion.Euler(0f, 0f, 180f);
        parent1Phenotype.transform.position = new Vector3(0, -parent1Phenotype.GetBounds().center.y + parent1Phenotype.GetBounds().extents.y * 2, 0);
        Physics.SyncTransforms();
    }

    private Genotype Worm()
    {
        NeuronDefinition wave = new(
            NeuronType.OscillateWave,
            new List<SignalReceiverInputDefinition>()
            {
                new(1f, 2f),
                new(1f, 1f),
                new(1f, 1f)
            }.AsReadOnly()
        );
        LimbConnection connection = new(0, 5, Vector2.zero, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f), false, false, false, false);
        LimbNode segment = new(new Vector3(1f, 0.1f, 0.5f),
            new JointDefinition(
                JointType.Revolute,
                new List<JointAxisDefinition>()
                {
                    new(45f, new SignalReceiverInputDefinition(0.2f, 1f)),
                    new(0f, new SignalReceiverInputDefinition(0f, 0f)),
                    new(0f, new SignalReceiverInputDefinition(0f, 0f))
                }.AsReadOnly()
            ),
            5,
            (new List<NeuronDefinition> { }).AsReadOnly(),
            (new List<LimbConnection> { connection }).AsReadOnly()
        );

        List<LimbNode> limbNodes = new() { segment };

        List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition> { wave };

        return new Genotype(null, null, brainNeuronDefinitions.AsReadOnly(), limbNodes.AsReadOnly());
    }

    // private Genotype Spinner()
    // {
    //     LimbConnection connection = new(0, 2, new Vector2(0f, 1f), new Vector3(60f, 0f, 0f), new Vector3(0.5f, 0.5f, 1f), false, false, true, false);
    //     LimbNode segment = new(new Vector3(1f, 1f, 1f),
    //     new JointDefinition(
    //         JointType.Revolute,
    //         (new List<float> { 90f }).AsReadOnly(),
    //          (new List<ReadOnlyCollection<float>> { (new List<float> { 1f }).AsReadOnly() }).AsReadOnly(),
    //          (new List<ReadOnlyCollection<float>> { (new List<float> { -1f }).AsReadOnly() }).AsReadOnly()),
    //           1,
    //            (new List<NeuronDefinition> { }).AsReadOnly(),
    //             (new List<LimbConnection> { connection }).AsReadOnly());

    //     List<LimbNode> limbNodes = new() { segment };

    //     List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition> { };

    //     return new Genotype(null, null, brainNeuronDefinitions.AsReadOnly(), limbNodes.AsReadOnly());
    // }
}
