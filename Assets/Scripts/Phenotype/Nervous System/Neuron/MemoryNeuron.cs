
public class MemoryNeuron : NeuronBase
{
    public MemoryNeuron() : base(2) {}

    private float storedValue = 0f;
    
    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        if (inputValues[0] > 0)
            storedValue = inputValues[1];
        return storedValue;
    }
}
