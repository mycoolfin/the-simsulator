using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class JointDefinition
{
    [SerializeField] private JointType type;
    [SerializeField] private List<JointAxisDefinition> axisDefinitions;

    public JointType Type => type;
    public ReadOnlyCollection<JointAxisDefinition> AxisDefinitions => axisDefinitions.AsReadOnly();

    public JointDefinition(JointType type, IList<JointAxisDefinition> axisDefinitions)
    {
        this.type = type;
        this.axisDefinitions = axisDefinitions.ToList();
    }

    public void Validate(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        bool validJointType = Enum.IsDefined(typeof(JointType), type);
        if (!validJointType)
            throw new ArgumentException("Unknown joint type. Specified: " + type);

        bool validAxisDefinitions = axisDefinitions.Count == 3;
        if (!validAxisDefinitions)
            throw new ArgumentException("Expected 3 axis definitions. Specified: " + axisDefinitions.Count);

        foreach (JointAxisDefinition axisDefinition in axisDefinitions)
            axisDefinition.Validate(emitterAvailabilityMap);
    }

    public static JointDefinition CreateRandom(EmitterAvailabilityMap emitterAvailabilityMap, JointType type)
    {
        return new(
            type,
            new List<JointAxisDefinition> {
                JointAxisDefinition.CreateRandom(emitterAvailabilityMap),
                JointAxisDefinition.CreateRandom(emitterAvailabilityMap),
                JointAxisDefinition.CreateRandom(emitterAvailabilityMap)
            }
        );
    }
}
