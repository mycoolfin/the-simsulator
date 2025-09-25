using System;
using System.Numerics;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    using Genotype;

    public class Joint : ISensorContainer, IActuatorContainer
    {
        public readonly JointType Type;

        public readonly Limb ParentLimb;
        public readonly Limb ChildLimb;

        public readonly Vector3 ParentSpaceAnchor;
        public readonly Vector3 ParentSpaceXAxis;
        public readonly Vector3 ParentSpaceYAxis;
        public readonly Vector3 ParentSpaceZAxis;

        public readonly Vector3 AngleLimits;

        public readonly float MinCrossSectionalArea;

        private readonly List<Sensor> sensors;
        public IEnumerable<Sensor> Sensors => sensors;
        private readonly List<Actuator> actuators;
        public IEnumerable<Actuator> Actuators => actuators;

        public Joint(
            JointType type,
            Limb parentLimb,
            Limb childLimb,
            Vector3 parentSpaceAnchor,
            Vector3 parentSpaceXAxis,
            Vector3 parentSpaceYAxis,
            Vector3 parentSpaceZAxis,
            Vector3 angleLimits,
            float minCrossSectionalArea
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

            MinCrossSectionalArea = minCrossSectionalArea;

            sensors = new(type.DegreesOfFreedom());
            actuators = new(type.DegreesOfFreedom());
            switch (type)
            {
                case JointType.Rigid:
                    break; // No sensors or actuators for rigid joints.
                case JointType.Revolute:
                    sensors.Add(new JointAngleSensor(Vector3.UnitX));
                    actuators.Add(new JointAngleActuator(Vector3.UnitX));
                    break;
                case JointType.Twist:
                    sensors.Add(new JointAngleSensor(Vector3.UnitZ));
                    actuators.Add(new JointAngleActuator(Vector3.UnitZ));
                    break;
                case JointType.BendTwist:
                    sensors.Add(new JointAngleSensor(Vector3.UnitX));
                    sensors.Add(new JointAngleSensor(Vector3.UnitZ));
                    actuators.Add(new JointAngleActuator(Vector3.UnitX));
                    actuators.Add(new JointAngleActuator(Vector3.UnitZ));
                    break;
                case JointType.TwistBend:
                    sensors.Add(new JointAngleSensor(Vector3.UnitZ));
                    sensors.Add(new JointAngleSensor(Vector3.UnitX));
                    actuators.Add(new JointAngleActuator(Vector3.UnitZ));
                    actuators.Add(new JointAngleActuator(Vector3.UnitX));
                    break;
                case JointType.Universal:
                    sensors.Add(new JointAngleSensor(Vector3.UnitX));
                    sensors.Add(new JointAngleSensor(Vector3.UnitY));
                    actuators.Add(new JointAngleActuator(Vector3.UnitX));
                    actuators.Add(new JointAngleActuator(Vector3.UnitY));
                    break;
                case JointType.Spherical:
                    sensors.Add(new JointAngleSensor(Vector3.UnitX));
                    sensors.Add(new JointAngleSensor(Vector3.UnitY));
                    sensors.Add(new JointAngleSensor(Vector3.UnitZ));
                    actuators.Add(new JointAngleActuator(Vector3.UnitX));
                    actuators.Add(new JointAngleActuator(Vector3.UnitY));
                    actuators.Add(new JointAngleActuator(Vector3.UnitZ));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported joint type.");
            }
        }
    }
}
