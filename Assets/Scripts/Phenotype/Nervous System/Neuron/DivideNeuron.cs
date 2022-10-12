
public class DivideNeuron : NeuronBase
{
    public DivideNeuron() : base(2) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return (inputValues[1] == 0) ? 0 : inputValues[0] / inputValues[1];
    }
}
