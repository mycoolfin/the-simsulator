using UnityEngine;
using System.Collections;
using System.Linq;

public class MaxNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Max(GetInputValues());
    }
}
