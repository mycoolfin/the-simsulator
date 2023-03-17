using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

[Serializable]
public struct JointDefinition
{
    public readonly JointType type;
    public readonly ReadOnlyCollection<JointAxisDefinition> axisDefinitions;

    public JointDefinition(JointType type, ReadOnlyCollection<JointAxisDefinition> axisDefinitions)
    {
        bool validJointType = Enum.IsDefined(typeof(JointType), type);
        if (!validJointType)
            throw new ArgumentException("Unknown joint type. Specified: " + type);

        bool validAxisDefinitions = axisDefinitions.Count == 3;
        if (!validAxisDefinitions)
            throw new ArgumentException("Expected 3 axis definitions. Specified: " + axisDefinitions.Count);

        this.type = type;
        this.axisDefinitions = axisDefinitions;
    }

    public JointDefinition CreateCopy()
    {
        return new JointDefinition(
            type,
            axisDefinitions
        );
    }

    public static JointDefinition CreateRandom()
    {
        Array jointTypes = Enum.GetValues(typeof(JointType));
        JointType type = (JointType)jointTypes.GetValue(UnityEngine.Random.Range(0, jointTypes.Length));

        return new JointDefinition(
            type,
            new List<JointAxisDefinition> {
                JointAxisDefinition.CreateRandom(),
                JointAxisDefinition.CreateRandom(),
                JointAxisDefinition.CreateRandom()
            }.AsReadOnly()
        );
    }
}
