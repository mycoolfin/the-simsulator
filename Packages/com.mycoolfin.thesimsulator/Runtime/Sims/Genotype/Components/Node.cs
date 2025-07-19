using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Node
    {
        public const float MIN_DIMENSION = 0.1f;
        public const float MAX_DIMENSION = 2.0f;
        public const int MIN_RECURSIVE_LIMIT = 1;
        public const int MAX_RECURSIVE_LIMIT = 10;
        public const int MIN_NEURON_DEFINITIONS = 0;
        public const int MAX_NEURON_DEFINITIONS = 10;
        public const int MIN_CONNECTIONS = 0;
        public const int MAX_CONNECTIONS = 4;

        public readonly ulong Gid;
        public readonly Vector3 Dimensions;
        public readonly JointDefinition JointDefinition;
        public readonly int RecursiveLimit;
        public readonly NodeColor Color;

        public int SensorCount => JointDefinition.JointType.DegreesOfFreedom();

        public Node(Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit, NodeColor color)
        {
            Gid = SharedRandom.NextUInt64();
            Dimensions = dimensions;
            JointDefinition = jointDefinition;
            RecursiveLimit = recursiveLimit;
            Color = color;
        }

        private Node(ulong gid, Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit, NodeColor color)
        {
            Gid = gid;
            Dimensions = dimensions;
            JointDefinition = jointDefinition;
            RecursiveLimit = recursiveLimit;
            Color = color;
        }

        public Node CopyWithNewGid()
        {
            return new Node(Dimensions, JointDefinition, RecursiveLimit, Color);
        }

        public Node CopyWithSameGid(JointDefinition newJointDefinition)
        {
            return new Node(Gid, Dimensions, newJointDefinition, RecursiveLimit, Color);
        }

        public static Node CreateRandom(IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            Vector3 randomDimensions = new(RandomDimension(), RandomDimension(), RandomDimension());

            int randomRecursiveLimit = RandomRecursiveLimit();

            NodeColor randomColor = NodeColor.CreateRandom();

            Node randomNode = new(randomDimensions, new(), randomRecursiveLimit, randomColor);
            JointDefinition randomJointDefinition = JointDefinition.CreateRandom(randomNode.Gid, nodes, connections, neuronDefinitions);
            return randomNode.CopyWithSameGid(randomJointDefinition);
        }

        private static float RandomDimension() => (float)SharedRandom.NextDouble() * (MAX_DIMENSION - MIN_DIMENSION) + MIN_DIMENSION;
        private static int RandomRecursiveLimit() => SharedRandom.Next(MIN_RECURSIVE_LIMIT, MAX_RECURSIVE_LIMIT + 1);
    }
}
