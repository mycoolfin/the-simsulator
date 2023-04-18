using System;
using System.Linq;
using System.Collections.Generic;

public class EmitterAvailabilityMap
{
    public readonly int sameLimbEmitterCount;
    public readonly int brainEmitterCount;
    public readonly int parentLimbEmitterCount;
    public readonly List<int> childLimbEmitterCounts;

    // Refers to specific instances instead of relative positions.
    public readonly Dictionary<string, int> limbInstanceEmitterCounts;

    private EmitterAvailabilityMap(
        int sameLimbEmitterCount,
        int brainEmitterCount,
        int parentLimbEmitterCount,
        List<int> childLimbEmitterCounts,
        Dictionary<string, int> limbInstanceEmitterCounts
    )
    {
        this.sameLimbEmitterCount = sameLimbEmitterCount;
        this.brainEmitterCount = brainEmitterCount;
        this.parentLimbEmitterCount = parentLimbEmitterCount;
        this.childLimbEmitterCounts = childLimbEmitterCounts ?? new();
        this.limbInstanceEmitterCounts = limbInstanceEmitterCounts ?? new();
    }

    public List<EmitterSetLocation> GetValidInputSetLocations()
    {
        // Returns a list of input set locations with at least one available input.
        List<EmitterSetLocation> validLocations = new() { EmitterSetLocation.None };
        if (sameLimbEmitterCount > 0) validLocations.Add(EmitterSetLocation.SameLimb);
        if (brainEmitterCount > 0) validLocations.Add(EmitterSetLocation.Brain);
        if (parentLimbEmitterCount > 0) validLocations.Add(EmitterSetLocation.ParentLimb);
        if (childLimbEmitterCounts.Sum() > 0) validLocations.Add(EmitterSetLocation.ChildLimbs);
        if (limbInstanceEmitterCounts.Sum(x => x.Value) > 0) validLocations.Add(EmitterSetLocation.LimbInstances);
        return validLocations;
    }

    public List<int> GetValidChildLimbIndices()
    {
        return childLimbEmitterCounts.Select((count, i) => (count, i)).Where(x => x.Item1 > 0).Select(x => x.Item2).ToList();
    }

    public List<string> GetValidLimbInstanceIds()
    {
        return limbInstanceEmitterCounts.Where(x => x.Value > 0).Select(x => x.Key).ToList();
    }

    public int GetInputCountAtLocation(EmitterSetLocation emitterSetLocation, int childLimbIndex, string instanceId)
    {
        switch (emitterSetLocation)
        {
            case EmitterSetLocation.SameLimb:
                return sameLimbEmitterCount;
            case EmitterSetLocation.Brain:
                return brainEmitterCount;
            case EmitterSetLocation.ParentLimb:
                return parentLimbEmitterCount;
            case EmitterSetLocation.ChildLimbs:
                return (childLimbIndex < 0 || childLimbIndex >= childLimbEmitterCounts.Count) ? -1 : childLimbEmitterCounts[childLimbIndex];
            case EmitterSetLocation.LimbInstances:
                return !limbInstanceEmitterCounts.ContainsKey(instanceId) ? -1 : limbInstanceEmitterCounts[instanceId];
            default:
                throw new ArgumentException("Invalid operation.");
        }
    }

    public static EmitterAvailabilityMap GenerateMapForBrain(int brainNeuronEmitterCount, IList<InstancedLimbNode> instancedLimbNodes)
    {
        // Brain neurons can connect to any output in itself or any specific instance of a limb.
        Dictionary<string, int> limbInstanceCounts = instancedLimbNodes.ToDictionary(i => i.InstanceId, i => i.SignalEmitterCount);
        return new(
            0,
            brainNeuronEmitterCount,
            0,
            null,
            limbInstanceCounts
        );
    }

    public static EmitterAvailabilityMap GenerateMapForLimbNode(int brainNeuronEmitterCount, IList<ILimbNodeEssentialInfo> limbNodes, int nodeId)
    {
        ILimbNodeEssentialInfo limbNode = limbNodes[nodeId];

        // If there are multiple parents, get the smallest count.
        List<ILimbNodeEssentialInfo> parentLimbs = limbNodes.Where(node => node.Connections.Count > 0 && node.Connections.Any(c => c.ChildNodeId == nodeId)).ToList();
        int parentLimbEmitterCount = parentLimbs.Count > 0 ? parentLimbs.Min(parentNode => parentNode.SignalEmitterCount) : 0;

        List<int> childLimbEmitterCounts = new();
        foreach (LimbConnection connection in limbNode.Connections)
        {
            int potentialInstances = (int)Math.Pow(2, (connection.ReflectionX ? 1 : 0) + (connection.ReflectionY ? 1 : 0) + (connection.ReflectionZ ? 1 : 0));
            for (int i = 0; i < potentialInstances; i++)
                childLimbEmitterCounts.Add(limbNodes[connection.ChildNodeId].SignalEmitterCount);
        }

        return new EmitterAvailabilityMap(
            limbNode.SignalEmitterCount,
            brainNeuronEmitterCount,
            parentLimbEmitterCount,
            childLimbEmitterCounts,
            null
        );
    }
}
