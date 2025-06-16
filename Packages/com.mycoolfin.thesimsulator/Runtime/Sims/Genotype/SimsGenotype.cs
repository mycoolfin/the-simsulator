using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsGenotype : IGenotype<SimsGenotype>
    {
        public static readonly int MinNodeCount = 1;
        public static readonly int MaxNodeCount = 10;
        public static readonly int MinConnectionCount = 1;
        public static readonly int MaxConnectionCount = 10;
        public static readonly int MinNeuronDefinitionCount = 0;
        public static readonly int MaxNeuronDefinitionCount = 100;

        public const ulong BRAIN_GID = 0UL;

        public readonly string Gid = Guid.NewGuid().ToString();
        public readonly List<Node> Nodes;
        public readonly List<Connection> Connections;
        public readonly List<NeuronDefinition> NeuronDefinitions;

        public SimsGenotype(List<Node> nodes, List<Connection> connections, List<NeuronDefinition> neuronDefinitions)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes), "Nodes cannot be null.");
            Connections = connections ?? throw new ArgumentNullException(nameof(connections), "Connections cannot be null.");
            NeuronDefinitions = neuronDefinitions ?? throw new ArgumentNullException(nameof(neuronDefinitions), "NeuronDefinitions cannot be null.");

            if (Nodes.Count < MinNodeCount || Nodes.Count > MaxNodeCount)
                throw new ArgumentOutOfRangeException(nameof(nodes), $"Node count must be between {MinNodeCount} and {MaxNodeCount}.");
            if (Connections.Count < MinConnectionCount || Connections.Count > MaxConnectionCount)
                throw new ArgumentOutOfRangeException(nameof(connections), $"Connection count must be between {MinConnectionCount} and {MaxConnectionCount}.");
            if (NeuronDefinitions.Count < MinNeuronDefinitionCount || NeuronDefinitions.Count > MaxNeuronDefinitionCount)
                throw new ArgumentOutOfRangeException(nameof(neuronDefinitions), $"NeuronDefinition count must be between {MinNeuronDefinitionCount} and {MaxNeuronDefinitionCount}.");
        }
    }
}
