
public class DivideNeuron : NeuronBase
{
    public DivideNeuron() : base(2) {}

    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return (inputValues[1] == 0) ? 0 : inputValues[0] / inputValues[1];
    }
}
