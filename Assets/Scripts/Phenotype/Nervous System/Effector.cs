using System.Collections.ObjectModel;

public class Effector
{
    private readonly EffectorType type;
    public EffectorType Type => type;
    private readonly SignalProcessor processor;
    public SignalProcessor Processor => processor;
    public float Excitation
    {
        get
        {
            return processor.Emitter.OutputValue;
        }
    }

    public Effector(EffectorType type, ReadOnlyCollection<InputDefinition> inputDefinitions)
    {
        this.type = type;
        processor = new(inputDefinitions, (i1, i2, i3) => i1);
        processor.Emitter.SetDisabled(false); // Effector emitters are never disabled.
    }
}
