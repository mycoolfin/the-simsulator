using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

public class InstancedLimbNode : ILimbNodeEssentialInfo
{
    [SerializeField] private string instanceId;
    [SerializeField] private LimbNode limbNode;
    [SerializeField] private LimbConnection connectionToParent;
    [SerializeField] private bool reflectedX;
    [SerializeField] private bool reflectedY;
    [SerializeField] private bool reflectedZ;
    [SerializeField] private List<InstancedLimbNode> childLimbNodeInstances;

    public string InstanceId => instanceId;
    public LimbNode LimbNode => limbNode;
    public LimbConnection ConnectionToParent => connectionToParent;
    public bool ReflectedX => reflectedX;
    public bool ReflectedY => reflectedY;
    public bool ReflectedZ => reflectedZ;
    public ReadOnlyCollection<InstancedLimbNode> ChildLimbNodeInstances => childLimbNodeInstances.AsReadOnly();

    public int RecursiveLimit => limbNode.RecursiveLimit;
    public ReadOnlyCollection<LimbConnection> Connections => limbNode.Connections;
    public int SignalEmitterCount => connectionToParent == null ? limbNode.NeuronDefinitions.Count : limbNode.SignalEmitterCount;

    private InstancedLimbNode(string instanceId, LimbNode limbNode, LimbConnection connectionToParent, bool reflectedX, bool reflectedY, bool reflectedZ)
    {
        this.instanceId = instanceId;
        this.limbNode = limbNode;
        this.connectionToParent = connectionToParent;
        this.reflectedX = reflectedX;
        this.reflectedY = reflectedY;
        this.reflectedZ = reflectedZ;
        this.childLimbNodeInstances = new List<InstancedLimbNode>();
    }

    private InstancedLimbNode CreateChild(LimbNode childLimbNode, LimbConnection connectionToParent, bool reflectX, bool reflectY, bool reflectZ)
    {
        string childInstanceId = instanceId + "-" + childLimbNodeInstances.Count.ToString();
        InstancedLimbNode childInstance = new(
            childInstanceId,
            childLimbNode,
            connectionToParent,
            reflectedX ^ reflectX, // Existing reflection is cancelled out by incoming reflection.
            reflectedY ^ reflectY,
            reflectedZ ^ reflectZ
        );

        childLimbNodeInstances.Add(childInstance);

        return childInstance;
    }

    private static List<InstancedLimbNode> RecursivelyDeriveInstancedLimbNodes(List<InstancedLimbNode> instanceList, IList<LimbNode> limbNodes,
        InstancedLimbNode parentNodeInstance, LimbConnection connectionToParent, int nodeRecursionCount)
    {
        if (instanceList == null)
            instanceList = new();

        int nodeId = connectionToParent == null ? 0 : connectionToParent.ChildNodeId;
        LimbNode node = limbNodes[nodeId];

        List<InstancedLimbNode> newInstances = new();
        if (connectionToParent == null)
        {
            parentNodeInstance = new InstancedLimbNode("0", node, null, false, false, false);
            newInstances.Add(parentNodeInstance);
        }
        else
        {
            int numberOfLimbs = instanceList.Count;

            bool canBeginReflectedSubtrees = nodeRecursionCount == 0 || (nodeRecursionCount == 1 && numberOfLimbs == 1);
            bool beginReflectedX = canBeginReflectedSubtrees && connectionToParent.ReflectionX;
            bool beginReflectedY = canBeginReflectedSubtrees && connectionToParent.ReflectionY;
            bool beginReflectedZ = canBeginReflectedSubtrees && connectionToParent.ReflectionZ;

            int limbsToAdd = (int)Mathf.Pow(2, (beginReflectedX ? 1 : 0) + (beginReflectedY ? 1 : 0) + (beginReflectedZ ? 1 : 0));
            bool canAddMoreLimbs = (numberOfLimbs + limbsToAdd) <= PhenotypeBuilderParameters.MaxLimbs;

            bool recursing = node == parentNodeInstance.limbNode;
            bool recursionLimitReached = nodeRecursionCount >= node.RecursiveLimit;
            bool connectionCriteriaMet = (!recursionLimitReached && !connectionToParent.TerminalOnly) || (recursionLimitReached && connectionToParent.TerminalOnly) || (recursionLimitReached && !recursing);

            if (!canAddMoreLimbs)
                return null;

            if (connectionCriteriaMet)
            {
                newInstances.Add(parentNodeInstance.CreateChild(node, connectionToParent, false, false, false));

                if (canBeginReflectedSubtrees)
                {
                    List<bool> reflections = new List<bool> { beginReflectedX, beginReflectedY, beginReflectedZ };
                    for (int reflectIndex = 0; reflectIndex < reflections.Count; reflectIndex++)
                    {
                        if (reflections[reflectIndex])
                        {
                            int newLimbCount = newInstances.Count;

                            for (int limbIndex = 0; limbIndex < newLimbCount; limbIndex++)
                            {
                                bool reflectX = reflectIndex == 0 || limbIndex % 2 != 0;
                                bool reflectY = reflectIndex == 1 || limbIndex > 1;
                                bool reflectZ = reflectIndex == 2;
                                newInstances.Add(
                                    parentNodeInstance.CreateChild(
                                    node,
                                    connectionToParent,
                                    reflectX,
                                    reflectY,
                                    reflectZ
                                ));
                            }
                        }
                    }
                }
            }

            int missingChildren = 0;

            if (!connectionCriteriaMet)
                missingChildren += 1;

            if (!connectionCriteriaMet || !canBeginReflectedSubtrees)
            {
                int unusedReflections = (int)Math.Pow(2,
                    (connectionToParent.ReflectionX ? 1 : 0) + (connectionToParent.ReflectionY ? 1 : 0) + (connectionToParent.ReflectionZ ? 1 : 0))
                    - 1;
                missingChildren += unusedReflections;
            }

            for (int i = 0; i < missingChildren; i++)
                parentNodeInstance.childLimbNodeInstances.Add(null); // Add null children to preserve child ordering.
        }
        newInstances.ForEach(l => instanceList.Add(l));

        foreach (LimbConnection connection in node.Connections)
        {
            bool nextNodeIsThisNode = nodeId == connection.ChildNodeId;
            foreach (InstancedLimbNode newInstance in newInstances)
                RecursivelyDeriveInstancedLimbNodes(instanceList, limbNodes, newInstance, connection, nodeRecursionCount + (nextNodeIsThisNode ? 1 : 0));
        }

        return instanceList;
    }

    public static List<InstancedLimbNode> DeriveInstancedLimbNodes(IList<LimbNode> limbNodes)
    {
        return RecursivelyDeriveInstancedLimbNodes(null, limbNodes, null, null, 0);
    }
}
