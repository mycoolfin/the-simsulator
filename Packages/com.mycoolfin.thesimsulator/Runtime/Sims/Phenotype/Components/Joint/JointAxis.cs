namespace mycoolfin.TheSimsulator.Sims
{
    public class JointAxisController
    {
        public JointAngleSensor Sensor { get; }
        public JointAngleActuator Actuator { get; }

        public JointAxisController(JointAngleSensor sensor, JointAngleActuator actuator)
        {
            Sensor = sensor;
            Actuator = actuator;
        }
    }
}
