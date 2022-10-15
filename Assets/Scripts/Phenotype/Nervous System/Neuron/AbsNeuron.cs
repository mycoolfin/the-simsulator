using UnityEngine;

public class AbsNeuron : NeuronBase
{
    public AbsNeuron() : base(1) {}

    protected override float Evaluate()
    {
        return Mathf.Abs(WeightedInputValues[0]);
    }
}
