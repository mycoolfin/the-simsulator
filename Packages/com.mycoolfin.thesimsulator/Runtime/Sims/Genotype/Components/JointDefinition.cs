using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    public enum JointType : int
    {
        Rigid,
        Revolute,
        Twist,
        Universal,
        BendTwist,
        TwistBend,
        Spherical
    }

    [StructLayout(LayoutKind.Explicit, Size = 96)]
    public readonly struct JointDefinition
    {
        [FieldOffset(0)] public readonly JointType JointType;
        [FieldOffset(4)] public readonly Vector2 AnchorOnParentFace;
        [FieldOffset(12)] public readonly Vector3 AngleLimits;
        [FieldOffset(24)] public readonly InputSetDefinition XAxisInputs;
        [FieldOffset(48)] public readonly InputSetDefinition YAxisInputs;
        [FieldOffset(72)] public readonly InputSetDefinition ZAxisInputs;

        public JointDefinition(JointType jointType, Vector2 anchorOnParentFace, Vector3 angleLimits, InputSetDefinition xAxisInputs, InputSetDefinition yAxisInputs, InputSetDefinition zAxisInputs)
        {
            JointType = jointType;
            AnchorOnParentFace = anchorOnParentFace;
            AngleLimits = angleLimits;
            XAxisInputs = xAxisInputs;
            YAxisInputs = yAxisInputs;
            ZAxisInputs = zAxisInputs;
        }

        public static readonly JointType[] AllJointTypes = (JointType[])System.Enum.GetValues(typeof(JointType));
        public static readonly Vector2 MinAnchorOnParentFace = new(-1.0f, -1.0f);
        public static readonly Vector2 MaxAnchorOnParentFace = new(1.0f, 1.0f);
        public static readonly Vector3 MinAngleLimit = new(-90.0f, -90.0f, -90.0f);
        public static readonly Vector3 MaxAngleLimit = new(90.0f, 90.0f, 90.0f);

        public static JointDefinition CreateRandom(ulong containerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            JointType randomJointType = AllJointTypes[SharedRandom.Next(AllJointTypes.Length)];
            Vector2 randomAnchor = new(
                (float)SharedRandom.NextDouble() * (MaxAnchorOnParentFace.X - MinAnchorOnParentFace.X) + MinAnchorOnParentFace.X,
                (float)SharedRandom.NextDouble() * (MaxAnchorOnParentFace.Y - MinAnchorOnParentFace.Y) + MinAnchorOnParentFace.Y
            );
            Vector3 randomAngleLimits = new(
                (float)SharedRandom.NextDouble() * (MaxAngleLimit.X - MinAngleLimit.X) + MinAngleLimit.X,
                (float)SharedRandom.NextDouble() * (MaxAngleLimit.Y - MinAngleLimit.Y) + MinAngleLimit.Y,
                (float)SharedRandom.NextDouble() * (MaxAngleLimit.Z - MinAngleLimit.Z) + MinAngleLimit.Z
            );

            InputSetDefinition randomInputsX = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);
            InputSetDefinition randomInputsY = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);
            InputSetDefinition randomInputsZ = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);

            return new JointDefinition(randomJointType, randomAnchor, randomAngleLimits, randomInputsX, randomInputsY, randomInputsZ);
        }

        public static JointDefinition RandomiseSignalInputs(ulong containerId, JointDefinition jointDefinition, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            InputSetDefinition randomInputsX = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);
            InputSetDefinition randomInputsY = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);
            InputSetDefinition randomInputsZ = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);

            return new JointDefinition(
                jointDefinition.JointType,
                jointDefinition.AnchorOnParentFace,
                jointDefinition.AngleLimits,
                randomInputsX,
                randomInputsY,
                randomInputsZ
            );
        }
    }
}
