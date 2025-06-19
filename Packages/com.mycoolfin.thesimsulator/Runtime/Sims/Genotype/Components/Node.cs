using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    [StructLayout(LayoutKind.Explicit, Size = 128)]
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
        [FieldOffset(116)] public readonly int RecursiveLimit;
        [FieldOffset(120)] public readonly double _pad;

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

        private Node(ulong gid, Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit)
        {
            Gid = gid;
            Dimensions = dimensions;
            JointDefinition = jointDefinition;
            RecursiveLimit = recursiveLimit;
            _pad = 0.0;
        }

        public Node CopyWithNewGid()
        {
            return new Node(Dimensions, JointDefinition, RecursiveLimit);
        }

        public Node CopyWithSameGid(JointDefinition newJointDefinition)
        {
            return new Node(Gid, Dimensions, newJointDefinition, RecursiveLimit);
        }

        public static Node CreateRandom(IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            Vector3 randomDimensions = new(
                (float)SharedRandom.NextDouble() * (MaxDimensions.X - MinDimensions.X) + MinDimensions.X,
                (float)SharedRandom.NextDouble() * (MaxDimensions.Y - MinDimensions.Y) + MinDimensions.Y,
                (float)SharedRandom.NextDouble() * (MaxDimensions.Z - MinDimensions.Z) + MinDimensions.Z
            );
            int randomRecursiveLimit = SharedRandom.Next(MinRecursiveLimit, MaxRecursiveLimit + 1);

            Node randomNode = new(randomDimensions, new(), randomRecursiveLimit);
            JointDefinition randomJointDefinition = JointDefinition.CreateRandom(randomNode.Gid, nodes, connections, neuronDefinitions);
            return randomNode.CopyWithSameGid(randomJointDefinition);
        }
    }
}
