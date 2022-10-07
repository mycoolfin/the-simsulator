using UnityEngine;

public class PhenotypeBuilder
{
    public static GameObject ConstructPhenotype(GraphNode rootNode)
    {
        return RecursivelyAddLimbs(rootNode, null, null, 0);
    }

    private static GameObject RecursivelyAddLimbs(GraphNode parentNode, GraphConnection connectionToParent, Limb parentLimb, int nodeRecursionCount)
    {
        GameObject phenotype = null;
        Limb limb;
        GraphNode node;

        if (connectionToParent == null) // This is the root node.
        {
            // Create a new creature.
            node = parentNode;
            phenotype = new();
            limb = Limb.CreateLimb();
            limb.transform.parent = phenotype.transform;
            limb.Dimensions = node.dimensions;
        }
        else
        {
            // Add a limb to the parent node using the supplied connection specification.
            node = connectionToParent.childNode;
            Vector3 dimensions = Vector3.Scale(node.dimensions, connectionToParent.scale);
            limb = parentLimb.AddChildLimb(
                connectionToParent.reflection ? (connectionToParent.parentFace + 3) % 6 : connectionToParent.parentFace,
                connectionToParent.position,
                connectionToParent.orientation,
                dimensions,
                node.jointType,
                node.jointLimits
            );
            if (limb == null) // Limb creation failed, ignore all downstream nodes.
                return null;
        }

        bool recursionLimitReached = nodeRecursionCount >= node.recursiveLimit;

        if (node.connections != null)
        {
            foreach (GraphConnection connection in node.connections)
            {
                if ((!recursionLimitReached && !connection.terminalOnly) || (recursionLimitReached && connection.terminalOnly) || (recursionLimitReached && node != connection.childNode))
                    RecursivelyAddLimbs(node, connection, limb, nodeRecursionCount + (connection.childNode == node ? 1 : 0));
            }
        }

        return phenotype;
    }
}
