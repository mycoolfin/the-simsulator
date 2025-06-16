namespace mycoolfin.TheSimsulator.Sims
{
    public class JointAngleSensor : SensorBase
    {
        public void UpdateJointAngle(float eulerAngle, float angleLimit)
        {
            Output = System.Math.Clamp(eulerAngle / angleLimit, MinOutput, MaxOutput);
        }
    }
}
