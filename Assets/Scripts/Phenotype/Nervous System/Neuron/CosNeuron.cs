using UnityEngine;

public class CosNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Cos;

    protected override float Evaluate()
    {
        return Mathf.Cos(GetWeightedInputValues()[0]);
    }
}
