namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class JointAngleSensor : Sensor
    {
        public readonly JointAxis Axis;

        public JointAngleSensor(JointAxis axis) : base(SensorType.JointAngle)
        {
            Axis = axis;
        }
    }
}
