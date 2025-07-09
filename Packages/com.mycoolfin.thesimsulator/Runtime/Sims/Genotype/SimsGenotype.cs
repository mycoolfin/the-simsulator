using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public class SimsGenotype : IGenotype<SimsGenotype>
    {
        public const int MIN_NODES = 1;
        public const int MAX_NODES = 10;
        public const int MIN_CONNECTIONS = 1;
        public const int MAX_CONNECTIONS = 10;
        public const int MIN_NEURON_DEFINITIONS = 0;
        public const int MAX_NEURON_DEFINITIONS = 500;

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

            if (Nodes.Count < MIN_NODES || Nodes.Count > MAX_NODES)
                throw new ArgumentOutOfRangeException(nameof(nodes), $"Node count must be between {MIN_NODES} and {MAX_NODES}.");
            if (Connections.Count < MIN_CONNECTIONS || Connections.Count > MAX_CONNECTIONS)
                throw new ArgumentOutOfRangeException(nameof(connections), $"Connection count must be between {MIN_CONNECTIONS} and {MAX_CONNECTIONS}.");
            if (NeuronDefinitions.Count < MIN_NEURON_DEFINITIONS || NeuronDefinitions.Count > MAX_NEURON_DEFINITIONS)
                throw new ArgumentOutOfRangeException(nameof(neuronDefinitions), $"NeuronDefinition count must be between {MIN_NEURON_DEFINITIONS} and {MAX_NEURON_DEFINITIONS}.");
        }
    }
}
