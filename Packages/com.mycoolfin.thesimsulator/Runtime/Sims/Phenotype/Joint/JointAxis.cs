namespace mycoolfin.TheSimsulator.Sims
{
    public class JointAxis
    {
        public JointAngleSensor Sensor { get; }
        public JointAngleActuator Actuator { get; }

        public JointAxis(JointAngleSensor sensor, JointAngleActuator actuator)
        {
            Sensor = sensor;
            Actuator = actuator;
        }
    }
}
