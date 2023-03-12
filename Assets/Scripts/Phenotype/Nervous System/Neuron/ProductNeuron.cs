using System.Collections.Generic;

public class ProductNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Product;

    protected override float Evaluate()
    {
        List<float> inputValues = GetWeightedInputValues();
        return inputValues[0] * inputValues[1];
    }
}
