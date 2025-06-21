using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public abstract class JointBase : ISensorContainer, IActuatorContainer, IDisposable
    {
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

        public JointBase(
            Limb parentLimb,
            Limb childLimb,
            Vector3 parentSpaceAnchor,
            Vector3 parentSpaceXAxis,
            Vector3 parentSpaceYAxis,
            Vector3 parentSpaceZAxis,
            Vector3 angleLimits,
            JointAxisController xAxisController,
            JointAxisController yAxisController,
            JointAxisController zAxisController
        )
        {
            ParentLimb = parentLimb ?? throw new ArgumentNullException(nameof(parentLimb), "Parent limb cannot be null.");
            ChildLimb = childLimb ?? throw new ArgumentNullException(nameof(childLimb), "Child limb cannot be null.");

            ParentSpaceAnchor = parentSpaceAnchor;
            ParentSpaceXAxis = parentSpaceXAxis;
            ParentSpaceYAxis = parentSpaceYAxis;
            ParentSpaceZAxis = parentSpaceZAxis;

            AngleLimits = angleLimits;

            DesiredAngles = Vector3.Zero;
            ActualAngles = Vector3.Zero;

            XAxisController = xAxisController;
            YAxisController = yAxisController;
            ZAxisController = zAxisController;

            if (xAxisController?.Sensor != null) sensors.Add(xAxisController.Sensor);
            if (yAxisController?.Sensor != null) sensors.Add(yAxisController.Sensor);
            if (zAxisController?.Sensor != null) sensors.Add(zAxisController.Sensor);

            if (xAxisController?.Actuator != null) actuators.Add(xAxisController.Actuator);
            if (yAxisController?.Actuator != null) actuators.Add(yAxisController.Actuator);
            if (zAxisController?.Actuator != null) actuators.Add(zAxisController.Actuator);
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
