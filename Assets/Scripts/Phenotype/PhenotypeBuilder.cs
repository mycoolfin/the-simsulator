using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public static class PhenotypeBuilder
{
    public static Phenotype ConstructPhenotype(Genotype genotype)
    {
        // Create brain.
        Brain brain = new Brain(genotype.brainNeuronDefinitions);

        // Create limbs.
        GameObject limbContainer = RecursivelyAddLimbs(genotype.limbNodes, null, null, 0);
        List<Limb> limbs = limbContainer.GetComponentsInChildren<Limb>().ToList();

        // Wire up nervous system.
        NervousSystem.Configure(brain, limbs);

        Phenotype phenotype = limbContainer.AddComponent<Phenotype>();
        phenotype.genotype = genotype;
        phenotype.brain = brain;
        phenotype.limbs = limbs;

        phenotype.gameObject.name = "P of G" + genotype.id;

        return phenotype;
    }

    private static GameObject RecursivelyAddLimbs(ReadOnlyCollection<LimbNode> limbNodes, LimbConnection? connectionToParent, Limb parentLimb, int nodeRecursionCount)
    {
        GameObject limbContainer = null;

        int nodeId = connectionToParent == null ? 0 : ((LimbConnection)connectionToParent).childNodeId;
        LimbNode node = limbNodes[nodeId];

        Limb limb;
        if (connectionToParent == null) // Create a new creature.
        {
            limbContainer = new();
            limb = Limb.CreateLimb(node);
            limb.name = "Limb 1";
            limb.transform.parent = limbContainer.transform;
        }
        else
        {
            int numberOfLimbs = parentLimb.transform.parent.GetComponentsInChildren<Limb>().Length;
            if (numberOfLimbs >= PhenotypeBuilderParameters.MaxLimbs)
                return null;

            // Add a limb to the parent node using the supplied connection specification.
            limb = parentLimb.AddChildLimb(node, ((LimbConnection)connectionToParent));
            if (limb == null) // Limb creation failed, ignore all downstream nodes.
                return null;
            limb.name = "Limb " + (numberOfLimbs + 1);
        }

        bool recursionLimitReached = nodeRecursionCount >= node.recursiveLimit;

        foreach (LimbConnection connection in node.connections)
        {
            bool nextNodeIsThisNode = nodeId == connection.childNodeId;
            if ((!recursionLimitReached && !connection.terminalOnly) || (recursionLimitReached && connection.terminalOnly) || (recursionLimitReached && !nextNodeIsThisNode))
            {
                RecursivelyAddLimbs(limbNodes, connection, limb, nodeRecursionCount + (nextNodeIsThisNode ? 1 : 0));
            }
        }

        return limbContainer;
    }
}
