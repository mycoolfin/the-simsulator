using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class GenotypeSimilarity
{
    private static class MutationProbabilityTotals
    {
        public static readonly float Root =
            ParameterManager.Instance.Mutation.Root.ChangeLimbNode +
            ParameterManager.Instance.Mutation.Root.ChangeBrain;

        public static readonly float LimbNode =
            ParameterManager.Instance.Mutation.LimbNode.ChangeDimensions +
            ParameterManager.Instance.Mutation.LimbNode.ChangeJointDefinition +
            ParameterManager.Instance.Mutation.LimbNode.ChangeRecursiveLimit +
            ParameterManager.Instance.Mutation.LimbNode.AddNeuron +
            ParameterManager.Instance.Mutation.LimbNode.RemoveNeuron +
            ParameterManager.Instance.Mutation.LimbNode.ChangeNeuronDefinition +
            ParameterManager.Instance.Mutation.LimbNode.AddLimbConnection +
            ParameterManager.Instance.Mutation.LimbNode.RemoveLimbConnection +
            ParameterManager.Instance.Mutation.LimbNode.ChangeLimbConnection;

        public static readonly float LimbConnection =
            ParameterManager.Instance.Mutation.LimbConnection.ChangeChildNode +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeParentFace +
            ParameterManager.Instance.Mutation.LimbConnection.ChangePosition +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeOrientation +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeScale +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeReflectionX +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeReflectionY +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeReflectionZ +
            ParameterManager.Instance.Mutation.LimbConnection.ChangeTerminalOnly;

        public static readonly float JointDefinition =
            ParameterManager.Instance.Mutation.JointDefinition.ChangeJointType +
            ParameterManager.Instance.Mutation.JointDefinition.ChangeJointAxisDefinition;

        public static readonly float JointAxisDefinition =
            ParameterManager.Instance.Mutation.JointAxisDefinition.ChangeJointLimit +
            ParameterManager.Instance.Mutation.JointAxisDefinition.ChangeInputDefinition;

        public static readonly float NeuronDefinition =
            ParameterManager.Instance.Mutation.NeuronDefinition.ChangeNeuronType +
            ParameterManager.Instance.Mutation.NeuronDefinition.ChangeInputDefinition;

        public static readonly float InputDefinition =
            ParameterManager.Instance.Mutation.InputDefinition.ChangeEmitterSetLocation +
            ParameterManager.Instance.Mutation.InputDefinition.ChangeChildLimbIndex +
            ParameterManager.Instance.Mutation.InputDefinition.ChangeInstanceId +
            ParameterManager.Instance.Mutation.InputDefinition.ChangeEmitterIndex +
            ParameterManager.Instance.Mutation.InputDefinition.ChangeWeight;
    }

    public static float GetGeneticSimilarity(Genotype g1, Genotype g2)
    {
        // 0.0 <-> 1.0 = no similarity <-> exact similarity.

        if (g1 == null || g2 == null)
            return 0f;

        float limbNodesSimilarity = GetPairwiseSimilarity(g1.LimbNodes, g2.LimbNodes, GetLimbNodeSimilarity);
        float brainSimilarity = GetPairwiseSimilarity(g1.BrainNeuronDefinitions, g2.BrainNeuronDefinitions, GetNeuronDefinitionSimilarity);

        return (
            limbNodesSimilarity * ParameterManager.Instance.Mutation.Root.ChangeLimbNode +
            brainSimilarity * ParameterManager.Instance.Mutation.Root.ChangeBrain
        ) / MutationProbabilityTotals.Root;
    }

    private static float GetLimbNodeSimilarity(LimbNode l1, LimbNode l2)
    {
        float sDimensions = GetVector3Similarity(l1.Dimensions, l2.Dimensions);
        float sJointDefinition = GetJointDefinitionSimilarity(l1.JointDefinition, l2.JointDefinition);
        float sRecursiveLimit = GetIntSimilarity(l1.RecursiveLimit, l2.RecursiveLimit);

        float sNeuronDefinitions = GetPairwiseSimilarity(l1.NeuronDefinitions, l2.NeuronDefinitions, GetNeuronDefinitionSimilarity);
        float sLimbConnections = GetPairwiseSimilarity(l1.Connections, l2.Connections, GetLimbConnectionSimilarity);

        return (
            sDimensions * ParameterManager.Instance.Mutation.LimbNode.ChangeDimensions +
            sJointDefinition * ParameterManager.Instance.Mutation.LimbNode.ChangeJointDefinition +
            sRecursiveLimit * ParameterManager.Instance.Mutation.LimbNode.ChangeRecursiveLimit +
            sNeuronDefinitions *
              (ParameterManager.Instance.Mutation.LimbNode.AddNeuron +
               ParameterManager.Instance.Mutation.LimbNode.RemoveNeuron +
               ParameterManager.Instance.Mutation.LimbNode.ChangeNeuronDefinition) +
            sLimbConnections *
              (ParameterManager.Instance.Mutation.LimbNode.AddLimbConnection +
               ParameterManager.Instance.Mutation.LimbNode.RemoveLimbConnection +
               ParameterManager.Instance.Mutation.LimbNode.ChangeLimbConnection)
        ) / MutationProbabilityTotals.LimbNode;
    }

    private static float GetJointDefinitionSimilarity(JointDefinition j1, JointDefinition j2)
    {
        float sType = GetIntSimilarity((int)j1.Type, (int)j2.Type);
        float sAxisDefinition = Enumerable.Range(0, 3).Average(i => GetJointAxisDefinitionSimilarity(j1.AxisDefinitions[i], j2.AxisDefinitions[i]));

        return (
            sType * ParameterManager.Instance.Mutation.JointDefinition.ChangeJointType +
            sAxisDefinition * ParameterManager.Instance.Mutation.JointDefinition.ChangeJointAxisDefinition
        ) / MutationProbabilityTotals.JointDefinition;
    }

    private static float GetJointAxisDefinitionSimilarity(JointAxisDefinition a1, JointAxisDefinition a2)
    {
        float sLimit = GetFloatSimilarity(a1.Limit, a2.Limit);
        float sInputDefinition = GetInputDefinitionSimilarity(a1.InputDefinition, a2.InputDefinition);

        return (
            sLimit * ParameterManager.Instance.Mutation.JointAxisDefinition.ChangeJointLimit +
            sInputDefinition * ParameterManager.Instance.Mutation.JointAxisDefinition.ChangeInputDefinition
        ) / MutationProbabilityTotals.JointAxisDefinition;
    }

    private static float GetNeuronDefinitionSimilarity(NeuronDefinition n1, NeuronDefinition n2)
    {
        float sType = GetIntSimilarity((int)n1.Type, (int)n2.Type);
        float sInputDefinitions = GetPairwiseSimilarity(n1.InputDefinitions, n2.InputDefinitions, GetInputDefinitionSimilarity);

        return (
            sType * ParameterManager.Instance.Mutation.NeuronDefinition.ChangeNeuronType +
            sInputDefinitions * ParameterManager.Instance.Mutation.NeuronDefinition.ChangeInputDefinition
        ) / MutationProbabilityTotals.NeuronDefinition;
    }

    private static float GetLimbConnectionSimilarity(LimbConnection c1, LimbConnection c2)
    {
        float sChildNode = GetIntSimilarity(c1.ChildNodeId, c2.ChildNodeId);
        float sParentFace = GetIntSimilarity(c1.ParentFace, c2.ParentFace);
        float sPosition = GetVector2Similarity(c1.Position, c2.Position);
        float sOrientation = GetVector3Similarity(c1.Orientation, c2.Orientation);
        float sScale = GetVector3Similarity(c1.Scale, c2.Scale);
        float sReflectionX = c1.ReflectionX == c2.ReflectionX ? 1f : 0f;
        float sReflectionY = c1.ReflectionY == c2.ReflectionY ? 1f : 0f;
        float sReflectionZ = c1.ReflectionZ == c2.ReflectionZ ? 1f : 0f;
        float sTerminalOnly = c1.TerminalOnly == c2.TerminalOnly ? 1f : 0f;

        return (
            sChildNode * ParameterManager.Instance.Mutation.LimbConnection.ChangeChildNode +
            sParentFace * ParameterManager.Instance.Mutation.LimbConnection.ChangeParentFace +
            sPosition * ParameterManager.Instance.Mutation.LimbConnection.ChangePosition +
            sOrientation * ParameterManager.Instance.Mutation.LimbConnection.ChangeOrientation +
            sScale * ParameterManager.Instance.Mutation.LimbConnection.ChangeScale +
            sReflectionX * ParameterManager.Instance.Mutation.LimbConnection.ChangeReflectionX +
            sReflectionY * ParameterManager.Instance.Mutation.LimbConnection.ChangeReflectionY +
            sReflectionZ * ParameterManager.Instance.Mutation.LimbConnection.ChangeReflectionZ +
            sTerminalOnly * ParameterManager.Instance.Mutation.LimbConnection.ChangeTerminalOnly
        ) / MutationProbabilityTotals.LimbConnection;
    }

    private static float GetInputDefinitionSimilarity(InputDefinition i1, InputDefinition i2)
    {
        float sEmitterSetLocation = GetIntSimilarity((int)i1.EmitterSetLocation, (int)i2.EmitterSetLocation);
        float sChildLimbIndex = GetIntSimilarity(i1.ChildLimbIndex, i2.ChildLimbIndex);
        float sInstanceId = i1.InstanceId == i2.InstanceId ? 1f : 0f;
        float sEmitterIndex = GetIntSimilarity(i1.EmitterIndex, i2.EmitterIndex);
        float sWeight = GetFloatSimilarity(i1.Weight, i2.Weight);

        return (
            sEmitterSetLocation * ParameterManager.Instance.Mutation.InputDefinition.ChangeEmitterSetLocation +
            sChildLimbIndex * ParameterManager.Instance.Mutation.InputDefinition.ChangeChildLimbIndex +
            sInstanceId * ParameterManager.Instance.Mutation.InputDefinition.ChangeInstanceId +
            sEmitterIndex * ParameterManager.Instance.Mutation.InputDefinition.ChangeEmitterIndex +
            sWeight * ParameterManager.Instance.Mutation.InputDefinition.ChangeWeight
        ) / MutationProbabilityTotals.InputDefinition;
    }

    private struct Pair<T>
    {
        public T p1;
        public T p2;
        public float score;
    }

    private static float GetPairwiseSimilarity<T>(IList<T> col1, IList<T> col2, Func<T, T, float> GetPairSimilarity)
    {
        if (col1.Count == 0 && col2.Count == 0)
            return 1f;
        if (col1.Count == 0 || col2.Count == 0)
            return 0f;

        IList<T> basis = col1.Count > col2.Count ? col1 : col2;
        IList<T> other = col1.Count > col2.Count ? col2 : col1;

        float[,] similarityMatrix = new float[col1.Count, col2.Count];
        for (int i = 0; i < col1.Count; i++)
            for (int j = 0; j < col2.Count; j++)
                similarityMatrix[i, j] = GetPairSimilarity(col1[i], col2[j]);

        List<Pair<T>> pairs = new();
        List<int> pairedBasisNodes = new();
        while (pairs.Count < col1.Count)
        {
            float highestScore = similarityMatrix.Length == 0 ? -1f : similarityMatrix.Cast<float>().Max();
            if (highestScore >= 0)
            {
                (int, int) index = FindFirstIndexOf(similarityMatrix, highestScore);
                pairs.Add(new()
                {
                    p1 = col1[index.Item1],
                    p2 = col2[index.Item2],
                    score = highestScore
                });

                // Clear selected items from matrix.
                for (int j = 0; j < col2.Count; j++)
                    similarityMatrix[index.Item1, j] = -1f;
                for (int i = 0; i < col1.Count; i++)
                    similarityMatrix[i, index.Item2] = -1f;
            }
            else
            {
                // Add remaining unpaired items.
                for (int i = 0; i < col1.Count; i++)
                {
                    if (!pairedBasisNodes.Contains(i))
                    {
                        pairs.Add(new()
                        {
                            p1 = col1[i],
                            p2 = default,
                            score = 0f
                        });
                    }
                }
            }
        }

        return pairs.Average(p => p.score);
    }

    private static float GetVector2Similarity(Vector2 v1, Vector2 v2)
    {
        Vector2 diff = v2 - v1;
        return (
            TwoTailedTest(diff.x) +
            TwoTailedTest(diff.y)
        ) / 2f;
    }

    private static float GetVector3Similarity(Vector3 v1, Vector3 v2)
    {
        Vector3 diff = v2 - v1;
        return (
            TwoTailedTest(diff.x) +
            TwoTailedTest(diff.y) +
            TwoTailedTest(diff.z)
        ) / 3f;
    }

    private static float GetIntSimilarity(int i1, int i2)
    {
        return i1 == i2 ? 1f : 0f;
    }

    private static float GetFloatSimilarity(float f1, float f2)
    {
        return TwoTailedTest(f2 - f1);
    }

    public static float TwoTailedTest(float value)
    {
        float a1 = 0.254829592f;
        float a2 = -0.284496736f;
        float a3 = 1.421413741f;
        float a4 = -1.453152027f;
        float a5 = 1.061405429f;
        float p = 0.3275911f;

        float x = Mathf.Abs(value) / Mathf.Sqrt(2.0f);

        float t = 1.0f / (1.0f + p * x);
        float y = 1.0f - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Mathf.Exp(-x * x);

        return 1.0f - y;
    }

    private static (int, int) FindFirstIndexOf(float[,] array, float value)
    {
        for (int i = 0; i < array.GetLength(0); i++)
            for (int j = 0; j < array.GetLength(1); j++)
            {
                if (array[i, j] == value) return (i, j);
            }
        return (-1, -1);
    }
}
