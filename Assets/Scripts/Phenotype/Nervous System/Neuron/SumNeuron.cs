using System.Collections.Generic;

public class SumNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Sum;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        return inputValues[0] + inputValues[1];
    }
}
