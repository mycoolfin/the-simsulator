
public class ProductNeuron : NeuronBase
{
    public ProductNeuron() : base(2) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return inputValues[0] * inputValues[1];
    }
}
