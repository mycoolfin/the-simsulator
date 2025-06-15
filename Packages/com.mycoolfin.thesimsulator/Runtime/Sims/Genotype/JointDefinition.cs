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

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public readonly struct JointDefinition
    {
        [FieldOffset(0)] public readonly JointType JointType;
        [FieldOffset(4)] public readonly Vector2 AnchorOnParentFace;
        [FieldOffset(12)] public readonly Vector3 AngleLimits;
        [FieldOffset(24)] private readonly double _pad;

        public JointDefinition(JointType jointType, Vector2 anchorOnParentFace, Vector3 angleLimits)
        {
            JointType = jointType;
            AnchorOnParentFace = anchorOnParentFace;
            AngleLimits = angleLimits;
            _pad = 0;
        }

        public static readonly JointType[] AllJointTypes = (JointType[])System.Enum.GetValues(typeof(JointType));
        public static readonly Vector2 MinAnchorOnParentFace = new(-1.0f, -1.0f);
        public static readonly Vector2 MaxAnchorOnParentFace = new(1.0f, 1.0f);
        public static readonly Vector3 MinAngleLimit = new(-90.0f, -90.0f, -90.0f);
        public static readonly Vector3 MaxAngleLimit = new(90.0f, 90.0f, 90.0f);

        public static JointDefinition CreateRandom()
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

            return new JointDefinition(randomJointType, randomAnchor, randomAngleLimits);
        }
    }
}
