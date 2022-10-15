using UnityEngine;

public class MaxNeuron : NeuronBase
{
    public MaxNeuron() : base(3) {}

    protected override float Evaluate()
    {
        return Mathf.Max(WeightedInputValues);
    }
}
