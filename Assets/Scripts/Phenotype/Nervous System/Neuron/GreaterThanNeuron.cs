
public class GreaterThanNeuron : NeuronBase
{
    public GreaterThanNeuron() : base(2) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return inputValues[0] > inputValues[1] ? 1.0f : -1.0f;
    }
}
