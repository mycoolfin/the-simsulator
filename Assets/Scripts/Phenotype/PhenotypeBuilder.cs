using UnityEngine;

public static class PhenotypeBuilder
{
    public static Phenotype ConstructPhenotype(Genotype genotype)
    {
        // Construct limbs.
        GameObject limbContainer = RecursivelyAddLimbs(genotype.limbNodes[0], null, null, 0);
        Limb[] limbs = limbContainer.GetComponentsInChildren<Limb>();

        // Wire up nervous system.
        Brain brain = new Brain(genotype.brainNeuronNodes, limbs);
        foreach (Limb limb in limbs)
            limb.ConfigureLimbNervousSystem(brain);

        Phenotype phenotype = limbContainer.AddComponent<Phenotype>();
        phenotype.brain = brain;
        phenotype.limbs = limbs;

        return phenotype;
    }

    private static GameObject RecursivelyAddLimbs(LimbNode parentNode, LimbConnection connectionToParent, Limb parentLimb, int nodeRecursionCount)
    {
        GameObject limbContainer = null;
        Limb limb;
        LimbNode node;

        if (connectionToParent == null) // This is the root node.
        {
            // Create a new creature.
            node = parentNode;
            limbContainer = new("Creature");
            limb = Limb.CreateLimb(node);
            limb.transform.parent = limbContainer.transform;
            limb.Dimensions = node.dimensions;
        }
        else
        {
            // Add a limb to the parent node using the supplied connection specification.
            node = connectionToParent.childNode;
            limb = parentLimb.AddChildLimb(node, connectionToParent);
            if (limb == null) // Limb creation failed, ignore all downstream nodes.
                return null;
        }

        bool recursionLimitReached = nodeRecursionCount >= node.recursiveLimit;

        foreach (LimbConnection connection in node.connections)
        {
            if ((!recursionLimitReached && !connection.terminalOnly) || (recursionLimitReached && connection.terminalOnly) || (recursionLimitReached && node != connection.childNode))
                RecursivelyAddLimbs(node, connection, limb, nodeRecursionCount + (connection.childNode == node ? 1 : 0));
        }

        return limbContainer;
    }
}
