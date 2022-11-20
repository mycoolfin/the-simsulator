using UnityEngine;

public static class PhenotypeBuilder
{
    public static Phenotype ConstructPhenotype(Genotype genotype)
    {
        // Create brain.
        Brain brain = new Brain(genotype.brainNeuronDefinitions);

        // Create limbs.
        GameObject limbContainer = RecursivelyAddLimbs(genotype.limbNodes, null, null, 0);
        Limb[] limbs = limbContainer.GetComponentsInChildren<Limb>();

        // Wire up nervous system.
        NervousSystem.Configure(brain, limbs);

        Phenotype phenotype = limbContainer.AddComponent<Phenotype>();
        phenotype.brain = brain;
        phenotype.limbs = limbs;

        return phenotype;
    }

    private static GameObject RecursivelyAddLimbs(LimbNode[] limbNodes, LimbConnection? connectionToParent, Limb parentLimb, int nodeRecursionCount)
    {
        GameObject limbContainer = null;

        int nodeId = connectionToParent == null ? 0 : ((LimbConnection)connectionToParent).childNodeId;
        LimbNode node = limbNodes[nodeId];

        Limb limb;
        if (connectionToParent == null) // Create a new creature.
        {
            limbContainer = new("Creature " + Random.Range(1, 1000));
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
