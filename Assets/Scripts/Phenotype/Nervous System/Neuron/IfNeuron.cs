
public class IfNeuron : NeuronBase
{
    public IfNeuron() : base(3) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return (inputValues[0] > 0) ? inputValues[1] : inputValues[2];
    }
}
