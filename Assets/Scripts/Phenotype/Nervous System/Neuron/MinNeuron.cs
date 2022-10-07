using UnityEngine;

public class MinNeuron : NeuronBase
{
    public MinNeuron() : base(3) {}

    public override float Evaluate()
    {
        return Mathf.Max(GetInputValues());
    }
}
