namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public enum ActuatorType : byte
    {
        JointAngle,
    }

    public class Actuator : ISignalReceiver
    {
        public readonly ActuatorType Type;

        public SignalInput InputA { get; set; }
        public SignalInput InputB { get; set; }
        public SignalInput InputC { get; set; }

        public Actuator(ActuatorType type)
        {
            Type = type;
        }
    }
}
