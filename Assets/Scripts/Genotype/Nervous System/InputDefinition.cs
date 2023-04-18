using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InputDefinition
{
    [SerializeField] private EmitterSetLocation emitterSetLocation;
    [SerializeField] private int childLimbIndex;
    [SerializeField] private string instanceId;
    [SerializeField] private int emitterIndex;
    [SerializeField] private float weight;

    public EmitterSetLocation EmitterSetLocation => emitterSetLocation;
    public int ChildLimbIndex => childLimbIndex;
    public string InstanceId => instanceId;
    public int EmitterIndex => emitterIndex;
    public float Weight => weight;

    public InputDefinition(EmitterSetLocation emitterSetLocation, int childLimbIndex, string instanceId, int emitterIndex, float weight)
    {
        this.emitterSetLocation = emitterSetLocation;
        this.childLimbIndex = childLimbIndex;
        this.instanceId = instanceId;
        this.emitterIndex = emitterIndex;
        this.weight = weight;
    }

    public void Validate(EmitterAvailabilityMap map)
    {
        switch (emitterSetLocation)
        {
            case EmitterSetLocation.None:
                break;
            case EmitterSetLocation.SameLimb:
                if (emitterIndex < 0 || emitterIndex >= map.sameLimbEmitterCount)
                    throw new ArgumentException("Emitter index for same limb out of bounds. Min: 0, Max: " + (map.sameLimbEmitterCount - 1) + ", Specified: " + emitterIndex);
                break;
            case EmitterSetLocation.Brain:
                if (emitterIndex < 0 || emitterIndex >= map.brainEmitterCount)
                    throw new ArgumentException("Emitter index for brain out of bounds. Min: 0, Max: " + (map.brainEmitterCount - 1) + ", Specified: " + emitterIndex);
                break;
            case EmitterSetLocation.ParentLimb:
                if (emitterIndex < 0 || emitterIndex >= map.parentLimbEmitterCount)
                    throw new ArgumentException("Emitter index for parent limb out of bounds. Min: 0, Max: " + (map.parentLimbEmitterCount - 1) + ", Specified: " + emitterIndex);
                break;
            case EmitterSetLocation.ChildLimbs:
                if (childLimbIndex < 0 || childLimbIndex >= map.childLimbEmitterCounts.Count)
                    throw new ArgumentException("Child limb index out of bounds. Min: 0, Max: " + (map.childLimbEmitterCounts.Count - 1) + ", Specified: " + childLimbIndex);
                if (emitterIndex < 0 || emitterIndex >= map.childLimbEmitterCounts[childLimbIndex])
                    throw new ArgumentException("Emitter index for child limb " + childLimbIndex + " out of bounds. Min: 0, Max: " + (map.childLimbEmitterCounts[childLimbIndex] - 1) + ", Specified: " + emitterIndex);
                break;
            case EmitterSetLocation.LimbInstances:
                if (string.IsNullOrEmpty(instanceId))
                    throw new ArgumentException("Limb instance id is required.");
                if (emitterIndex < 0 || emitterIndex >= map.limbInstanceEmitterCounts[instanceId])
                    throw new ArgumentException("Emitter index for limb instance " + instanceId + " out of bounds. Min: 0, Max: " + (map.limbInstanceEmitterCounts[instanceId] - 1) + ", Specified: " + emitterIndex);
                break;
            default:
                throw new ArgumentException("Emitter set location not known: " + emitterSetLocation);
        }

        bool validWeight = weight >= InputDefinitionParameters.MinWeight && weight <= InputDefinitionParameters.MaxWeight;
        if (!validWeight)
            throw new ArgumentException("Weight out of bounds. Min: " + InputDefinitionParameters.MinWeight
            + ", Max: " + InputDefinitionParameters.MaxWeight + ", Specified: " + weight);
    }

    public static InputDefinition CreateRandom(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        List<EmitterSetLocation> validLocations = emitterAvailabilityMap.GetValidInputSetLocations();
        EmitterSetLocation emitterSetLocation = validLocations[UnityEngine.Random.Range(0, validLocations.Count)];
        int childLimbIndex = -1;
        string instanceId = null;
        int emitterIndex = -1;
        switch (emitterSetLocation)
        {
            case EmitterSetLocation.None:
                break; // No action required. A constant value (the weight) will be applied to this input.
            case EmitterSetLocation.SameLimb:
                emitterIndex = UnityEngine.Random.Range(0, emitterAvailabilityMap.sameLimbEmitterCount);
                break;
            case EmitterSetLocation.Brain:
                emitterIndex = UnityEngine.Random.Range(0, emitterAvailabilityMap.brainEmitterCount);
                break;
            case EmitterSetLocation.ParentLimb:
                emitterIndex = UnityEngine.Random.Range(0, emitterAvailabilityMap.parentLimbEmitterCount);
                break;
            case EmitterSetLocation.ChildLimbs:
                List<int> nonZeroChildLimbs = emitterAvailabilityMap.GetValidChildLimbIndices();
                childLimbIndex = nonZeroChildLimbs[UnityEngine.Random.Range(0, nonZeroChildLimbs.Count)];
                emitterIndex = UnityEngine.Random.Range(0, emitterAvailabilityMap.childLimbEmitterCounts[childLimbIndex]);
                break;
            case EmitterSetLocation.LimbInstances:
                List<string> nonZeroLimbInstances = emitterAvailabilityMap.GetValidLimbInstanceIds();
                instanceId = nonZeroLimbInstances[UnityEngine.Random.Range(0, nonZeroLimbInstances.Count)];
                emitterIndex = UnityEngine.Random.Range(0, emitterAvailabilityMap.limbInstanceEmitterCounts[instanceId]);
                break;
            default:
                throw new ArgumentException("Emitter set location not known: " + emitterSetLocation);
        }

        float weight = UnityEngine.Random.Range(InputDefinitionParameters.MinWeight, InputDefinitionParameters.MaxWeight);

        return new(
            emitterSetLocation,
            childLimbIndex,
            instanceId,
            emitterIndex,
            weight
        );
    }
}
