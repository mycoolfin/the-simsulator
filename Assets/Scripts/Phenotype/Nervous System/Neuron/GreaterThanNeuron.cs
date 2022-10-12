
public class GreaterThanNeuron : NeuronBase
{
    public GreaterThanNeuron() : base(2) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return inputValues[0] > inputValues[1] ? 1.0f : -1.0f;
    }
}
