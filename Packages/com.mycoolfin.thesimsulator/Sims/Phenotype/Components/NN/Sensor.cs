namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public enum SensorType : byte
    {
        JointAngle,
        Light,
        ContactTotalLoad,
        ContactDirectionalLoad,
        ContactSlip
    }

    public class Sensor : ISignalEmitter
    {
        public readonly SensorType Type;

        public Sensor(SensorType type)
        {
            Type = type;
        }
    }
}
