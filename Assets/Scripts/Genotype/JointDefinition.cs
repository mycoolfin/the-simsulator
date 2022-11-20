using System;
using System.Linq;
using System.Collections.Generic;
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
    public readonly float[] limits;
    public readonly float[][] effectorInputPreferences;
    public readonly float[][] effectorInputWeights;

    public JointDefinition(JointType type, float[] limits, float[][] effectorInputPreferences, float[][] effectorInputWeights)
    {
        if (limits?.Length != effectorInputPreferences?.Length || limits?.Length != effectorInputWeights?.Length)
            throw new System.ArgumentException("Joint parameters are improperly specified");

        this.type = type;
        this.limits = limits.Select(x => Mathf.Clamp(x, JointDefinitionGenerationParameters.MinAngle, JointDefinitionGenerationParameters.MaxAngle)).ToArray();
        this.effectorInputPreferences = effectorInputPreferences.Select(x => x.Select(y => Mathf.Clamp(y, 0f, 1f)).ToArray()).ToArray();
        this.effectorInputWeights = effectorInputWeights.Select(x => x.Select(y => Mathf.Clamp(y, JointDefinitionGenerationParameters.MinWeight, JointDefinitionGenerationParameters.MaxWeight)).ToArray()).ToArray();
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

        float[] limits = new float[jointDof];
        float[][] effectorInputPreferences = new float[jointDof][];
        float[][] effectorInputWeights = new float[jointDof][];

        for (int i = 0; i < jointDof; i++)
        {
            limits[i] = UnityEngine.Random.Range(JointDefinitionGenerationParameters.MinAngle, JointDefinitionGenerationParameters.MaxAngle);
            effectorInputPreferences[i] = new float[] { UnityEngine.Random.Range(0f, 1f) };
            effectorInputWeights[i] = new float[] { UnityEngine.Random.Range(JointDefinitionGenerationParameters.MinWeight, JointDefinitionGenerationParameters.MaxWeight) };
        }

        return new JointDefinition(type, limits, effectorInputPreferences, effectorInputWeights);
    }
}
