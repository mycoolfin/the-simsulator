using System;
using UnityEngine;

[Serializable]
public class InputDefinition : IDefinition
{
    [SerializeField] private float preference;
    [SerializeField] private float weight;

    public float Preference => preference;
    public float Weight => weight;

    public InputDefinition(float preference, float weight)
    {
        ValidatePreference(preference);
        ValidateWeight(weight);

        this.preference = preference;
        this.weight = weight;
    }

    private static void ValidatePreference(float preference)
    {
        bool validPreference = preference >= 0f && preference <= 1f;
        if (!validPreference)
            throw new ArgumentException("Preference out of bounds. Min: 0.0, Max: 1.0, Specified: " + preference);
    }

    private static void ValidateWeight(float weight)
    {
        bool validWeight = weight >= InputDefinitionParameters.MinWeight && weight <= InputDefinitionParameters.MaxWeight;
        if (!validWeight)
            throw new ArgumentException("Weight out of bounds. Min: " + InputDefinitionParameters.MinWeight
            + ", Max: " + InputDefinitionParameters.MaxWeight + ", Specified: " + weight);
    }

    public void Validate()
    {
        ValidatePreference(preference);
        ValidateWeight(weight);
    }

    public static InputDefinition CreateRandom()
    {
        return new InputDefinition(
            UnityEngine.Random.value,
            UnityEngine.Random.Range(InputDefinitionParameters.MinWeight, InputDefinitionParameters.MaxWeight)
        );
    }
}
