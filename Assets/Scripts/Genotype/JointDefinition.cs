using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class JointDefinition : IDefinition
{
    [SerializeField] private JointType type;
    [SerializeField] private List<JointAxisDefinition> axisDefinitions;

    public JointType Type => type;
    public ReadOnlyCollection<JointAxisDefinition> AxisDefinitions => axisDefinitions.AsReadOnly();

    public JointDefinition(JointType type, IList<JointAxisDefinition> axisDefinitions)
    {
        ValidateJointType(type);
        ValidateJointAxisDefinitions(axisDefinitions);

        this.type = type;
        this.axisDefinitions = axisDefinitions.ToList();
    }

    private static void ValidateJointType(JointType type)
    {
        bool validJointType = Enum.IsDefined(typeof(JointType), type);
        if (!validJointType)
            throw new ArgumentException("Unknown joint type. Specified: " + type);
    }

    private static void ValidateJointAxisDefinitions(IList<JointAxisDefinition> axisDefinitions)
    {
        bool validAxisDefinitions = axisDefinitions.Count == 3;
        if (!validAxisDefinitions)
            throw new ArgumentException("Expected 3 axis definitions. Specified: " + axisDefinitions.Count);

        foreach (JointAxisDefinition axisDefinition in axisDefinitions)
            axisDefinition.Validate();
    }

    public void Validate()
    {
        ValidateJointType(type);
        ValidateJointAxisDefinitions(axisDefinitions);
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
            }
        );
    }
}
