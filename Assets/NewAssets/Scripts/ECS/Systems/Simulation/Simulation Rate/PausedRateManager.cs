using Unity.Entities;

public unsafe class PausedRateManager : IRateManager
{
    public float Timestep
    {
        get => 0f;
        set => throw new System.NotSupportedException("Cannot set timestep on PausedRateManager.");
    }

    public bool ShouldGroupUpdate(ComponentSystemGroup group)
    {
        return false;
    }
}
