using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SignalInputDefinition
    {
        public const float MIN_WEIGHT = -1.0f;
        public const float MAX_WEIGHT = 1.0f;

        public SignalEmitterAddress SignalEmitterAddress { get; set; }
        public float Weight { get; set; }

        public SignalInputDefinition(SignalEmitterAddress signalEmitterAddress, float weight)
        {
            SignalEmitterAddress = signalEmitterAddress;
            Weight = weight;
        }

        public static SignalInputDefinition CreateRandom(SignalEmitterAddress signalEmitterAddress)
        {
            float weight = (float)SharedRandom.NextDouble() * (MAX_WEIGHT - MIN_WEIGHT) + MIN_WEIGHT;
            return new SignalInputDefinition(signalEmitterAddress, weight);
        }
    }
}
