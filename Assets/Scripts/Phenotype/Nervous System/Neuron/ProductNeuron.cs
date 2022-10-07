
public class ProductNeuron : NeuronBase
{
    public ProductNeuron() : base(2) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return inputValues[0] * inputValues[1];
    }
}
