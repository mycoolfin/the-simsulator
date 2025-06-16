namespace mycoolfin.TheSimsulator.Sims
{
    public abstract class ActuatorBase : ISignalReceiver
    {
        public static float MinValue = -1f;
        public static float MaxValue = 1f;

        public SignalInput InputA { get; set; }
        public SignalInput InputB { get; set; }
        public SignalInput InputC { get; set; }

        public float Value;
    }
}
