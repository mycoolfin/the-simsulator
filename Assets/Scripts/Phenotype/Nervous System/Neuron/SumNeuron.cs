
public class SumNeuron : NeuronBase
{
    public SumNeuron() : base(2) {}

    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return inputValues[0] + inputValues[1];
    }
}
