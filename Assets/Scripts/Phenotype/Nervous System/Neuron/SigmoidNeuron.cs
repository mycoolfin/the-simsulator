using UnityEngine;

public class SigmoidNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Sigmoid;

    protected override float Evaluate()
    {
        return 1f / (1f + Mathf.Exp(WeightedInputValues[0]));
    }
}
