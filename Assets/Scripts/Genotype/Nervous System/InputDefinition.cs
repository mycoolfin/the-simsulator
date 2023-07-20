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
        bool validWeight = weight >= InputDefinitionParameters.MinWeight && weight <= InputDefinitionParameters.MaxWeight;
        if (!validWeight)
            throw new ArgumentException("Weight out of bounds. Min: " + InputDefinitionParameters.MinWeight
            + ", Max: " + InputDefinitionParameters.MaxWeight + ", Specified: " + weight);
    }

    public static InputDefinition CreateRandom(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        List<EmitterSetLocation> validLocations = emitterAvailabilityMap.GetValidInputSetLocations();
        EmitterSetLocation emitterSetLocation = validLocations[UnityEngine.Random.Range(0, validLocations.Count)];
        int childLimbIndex = 0;
        string instanceId = null;
        int emitterIndex = 0;
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
