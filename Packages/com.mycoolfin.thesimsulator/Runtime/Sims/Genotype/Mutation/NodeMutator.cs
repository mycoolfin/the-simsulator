using System;
using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class NodeMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, int>> NodeMutations = new()
        {
            { MutateDimensions, 1f },
            { MutateJointDefinition, 1f },
            { MutateRecursiveLimit, 1f }
        };

        public static void MutateNode(SimsGenotypeCreationContext context, int nodeIndex)
        {
            NodeMutations.Choose().Invoke(context, nodeIndex);
        }

        public static void MutateDimensions(SimsGenotypeCreationContext context, int nodeIndex)
        {
            float sigma = 0.1f;
            float RandomDimension(float currentDimension) =>
                Math.Clamp(currentDimension + SharedRandom.DrawGaussian(sigma), Node.MIN_DIMENSION, Node.MAX_DIMENSION);

            Node node = context.Nodes[nodeIndex];
            Vector3 newDimensions = new(RandomDimension(node.Dimensions.X), RandomDimension(node.Dimensions.Y), RandomDimension(node.Dimensions.Z));
            Node newNode = new(newDimensions, node.JointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }

        public static void MutateJointDefinition(SimsGenotypeCreationContext context, int nodeIndex)
        {
            JointDefinitionMutator.MutateJointDefinition(context, nodeIndex);
        }

        public static void MutateRecursiveLimit(SimsGenotypeCreationContext context, int nodeIndex)
        {
            float sigma = 1f;
            int RandomRecursiveLimit(float currentLimit) =>
                (int)Math.Clamp(currentLimit + (int)Math.Floor(SharedRandom.DrawGaussian(sigma)), Node.MIN_RECURSIVE_LIMIT, Node.MAX_RECURSIVE_LIMIT);

            Node node = context.Nodes[nodeIndex];
            int newRecursiveLimit = RandomRecursiveLimit(node.RecursiveLimit);
            Node newNode = new(node.Dimensions, node.JointDefinition, newRecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }
    }
}
