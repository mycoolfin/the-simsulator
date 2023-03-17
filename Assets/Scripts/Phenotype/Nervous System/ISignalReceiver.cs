using System.Collections.ObjectModel;
using System.Collections.Generic;

public interface ISignalReceiver
{
    ReadOnlyCollection<InputDefinition> InputDefinitions { get; }
    List<ISignalEmitter> Inputs { get; }
    List<float?> InputOverrides { get; }
    List<float> Weights { get; }
    List<float> GetWeightedInputValues();
}
