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
        bool validLimit = limit >= ParameterManager.Instance.JointDefinition.MinAngle && limit <= ParameterManager.Instance.JointDefinition.MaxAngle;
        if (!validLimit)
            throw new ArgumentException("The joint axis limit was out of bounds. Min: " + ParameterManager.Instance.JointDefinition.MinAngle + ", Max: "
            + ParameterManager.Instance.JointDefinition.MaxAngle + ", Specified: " + limit);

        inputDefinition.Validate(emitterAvailabilityMap);
    }

    public static JointAxisDefinition CreateRandom(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        float limit = UnityEngine.Random.Range(ParameterManager.Instance.JointDefinition.MinAngle, ParameterManager.Instance.JointDefinition.MaxAngle);
        InputDefinition inputDefinition = InputDefinition.CreateRandom(emitterAvailabilityMap);
        return new(limit, inputDefinition);
    }
}
