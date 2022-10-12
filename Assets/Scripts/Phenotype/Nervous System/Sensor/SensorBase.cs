using UnityEngine;

public abstract class SensorBase : ISignalEmitter
{
    private float outputValue;
    public float OutputValue
    {
        get { return outputValue; }
        set
        {
            outputValue = Mathf.Clamp(value, -1f, 1f);
        }
    }
}
