using System;
using System.Numerics;
using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class Joint
    {
        public readonly Genotype.JointType Type;

        public readonly Limb ParentLimb;
        public readonly Limb ChildLimb;

        public readonly Vector3 ParentSpaceAnchor;
        public readonly Vector3 ParentSpaceXAxis;
        public readonly Vector3 ParentSpaceYAxis;
        public readonly Vector3 ParentSpaceZAxis;

        public readonly Vector3 AngleLimits;

        public readonly float MinCrossSectionalArea;

        public readonly List<JointAngleSensor> Sensors;
        public readonly List<JointAngleActuator> Actuators;

        public Joint(
            Genotype.JointType type,
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

            Sensors = new(type.DegreesOfFreedom());
            Actuators = new(type.DegreesOfFreedom());
            switch (type)
            {
                case JointType.Rigid:
                    break; // No sensors or actuators for rigid joints.
                case JointType.Revolute:
                    Sensors.Add(new(Vector3.UnitX));
                    Actuators.Add(new(Vector3.UnitX));
                    break;
                case JointType.Twist:
                    Sensors.Add(new(Vector3.UnitZ));
                    Actuators.Add(new(Vector3.UnitZ));
                    break;
                case JointType.BendTwist:
                    Sensors.Add(new(Vector3.UnitX));
                    Sensors.Add(new(Vector3.UnitZ));
                    Actuators.Add(new(Vector3.UnitX));
                    Actuators.Add(new(Vector3.UnitZ));
                    break;
                case JointType.TwistBend:
                    Sensors.Add(new(Vector3.UnitZ));
                    Sensors.Add(new(Vector3.UnitX));
                    Actuators.Add(new(Vector3.UnitZ));
                    Actuators.Add(new(Vector3.UnitX));
                    break;
                case JointType.Universal:
                    Sensors.Add(new(Vector3.UnitX));
                    Sensors.Add(new(Vector3.UnitY));
                    Actuators.Add(new(Vector3.UnitX));
                    Actuators.Add(new(Vector3.UnitY));
                    break;
                case JointType.Spherical:
                    Sensors.Add(new(Vector3.UnitX));
                    Sensors.Add(new(Vector3.UnitY));
                    Sensors.Add(new(Vector3.UnitZ));
                    Actuators.Add(new(Vector3.UnitX));
                    Actuators.Add(new(Vector3.UnitY));
                    Actuators.Add(new(Vector3.UnitZ));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported joint type.");
            }
        }
    }
}
