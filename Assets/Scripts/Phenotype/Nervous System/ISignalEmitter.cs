using System.Collections.Generic;

public interface ISignalEmitter
{
    float OutputValue { get; set; }
    List<ISignalReceiver> Consumers { get; set; }
    bool Disabled { get; set; }
}
