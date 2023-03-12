using UnityEngine;

public class AbsNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Abs;

    protected override float Evaluate()
    {
        return Mathf.Abs(GetWeightedInputValues()[0]);
    }
}
