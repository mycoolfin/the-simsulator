using System;
using System.Collections.ObjectModel;

public class SignalProcessor
{
    private readonly SignalReceiver receiver;
    public SignalReceiver Receiver => receiver;
    private readonly SignalEmitter emitter;
    public SignalEmitter Emitter => emitter;
    private readonly Func<float, float, float, float> evaluationFunction;

    public SignalProcessor(ReadOnlyCollection<InputDefinition> inputDefinitions, Func<float, float, float, float> evaluationFunction)
    {
        receiver = new(inputDefinitions);
        emitter = new();
        this.evaluationFunction = evaluationFunction;
    }

    private float ProcessSignal()
    {
        float[] inputValues = receiver.WeightedInputValues;
        return evaluationFunction(
            inputValues.Length > 0 ? inputValues[0] : 0f,
            inputValues.Length > 1 ? inputValues[1] : 0f,
            inputValues.Length > 2 ? inputValues[2] : 0f
        );
    }

    // Signal propagation is split into two phases, allowing us to wait until
    // all propagators have computed their next state before modifying outputs.
    public void PropagatePhaseOne()
    {
        emitter.SetPendingOutput(ProcessSignal);
    }

    public void PropagatePhaseTwo()
    {
        emitter.FlushOutput();
    }
}
