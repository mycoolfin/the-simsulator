using UnityEngine;

public class SignOfNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.SignOf;

    protected override float Evaluate()
    {
        return Mathf.Sign(GetWeightedInputValues()[0]);
    }
}
