using UnityEngine;

public class MinNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Min;

    protected override float Evaluate()
    {
        return Mathf.Max(WeightedInputValues);
    }
}
