using System;

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
            Node node = context.Nodes[nodeIndex];
            Vector3 newDimensions = new(
                Math.Clamp(SharedRandom.DrawGaussian(sigma) + node.Dimensions.X, Node.MinDimensions.X, Node.MaxDimensions.X),
                Math.Clamp(SharedRandom.DrawGaussian(sigma) + node.Dimensions.Y, Node.MinDimensions.Y, Node.MaxDimensions.Y),
                Math.Clamp(SharedRandom.DrawGaussian(sigma) + node.Dimensions.Z, Node.MinDimensions.Z, Node.MaxDimensions.Z)
            );

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
            Node node = context.Nodes[nodeIndex];
            int newRecursiveLimit = Math.Clamp(
                node.RecursiveLimit + (int)Math.Floor(SharedRandom.DrawGaussian(sigma)),
                Node.MinRecursiveLimit,
                Node.MaxRecursiveLimit
            );
            Node newNode = new(node.Dimensions, node.JointDefinition, newRecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }
    }
}
