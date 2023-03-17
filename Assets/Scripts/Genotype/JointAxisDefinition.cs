using System;

[Serializable]
public struct JointAxisDefinition
{
    public readonly float limit;
    public readonly SignalReceiverInputDefinition inputDefinition;

    public JointAxisDefinition(float limit, SignalReceiverInputDefinition inputDefinition)
    {
        bool validLimit = limit >= JointDefinitionParameters.MinAngle && limit <= JointDefinitionParameters.MaxAngle;
        if (!validLimit)
            throw new ArgumentException("The joint axis limit was out of bounds. Min: " + JointDefinitionParameters.MinAngle + ", Max: "
            + JointDefinitionParameters.MaxAngle + ", Specified: " + limit);

        this.limit = limit;
        this.inputDefinition = inputDefinition;
    }

    public static JointAxisDefinition CreateRandom()
    {
        float limit = UnityEngine.Random.Range(JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle);
        SignalReceiverInputDefinition inputDefinition = SignalReceiverInputDefinition.CreateRandom();
        return new JointAxisDefinition(limit, inputDefinition);
    }
}
