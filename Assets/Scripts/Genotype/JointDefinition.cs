using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public enum JointType
{
    Rigid,
    Revolute,
    Twist,
    Universal,
    BendTwist,
    TwistBend,
    Spherical
}

public static class JointTypeExtensions
{
    public static int DegreesOfFreedom(this JointType self)
    {
        return new Dictionary<JointType, int>
        {
            { JointType.Rigid, 0 },
            { JointType.Revolute, 1 },
            { JointType.Twist, 1 },
            { JointType.Universal, 1 },
            { JointType.BendTwist, 2 },
            { JointType.TwistBend, 2 },
            { JointType.Spherical, 3 },
        }[self];
    }
}

public struct JointDefinition
{
    public readonly JointType type;
    public readonly ReadOnlyCollection<float> limits;
    public readonly ReadOnlyCollection<ReadOnlyCollection<float>> effectorInputPreferences;
    public readonly ReadOnlyCollection<ReadOnlyCollection<float>> effectorInputWeights;

    public JointDefinition(JointType type, ReadOnlyCollection<float> limits, ReadOnlyCollection<ReadOnlyCollection<float>> effectorInputPreferences, ReadOnlyCollection<ReadOnlyCollection<float>> effectorInputWeights)
    {
        if (limits?.Count != effectorInputPreferences?.Count || limits?.Count != effectorInputWeights?.Count)
            throw new System.ArgumentException("Joint parameters are improperly specified");

        this.type = type;
        this.limits = limits.Select(x => Mathf.Clamp(x, JointDefinitionGenerationParameters.MinAngle, JointDefinitionGenerationParameters.MaxAngle)).ToList().AsReadOnly();
        this.effectorInputPreferences = effectorInputPreferences.Select(x => x.Select(y => Mathf.Clamp(y, 0f, 1f)).ToList().AsReadOnly()).ToList().AsReadOnly();
        this.effectorInputWeights = effectorInputWeights.Select(x => x.Select(y => Mathf.Clamp(y, JointDefinitionGenerationParameters.MinWeight, JointDefinitionGenerationParameters.MaxWeight)).ToList().AsReadOnly()).ToList().AsReadOnly();
    }

    public JointDefinition CreateCopy()
    {
        return new JointDefinition(
            type,
            limits,
            effectorInputPreferences,
            effectorInputWeights
        );
    }

    public static JointDefinition CreateRandom()
    {
        Array jointTypes = Enum.GetValues(typeof(JointType));
        JointType type = (JointType)jointTypes.GetValue(UnityEngine.Random.Range(0, jointTypes.Length));

        int jointDof = type.DegreesOfFreedom();

        List<float> limits = new List<float>();
        List<ReadOnlyCollection<float>> effectorInputPreferences = new List<ReadOnlyCollection<float>>();
        List<ReadOnlyCollection<float>> effectorInputWeights = new List<ReadOnlyCollection<float>>();

        for (int i = 0; i < jointDof; i++)
        {
            limits.Add(UnityEngine.Random.Range(JointDefinitionGenerationParameters.MinAngle, JointDefinitionGenerationParameters.MaxAngle));
            effectorInputPreferences.Add((new List<float> { UnityEngine.Random.Range(0f, 1f) }).AsReadOnly());
            effectorInputWeights.Add((new List<float> { UnityEngine.Random.Range(JointDefinitionGenerationParameters.MinWeight, JointDefinitionGenerationParameters.MaxWeight) }).AsReadOnly());
        }

        return new JointDefinition(type, limits.AsReadOnly(), effectorInputPreferences.AsReadOnly(), effectorInputWeights.AsReadOnly());
    }
}
