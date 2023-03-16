using System;
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
        limbs.ForEach(limb => limb.Optimise());

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

        List<Limb> newLimbs = new List<Limb>();
        if (connectionToParent == null) // Create a new creature.
        {
            limbContainer = new();
            Limb limb = Limb.CreateLimb("Limb 1", node);
            limb.transform.parent = limbContainer.transform;
            newLimbs.Add(limb);
        }
        else
        {
            LimbConnection connection = (LimbConnection)connectionToParent;
            int numberOfLimbs = parentLimb.transform.parent.GetComponentsInChildren<Limb>().Length;

            bool canBeginReflectedSubtrees = nodeRecursionCount == 0 || nodeRecursionCount == 1 && numberOfLimbs == 1;
            bool beginReflectedX = canBeginReflectedSubtrees && connection.reflectionX;
            bool beginReflectedY = canBeginReflectedSubtrees && connection.reflectionY;
            bool beginReflectedZ = canBeginReflectedSubtrees && connection.reflectionZ;

            int reflectedLimbsToAdd = (int)Mathf.Pow(2, Convert.ToInt32(beginReflectedX) + Convert.ToInt32(beginReflectedY) + Convert.ToInt32(beginReflectedZ));
            if (numberOfLimbs + 1 + reflectedLimbsToAdd > PhenotypeBuilderParameters.MaxLimbs)
                return null;

            // Add a limb to the parent node using the supplied connection specification.
            Limb limb = parentLimb.AddChildLimb(
                "Limb " + (numberOfLimbs + 1),
                node,
                connection.parentFace,
                connection.position,
                connection.orientation,
                connection.scale,
                false, false, false
            );
            if (limb == null)
                return null; // Limb creation failed, ignore all downstream nodes.
            else
                newLimbs.Add(limb);

            // Begin reflected subtrees.
            List<bool> reflections = new List<bool> { beginReflectedX, beginReflectedY, beginReflectedZ };
            for (int reflectIndex = 0; reflectIndex < reflections.Count; reflectIndex++)
            {
                if (!reflections[reflectIndex])
                    continue;

                int newLimbCount = newLimbs.Count;
                for (int limbIndex = 0; limbIndex < newLimbCount; limbIndex++)
                {
                    bool reflectX = reflectIndex == 0 || limbIndex % 2 != 0;
                    bool reflectY = reflectIndex == 1 || limbIndex > 1;
                    bool reflectZ = reflectIndex == 2;
                    int limbId = numberOfLimbs + newLimbCount + limbIndex + 1;
                    string reflectionString = " (Limb " + (numberOfLimbs + 1) + " reflected " + (reflectX ? "X" : "") + (reflectY ? "Y" : "") + (reflectZ ? "Z" : "") + ")";
                    Limb reflectedLimb = parentLimb.AddChildLimb(
                        "Limb " + limbId + reflectionString,
                        node,
                        connection.parentFace,
                        connection.position,
                        connection.orientation,
                        connection.scale,
                        reflectX,
                        reflectY,
                        reflectZ
                    );
                    if (reflectedLimb == null)
                        return null; // Limb creation failed, ignore all downstream nodes.
                    else
                        newLimbs.Add(reflectedLimb);
                }
            }
        }

        bool recursionLimitReached = nodeRecursionCount >= node.recursiveLimit;

        foreach (LimbConnection connection in node.connections)
        {
            bool nextNodeIsThisNode = nodeId == connection.childNodeId;
            if ((!recursionLimitReached && !connection.terminalOnly) || (recursionLimitReached && connection.terminalOnly) || (recursionLimitReached && !nextNodeIsThisNode))
                foreach (Limb newLimb in newLimbs)
                    RecursivelyAddLimbs(limbNodes, connection, newLimb, nodeRecursionCount + (nextNodeIsThisNode ? 1 : 0));
        }

        return limbContainer;
    }
}
