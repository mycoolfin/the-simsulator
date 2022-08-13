using UnityEngine;
using System.Collections;
using System.Linq;

public class ProductNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return GetInputValues().Aggregate(1f, (a, b) => a * b);
    }
}
