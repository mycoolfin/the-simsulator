using UnityEngine;

public class AtanNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Atan;

    protected override float Evaluate()
    {
        return Mathf.Atan(GetWeightedInputValues()[0]);
    }
}
