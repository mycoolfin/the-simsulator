using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Joint : ISensorContainer, IActuatorContainer, IDisposable
    {
        public readonly JointType Type;

        public readonly Limb ParentLimb;
        public readonly Limb ChildLimb;

        public readonly Vector3 ParentSpaceAnchor;
        public readonly Vector3 ParentSpaceXAxis;
        public readonly Vector3 ParentSpaceYAxis;
        public readonly Vector3 ParentSpaceZAxis;

        public readonly Vector3 AngleLimits;

        public Vector3 DesiredAngles { get; private set; }
        public Vector3 ActualAngles { get; private set; }

        public readonly JointAxisController XAxisController;
        public readonly JointAxisController YAxisController;
        public readonly JointAxisController ZAxisController;

        private readonly List<SensorBase> sensors = new();
        private readonly List<ActuatorBase> actuators = new();

        public IEnumerable<SensorBase> Sensors => sensors;
        public IEnumerable<ActuatorBase> Actuators => actuators;

        public event Action OnDispose;

        public Joint(
            JointType type,
            Limb parentLimb,
            Limb childLimb,
            Vector3 parentSpaceAnchor,
            Vector3 parentSpaceXAxis,
            Vector3 parentSpaceYAxis,
            Vector3 parentSpaceZAxis,
            Vector3 angleLimits
        )
        {
            Type = type;

            ParentLimb = parentLimb ?? throw new ArgumentNullException(nameof(parentLimb), "Parent limb cannot be null.");
            ChildLimb = childLimb ?? throw new ArgumentNullException(nameof(childLimb), "Child limb cannot be null.");

            ParentSpaceAnchor = parentSpaceAnchor;
            ParentSpaceXAxis = parentSpaceXAxis;
            ParentSpaceYAxis = parentSpaceYAxis;
            ParentSpaceZAxis = parentSpaceZAxis;

            AngleLimits = angleLimits;

            DesiredAngles = Vector3.Zero;
            ActualAngles = Vector3.Zero;

            XAxisController = (type != JointType.Rigid) ? new(new(), new()) : null;
            YAxisController = (type != JointType.Rigid && type != JointType.Revolute && type != JointType.Twist) ? new(new(), new()) : null;
            ZAxisController = (type == JointType.Spherical) ? new(new(), new()) : null;

            if (XAxisController?.Sensor != null) sensors.Add(XAxisController.Sensor);
            if (YAxisController?.Sensor != null) sensors.Add(YAxisController.Sensor);
            if (ZAxisController?.Sensor != null) sensors.Add(ZAxisController.Sensor);

            if (XAxisController?.Actuator != null) actuators.Add(XAxisController.Actuator);
            if (YAxisController?.Actuator != null) actuators.Add(YAxisController.Actuator);
            if (ZAxisController?.Actuator != null) actuators.Add(ZAxisController.Actuator);
        }

        public void SetActualAngles(Vector3 actualAngles)
        {
            ActualAngles = actualAngles;

            XAxisController?.Sensor.UpdateJointAngle(ActualAngles.X, AngleLimits.X);
            YAxisController?.Sensor.UpdateJointAngle(ActualAngles.Y, AngleLimits.Y);
            ZAxisController?.Sensor.UpdateJointAngle(ActualAngles.Z, AngleLimits.Z);
        }

        public void UpdateDesiredAngles()
        {
            DesiredAngles = new Vector3(
                ConvertActuatorValueToEulerAngle(XAxisController?.Actuator.Value ?? 0f, AngleLimits.X),
                ConvertActuatorValueToEulerAngle(YAxisController?.Actuator.Value ?? 0f, AngleLimits.Y),
                ConvertActuatorValueToEulerAngle(ZAxisController?.Actuator.Value ?? 0f, AngleLimits.Z)
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
