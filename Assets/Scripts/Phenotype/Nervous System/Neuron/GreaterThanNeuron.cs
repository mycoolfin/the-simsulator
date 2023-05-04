using System.Collections.Generic;

public class GreaterThanNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.GreaterThan;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        return inputValues[0] > inputValues[1] ? 1.0f : -1.0f;
    }
}
