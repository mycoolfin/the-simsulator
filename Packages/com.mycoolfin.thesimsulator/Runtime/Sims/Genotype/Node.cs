using System;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public readonly struct Node
    {
        public static readonly Vector3 MinDimensions = new(0.01f, 0.01f, 0.01f);
        public static readonly Vector3 MaxDimensions = new(1.0f, 1.0f, 1.0f);
        public static readonly int MinRecursiveLimit = 1;
        public static readonly int MaxRecursiveLimit = 10;

        public const int SENSOR_COUNT = 3;
        public const int ACTUATOR_COUNT = 3;

        [FieldOffset(0)] public readonly ulong Gid;
        [FieldOffset(8)] public readonly Vector3 Dimensions;
        [FieldOffset(20)] public readonly JointDefinition JointDefinition;
        [FieldOffset(52)] public readonly int RecursiveLimit;
        [FieldOffset(56)] public readonly double _pad;

        public Node(Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            SharedRandom.NextBytes(buffer);
            Gid = BitConverter.ToUInt64(buffer, 0);
            Dimensions = dimensions;
            JointDefinition = jointDefinition;
            RecursiveLimit = recursiveLimit;
            _pad = 0.0;
        }

        /// <summary>
        /// Creates a copy of this Node with a new Gid.
        /// </summary>
        public Node Copy()
        {
            return new Node(Dimensions, JointDefinition, RecursiveLimit);
        }

        public static Node CreateRandom()
        {
            Vector3 randomDimensions = new(
                (float)SharedRandom.NextDouble() * (MaxDimensions.X - MinDimensions.X) + MinDimensions.X,
                (float)SharedRandom.NextDouble() * (MaxDimensions.Y - MinDimensions.Y) + MinDimensions.Y,
                (float)SharedRandom.NextDouble() * (MaxDimensions.Z - MinDimensions.Z) + MinDimensions.Z
            );
            JointDefinition randomJointDefinition = JointDefinition.CreateRandom();
            int randomRecursiveLimit = SharedRandom.Next(MinRecursiveLimit, MaxRecursiveLimit + 1);

            return new Node(randomDimensions, randomJointDefinition, randomRecursiveLimit);
        }
    }
}
