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

            sensors = new() { new JointAngleSensor(JointAxis.Primary), new JointAngleSensor(JointAxis.Secondary), new JointAngleSensor(JointAxis.Tertiary) };
            actuators = new() { new JointAngleActuator(JointAxis.Primary), new JointAngleActuator(JointAxis.Secondary), new JointAngleActuator(JointAxis.Tertiary) };
        }
    }
}
