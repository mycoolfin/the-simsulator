using System.Collections.Generic;

public class DivideNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Divide;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        return (inputValues[1] == 0) ? 0 : inputValues[0] / inputValues[1];
    }
}
