using System.Collections.Generic;

public interface ISignalReceiver
{
    List<ISignalEmitter> Inputs { get; }
    List<float?> InputOverrides { get; }
    List<float> Weights { get; }
    List<float> GetWeightedInputValues();
}
