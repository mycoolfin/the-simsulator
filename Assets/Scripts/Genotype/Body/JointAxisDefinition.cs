using System;
using UnityEngine;

[Serializable]
public class JointAxisDefinition
{
    [SerializeField] private float limit;
    [SerializeField] private InputDefinition inputDefinition;

    public float Limit => limit;
    public InputDefinition InputDefinition => inputDefinition;

    public JointAxisDefinition(float limit, InputDefinition inputDefinition)
    {
        this.limit = limit;
        this.inputDefinition = inputDefinition;
    }

    public void Validate(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        bool validLimit = limit >= JointDefinitionParameters.MinAngle && limit <= JointDefinitionParameters.MaxAngle;
        if (!validLimit)
            throw new ArgumentException("The joint axis limit was out of bounds. Min: " + JointDefinitionParameters.MinAngle + ", Max: "
            + JointDefinitionParameters.MaxAngle + ", Specified: " + limit);

        inputDefinition.Validate(emitterAvailabilityMap);
    }

    public static JointAxisDefinition CreateRandom(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        float limit = UnityEngine.Random.Range(JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle);
        InputDefinition inputDefinition = InputDefinition.CreateRandom(emitterAvailabilityMap);
        return new(limit, inputDefinition);
    }
}
