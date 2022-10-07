using UnityEngine;

public class MaxNeuron : NeuronBase
{
    public MaxNeuron() : base(3) {}

    public override float Evaluate()
    {
        return Mathf.Max(GetInputValues());
    }
}
