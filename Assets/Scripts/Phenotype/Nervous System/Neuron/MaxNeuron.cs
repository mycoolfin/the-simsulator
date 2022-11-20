using UnityEngine;

public class MaxNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Max;

    protected override float Evaluate()
    {
        return Mathf.Max(WeightedInputValues);
    }
}
