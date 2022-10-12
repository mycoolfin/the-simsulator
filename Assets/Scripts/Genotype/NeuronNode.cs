
public class NeuronNode
{
    public readonly NeuronType type;
    public readonly float[] inputPreferences;
    public readonly float[] inputWeights;

    public NeuronNode(NeuronType type, float[] inputPreferences, float[] inputWeights)
    {
        if (inputPreferences.Length != inputWeights.Length)
            throw new System.ArgumentException("Input parameters are improperly specified");
        
        this.type = type;
        this.inputPreferences = inputPreferences;
        this.inputWeights = inputWeights;
    }
}
