﻿using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
    // void Start()
    // {
    //     Phenotype parent1Phenotype = Phenotype.Construct(Worm());
    //     parent1Phenotype.transform.rotation *= Quaternion.Euler(0f, 0f, 180f);
    //     parent1Phenotype.transform.position = new Vector3(0, -parent1Phenotype.GetBounds().center.y + parent1Phenotype.GetBounds().extents.y * 2, 0);
    //     Physics.SyncTransforms();
    // }

    // private Genotype Worm()
    // {
    //     NeuronDefinition wave = new(
    //         NeuronType.OscillateWave,
    //         new List<InputDefinition>()
    //         {
    //             new(EmitterSetLocation.None, -1, null, -1, 2f),
    //             new(EmitterSetLocation.LimbInstances, -1, "000", 0,  1f),
    //             new(EmitterSetLocation.None, -1, null, -1, 1f)
    //         }.AsReadOnly()
    //     );
    //     LimbConnection connection = new(0, 5, Vector2.zero, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f), false, false, true, false);
    //     LimbNode segment = new(new Vector3(1f, 0.1f, 0.5f),
    //         new JointDefinition(
    //             JointType.Revolute,
    //             new List<JointAxisDefinition>()
    //             {
    //                 new(45f, new(EmitterSetLocation.Brain,     -1, null,    0, 1f)),
    //                 new(0f,  new(EmitterSetLocation.None,        -1, null, -1,  0f)),
    //                 new(0f,  new(EmitterSetLocation.None,        -1, null, -1,  0f))
    //             }.AsReadOnly()
    //         ),
    //         5,
    //         (new List<NeuronDefinition> { }).AsReadOnly(),
    //         (new List<LimbConnection> { connection }).AsReadOnly()
    //     );

    //     List<LimbNode> limbNodes = new() { segment };

    //     List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition> { wave };

    //     return Genotype.Construct(null, null, brainNeuronDefinitions.AsReadOnly(), limbNodes.AsReadOnly());
    // }
}
