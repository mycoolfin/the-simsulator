using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public abstract class JointBase : ISensorContainer, IActuatorContainer, IDisposable
    {
        public readonly Limb ParentLimb;
        public readonly Limb ChildLimb;

        public readonly Vector3 LocalParentAnchor;
        public readonly Vector3 LocalChildAnchor;

        public readonly Vector3 AngleLimits;

        public Vector3 DesiredAngles { get; private set; }
        public Vector3 ActualAngles { get; private set; }

        public readonly JointAxis XAxis;
        public readonly JointAxis YAxis;
        public readonly JointAxis ZAxis;

        private readonly List<SensorBase> sensors = new();
        private readonly List<ActuatorBase> actuators = new();

        public IEnumerable<SensorBase> Sensors => sensors;
        public IEnumerable<ActuatorBase> Actuators => actuators;

        public event Action OnDispose;

        public JointBase(
            Limb parentLimb,
            Limb childLimb,
            Vector3 localParentAnchor,
            Vector3 localChildAnchor,
            Vector3 angleLimits,
            JointAxis xAxis,
            JointAxis yAxis,
            JointAxis zAxis
        )
        {
            ParentLimb = parentLimb ?? throw new ArgumentNullException(nameof(parentLimb), "Parent limb cannot be null.");
            ChildLimb = childLimb ?? throw new ArgumentNullException(nameof(childLimb), "Child limb cannot be null.");

            LocalParentAnchor = localParentAnchor;
            LocalChildAnchor = localChildAnchor;

            AngleLimits = angleLimits;

            DesiredAngles = Vector3.Zero;
            ActualAngles = Vector3.Zero;

            XAxis = xAxis;
            YAxis = yAxis;
            ZAxis = zAxis;

            if (xAxis.Sensor != null) sensors.Add(xAxis.Sensor);
            if (yAxis.Sensor != null) sensors.Add(yAxis.Sensor);
            if (zAxis.Sensor != null) sensors.Add(zAxis.Sensor);

            if (xAxis.Actuator != null) actuators.Add(xAxis.Actuator);
            if (yAxis.Actuator != null) actuators.Add(yAxis.Actuator);
            if (zAxis.Actuator != null) actuators.Add(zAxis.Actuator);
        }

        public void SetActualAngles(Vector3 actualAngles)
        {
            ActualAngles = actualAngles;

            XAxis?.Sensor.UpdateJointAngle(ActualAngles.X, AngleLimits.X);
            YAxis?.Sensor.UpdateJointAngle(ActualAngles.Y, AngleLimits.Y);
            ZAxis?.Sensor.UpdateJointAngle(ActualAngles.Z, AngleLimits.Z);
        }

        public void UpdateDesiredAngles()
        {
            DesiredAngles = new Vector3(
                ConvertActuatorValueToEulerAngle(XAxis?.Actuator.Value ?? 0f, AngleLimits.X),
                ConvertActuatorValueToEulerAngle(YAxis?.Actuator.Value ?? 0f, AngleLimits.Y),
                ConvertActuatorValueToEulerAngle(ZAxis?.Actuator.Value ?? 0f, AngleLimits.Z)
            );
        }

        private float ConvertActuatorValueToEulerAngle(float actuatorValue, float angleLimit)
        {
            return actuatorValue * angleLimit;
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
