using System;

[Serializable]
public struct SignalReceiverInputDefinition
{
    public float preference;
    public float weight;

    public SignalReceiverInputDefinition(float preference, float weight)
    {
        bool validPreference = preference >= 0f && preference <= 1f;
        if (!validPreference)
            throw new ArgumentException("Preference out of bounds. Min: 0.0, Max: 1.0, Specified: " + preference);

        bool validWeight = weight >= JointDefinitionParameters.MinWeight && weight <= JointDefinitionParameters.MaxWeight;
        if (!validPreference)
            throw new ArgumentException("Weight out of bounds. Min: " + JointDefinitionParameters.MinWeight
            + ", Max: " + JointDefinitionParameters.MaxWeight + ", Specified: " + preference);

        this.preference = preference;
        this.weight = weight;
    }

    public static SignalReceiverInputDefinition CreateRandom()
    {
        return new SignalReceiverInputDefinition(
            UnityEngine.Random.value,
            UnityEngine.Random.Range(NeuronDefinitionParameters.MinWeight, NeuronDefinitionParameters.MaxWeight)
        );
    }
}
