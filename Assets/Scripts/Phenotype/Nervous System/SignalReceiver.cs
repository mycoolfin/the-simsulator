using System.Collections.ObjectModel;

public class SignalReceiver
{
    private readonly ReadOnlyCollection<InputDefinition> inputDefinitions;
    public ReadOnlyCollection<InputDefinition> InputDefinitions => inputDefinitions;
    private readonly SignalEmitter[] inputs;
    public SignalEmitter[] Inputs => inputs;
    private readonly float[] weights;
    public float[] Weights => weights;
    private readonly float[] weightedInputValues;
    public float[] WeightedInputValues
    {
        get
        {
            // If there is no input specified, use the weight as a constant input.
            for (int i = 0; i < inputs.Length; i++)
                weightedInputValues[i] = (inputs[i]?.OutputValue ?? 1f) * Weights[i];
            return weightedInputValues;
        }
    }

    public SignalReceiver(ReadOnlyCollection<InputDefinition> inputDefinitions)
    {
        this.inputDefinitions = inputDefinitions;
        inputs = new SignalEmitter[inputDefinitions?.Count ?? 0];
        weights = new float[inputDefinitions?.Count ?? 0];
        weightedInputValues = new float[inputDefinitions?.Count ?? 0];
    }

    public void SetInput(int index, SignalEmitter input)
    {
        inputs[index] = input;
    }

    public void SetWeight(int index, float weight)
    {
        weights[index] = weight;
    }
}
