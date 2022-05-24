using UnityEngine;
using System.Collections;

public class MinNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Min(GetInputValues());
    }
}
