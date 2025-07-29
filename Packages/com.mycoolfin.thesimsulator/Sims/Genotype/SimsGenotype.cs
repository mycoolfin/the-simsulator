using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    using Core.Genotype;

    public class SimsGenotype : IGenotype<SimsGenotype>
    {
        public const int MIN_NODES = 1;
        public const int MAX_NODES = 10;
        public const int MIN_BRAIN_NEURON_DEFINITIONS = 0;
        public const int MAX_BRAIN_NEURON_DEFINITIONS = 10;

        public const ulong BRAIN_GID = 0UL;

        public string Gid { get; set; }
        public List<Node> Nodes { get; set; }
        public List<Connection> Connections { get; set; }
        public List<NeuronDefinition> NeuronDefinitions { get; set; }

        public SimsGenotype(List<Node> nodes, List<Connection> connections, List<NeuronDefinition> neuronDefinitions)
        {
            Gid = SharedRandom.NextUInt64().ToString();
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes), "Nodes cannot be null.");
            Connections = connections ?? throw new ArgumentNullException(nameof(connections), "Connections cannot be null.");
            NeuronDefinitions = neuronDefinitions ?? throw new ArgumentNullException(nameof(neuronDefinitions), "NeuronDefinitions cannot be null.");

            if (Nodes.Count < MIN_NODES || Nodes.Count > MAX_NODES)
                throw new ArgumentOutOfRangeException(nameof(nodes), $"Node count must be between {MIN_NODES} and {MAX_NODES} (Got {Nodes.Count}).");

            int totalMinConnections = Nodes.Count * Node.MIN_CONNECTIONS;
            int totalMaxConnections = Nodes.Count * Node.MAX_CONNECTIONS;
            if (Connections.Count < totalMinConnections || Connections.Count > totalMaxConnections)
                throw new ArgumentOutOfRangeException(nameof(connections), $"Connection count must be between {totalMinConnections} and {totalMaxConnections} (Got {Connections.Count}).");

            int totalMinNeuronDefinitions = Nodes.Count * Node.MIN_NEURON_DEFINITIONS + MIN_BRAIN_NEURON_DEFINITIONS;
            int totalMaxNeuronDefinitions = nodes.Count * Node.MAX_NEURON_DEFINITIONS + MAX_BRAIN_NEURON_DEFINITIONS;
            if (NeuronDefinitions.Count < totalMinNeuronDefinitions || NeuronDefinitions.Count > totalMaxNeuronDefinitions)
                throw new ArgumentOutOfRangeException(nameof(neuronDefinitions), $"NeuronDefinition count must be between {totalMinNeuronDefinitions} and {totalMaxNeuronDefinitions} (Got {NeuronDefinitions.Count}).");
        }
    }
}
