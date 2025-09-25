using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Node
    {
        public const float MIN_DIMENSION = 0.1f;
        public const float MAX_DIMENSION = 2.0f;
        public const int MIN_RECURSIVE_LIMIT = 1;
        public const int MAX_RECURSIVE_LIMIT = 10;
        public const int MIN_NEURON_DEFINITIONS = 0;
        public const int MAX_NEURON_DEFINITIONS = 10;
        public const int MIN_CONNECTIONS = 0;
        public const int MAX_CONNECTIONS = 4;

        public ulong Gid { get; set; }
        public Vector3 Dimensions { get; set; }
        public JointDefinition JointDefinition { get; set; }
        public int RecursiveLimit { get; set; }
        public NodeColor Color { get; set; }

        private readonly int ContactSensorCount => 5; // Total load, slip, and one for each axis.
        private readonly int LightSensorCount => 3; // One for each axis.
        private readonly int JointSensorCount => JointDefinition.JointType.DegreesOfFreedom();
        public readonly int SensorCount => ContactSensorCount + LightSensorCount + JointSensorCount;

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

        public readonly Node CopyWithNewGid()
        {
            return new Node(Dimensions, JointDefinition, RecursiveLimit, Color);
        }

        public readonly Node CopyWithSameGid(JointDefinition newJointDefinition)
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
