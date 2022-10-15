
public interface ISignalReceiver
{
    ISignalEmitter[] Inputs { get; }
    float?[] InputOverrides { get; }
    float[] Weights { get; }
    float[] WeightedInputValues { get; }
}
