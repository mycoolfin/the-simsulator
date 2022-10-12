
public interface ISignalReceiver
{
    ISignalEmitter[] Inputs { get; }
    float[] Weights { get; }
}
