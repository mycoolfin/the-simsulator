using UnityEngine;

public class ExptNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Expt;

    protected override float Evaluate()
    {
        return Mathf.Exp(WeightedInputValues[0]);
    }
}
