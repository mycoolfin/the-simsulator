using System.Collections.Generic;
using UnityEngine;

public abstract class SensorBase : ISignalEmitter
{
    private float outputValue;
    public float OutputValue
    {
        get => outputValue;
        set => outputValue = Mathf.Clamp(value, -1f, 1f);
    }
    public List<ISignalReceiver> Consumers { get; set; }
    public bool Disabled { get; set; }

    protected SensorBase()
    {
        Consumers = new();
    }
}
