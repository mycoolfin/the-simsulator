using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public enum JointType : byte
    {
        Rigid,
        Revolute,
        Twist,
        BendTwist,
        TwistBend,
        Universal,
        Spherical
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct JointDefinition
    {
        public const float MIN_ANCHOR_ON_PARENT_FACE = -1.0f;
        public const float MAX_ANCHOR_ON_PARENT_FACE = 1.0f;
        public const float MIN_ANGLE_LIMIT = (float)-Math.PI / 2;
        public const float MAX_ANGLE_LIMIT = (float)Math.PI / 2;

        public JointType JointType { get; set; }
        public Vector3 AngleLimits { get; set; } // Radians.
        public InputSetDefinition PrimaryAxisInputs { get; set; }
        public InputSetDefinition SecondaryAxisInputs { get; set; }
        public InputSetDefinition TertiaryAxisInputs { get; set; }

        public static readonly JointType[] AllJointTypes = (JointType[])Enum.GetValues(typeof(JointType));

        public JointDefinition(JointType jointType, Vector3 angleLimits, InputSetDefinition primaryAxisInputs, InputSetDefinition secondaryAxisInputs, InputSetDefinition tertiaryAxisInputs)
        {
            JointType = jointType;
            AngleLimits = angleLimits;
            PrimaryAxisInputs = primaryAxisInputs;
            SecondaryAxisInputs = secondaryAxisInputs;
            TertiaryAxisInputs = tertiaryAxisInputs;
        }

        public static JointDefinition CreateRandom(ulong containerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            JointType randomJointType = AllJointTypes[SharedRandom.Next(AllJointTypes.Length)];

            Vector3 randomAngleLimits = new(RandomAngleLimit(), RandomAngleLimit(), RandomAngleLimit());

            return new JointDefinition(
                randomJointType,
                randomAngleLimits,
                InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions),
                InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions),
                InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions)
            );
        }

        public static JointDefinition RandomiseSignalInputs(ulong containerId, JointDefinition jointDefinition, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            return new JointDefinition(
                jointDefinition.JointType,
                jointDefinition.AngleLimits,
                InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions),
                InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions),
                InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions)
            );
        }

        private static float RandomAngleLimit() => (float)SharedRandom.NextDouble() * (MAX_ANGLE_LIMIT - MIN_ANGLE_LIMIT) + MIN_ANGLE_LIMIT;
    }
}
