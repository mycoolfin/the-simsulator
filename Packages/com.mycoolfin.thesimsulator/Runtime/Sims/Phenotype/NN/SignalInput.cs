namespace mycoolfin.TheSimsulator.Sims
{
    public class SignalInput
    {
        public static float MinWeight = -1f;
        public static float MaxWeight = 1f;

        public ISignalEmitter Emitter { get; private set; }
        public float Weight { get; private set; }

        public SignalInput(ISignalEmitter emitter, float weight)
        {
            Emitter = emitter;
            Weight = System.Math.Clamp(weight, MinWeight, MaxWeight);
        }
    }
}
