using UnityEngine;

public abstract class SensorBase : NervousSystemNode
{
    public NervousSystemConnection[] outputs;

    private float inputValue;

    public float InputValue
    {
        get { return inputValue; }
        set
        {
            inputValue = Mathf.Clamp(value, -1f, 1f);
        }
    }
}
