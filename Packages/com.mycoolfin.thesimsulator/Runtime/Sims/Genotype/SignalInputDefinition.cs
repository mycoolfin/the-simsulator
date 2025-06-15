using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct SignalInputDefinition
    {
        public static readonly float MinWeight = -1.0f;
        public static readonly float MaxWeight = 1.0f;

        [FieldOffset(0)] public readonly SignalEmitterAddress SignalEmitterAddress;
        [FieldOffset(4)] public readonly float Weight;

        public SignalInputDefinition(SignalEmitterAddress signalEmitterAddress, float weight)
        {
            SignalEmitterAddress = signalEmitterAddress;
            Weight = weight;
        }

        public static SignalInputDefinition CreateRandom(SignalEmitterAddress signalEmitterAddress)
        {
            float weight = (float)SharedRandom.NextDouble() * (MaxWeight - MinWeight) + MinWeight;
            return new SignalInputDefinition(signalEmitterAddress, weight);
        }
    }
}
