using System.Collections.ObjectModel;

// The essential info for generating an emitter availability map.
public interface ILimbNodeEssentialInfo
{
    public int RecursiveLimit { get; }
    public ReadOnlyCollection<LimbConnection> Connections { get; }
    public int SignalEmitterCount { get; }
}
