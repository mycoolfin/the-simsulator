using System;

public class Sensor
{
    private readonly SensorType type;
    public SensorType Type => type;
    private readonly SignalProcessor processor;
    public SignalProcessor Processor => processor;

    public float Excitation
    {
        set
        {
            processor.Emitter.SetOutputOverride(MathF.Max(-1f, MathF.Min(value, 1f)));
        }
    }

    public Sensor(SensorType type)
    {
        this.type = type;
        processor = new(null, null);
    }
}
