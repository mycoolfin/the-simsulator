using System.Collections.ObjectModel;
using System.Collections.Generic;

public interface ISignalReceiver
{
    ReadOnlyCollection<SignalReceiverInputDefinition> InputDefinitions { get; }
    List<ISignalEmitter> Inputs { get; }
    List<float?> InputOverrides { get; }
    List<float> Weights { get; }
    List<float> GetWeightedInputValues();
}
