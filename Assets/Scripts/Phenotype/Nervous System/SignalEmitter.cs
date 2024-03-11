using System;

public class SignalEmitter
{
    private float pendingOutputValue;
    private float outputValue;
    public float OutputValue => outputValue;
    private bool disabled;
    public bool Disabled => disabled;
    private float? outputOverride;

    public SignalEmitter()
    {
        pendingOutputValue = 0f;
        outputValue = 0f;
        disabled = true; // All emitters start disabled and will be enabled if they have a purpose.
        outputOverride = null;
    }

    public void SetOutputOverride(float? outputOverride)
    {
        this.outputOverride = outputOverride;
    }

    public void SetDisabled(bool disabled)
    {
        this.disabled = disabled;
    }

    public void SetPendingOutput(Func<float> calculateOutput)
    {
        if (Disabled) return;
        pendingOutputValue = outputOverride ?? MathF.Max(-1f, MathF.Min(calculateOutput(), 1f));
    }

    public void FlushOutput()
    {
        if (Disabled) return;
        outputValue = pendingOutputValue;
    }
}
