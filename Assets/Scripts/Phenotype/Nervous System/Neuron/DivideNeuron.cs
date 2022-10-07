
public class DivideNeuron : NeuronBase
{
    public DivideNeuron() : base(2) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return (inputValues[1] == 0) ? 0 : inputValues[0] / inputValues[1];
    }
}
