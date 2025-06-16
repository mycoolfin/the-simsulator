namespace mycoolfin.TheSimsulator.Sims
{
    public abstract class SensorBase : ISignalEmitter
    {
        public static float MinOutput = -1f;
        public static float MaxOutput = 1f;

        public float Output
        {
            get
            {
                return Output;
            }
            set
            {
                Output = System.Math.Clamp(value, MinOutput, MaxOutput);
            }
        }
    }
}
