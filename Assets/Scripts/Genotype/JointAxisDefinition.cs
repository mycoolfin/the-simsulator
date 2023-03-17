using System;
using UnityEngine;

[Serializable]
public class JointAxisDefinition : IDefinition
{
    [SerializeField] private float limit;
    [SerializeField] private InputDefinition inputDefinition;

    public float Limit => limit;
    public InputDefinition InputDefinition => inputDefinition;

    public JointAxisDefinition(float limit, InputDefinition inputDefinition)
    {
        ValidateLimit(limit);
        ValidateInputDefinition(inputDefinition);

        this.limit = limit;
        this.inputDefinition = inputDefinition;
    }

    private static void ValidateLimit(float limit)
    {
        bool validLimit = limit >= JointDefinitionParameters.MinAngle && limit <= JointDefinitionParameters.MaxAngle;
        if (!validLimit)
            throw new ArgumentException("The joint axis limit was out of bounds. Min: " + JointDefinitionParameters.MinAngle + ", Max: "
            + JointDefinitionParameters.MaxAngle + ", Specified: " + limit);
    }

    private static void ValidateInputDefinition(InputDefinition inputDefinition)
    {
        inputDefinition.Validate();
    }

    public void Validate()
    {
        ValidateLimit(limit);
        ValidateInputDefinition(inputDefinition);
    }

    public static JointAxisDefinition CreateRandom()
    {
        float limit = UnityEngine.Random.Range(JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle);
        InputDefinition inputDefinition = InputDefinition.CreateRandom();
        return new JointAxisDefinition(limit, inputDefinition);
    }
}
