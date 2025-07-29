namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public enum SensorType : byte
    {
        JointAngle
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
