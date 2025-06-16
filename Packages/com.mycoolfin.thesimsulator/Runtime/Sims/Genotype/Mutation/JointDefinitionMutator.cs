using System;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class JointDefinitionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, int>> JointDefinitionMutations = new()
        {
            { MutateJointType, 1f },
            { MutateAnchorOnParentFace, 1f },
            { MutateAngleLimits, 1f },
            { MutateXAxisInputs, 1f },
            { MutateYAxisInputs, 1f },
            { MutateZAxisInputs, 1f }
        };

        public static void MutateJointDefinition(SimsGenotypeCreationContext context, int nodeIndex)
        {
            JointDefinitionMutations.Choose().Invoke(context, nodeIndex);
        }

        public static void MutateJointType(SimsGenotypeCreationContext context, int nodeIndex)
        {
            Node node = context.Nodes[nodeIndex];
            JointType newJointType = JointDefinition.AllJointTypes[SharedRandom.Next(JointDefinition.AllJointTypes.Length)];
            JointDefinition newJointDefinition = new(newJointType, node.JointDefinition.AnchorOnParentFace, node.JointDefinition.AngleLimits,
                node.JointDefinition.XAxisInputs, node.JointDefinition.YAxisInputs, node.JointDefinition.ZAxisInputs);
            Node newNode = new(node.Dimensions, newJointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }

        public static void MutateAnchorOnParentFace(SimsGenotypeCreationContext context, int nodeIndex)
        {
            float sigma = 0.1f;
            Node node = context.Nodes[nodeIndex];
            Vector2 newAnchorOnParentFace = new(
                Math.Clamp(node.JointDefinition.AnchorOnParentFace.X + SharedRandom.DrawGaussian(sigma), JointDefinition.MinAnchorOnParentFace.X, JointDefinition.MaxAnchorOnParentFace.X),
                Math.Clamp(node.JointDefinition.AnchorOnParentFace.Y + SharedRandom.DrawGaussian(sigma), JointDefinition.MinAnchorOnParentFace.Y, JointDefinition.MaxAnchorOnParentFace.Y)
            );
            JointDefinition newJointDefinition = new(node.JointDefinition.JointType, newAnchorOnParentFace, node.JointDefinition.AngleLimits,
                node.JointDefinition.XAxisInputs, node.JointDefinition.YAxisInputs, node.JointDefinition.ZAxisInputs);
            Node newNode = new(node.Dimensions, newJointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }

        public static void MutateAngleLimits(SimsGenotypeCreationContext context, int nodeIndex)
        {
            float sigma = 5f;
            Node node = context.Nodes[nodeIndex];
            Vector3 newAngleLimits = new(
                Math.Clamp(node.JointDefinition.AngleLimits.X + SharedRandom.DrawGaussian(sigma), JointDefinition.MinAngleLimit.X, JointDefinition.MaxAngleLimit.X),
                Math.Clamp(node.JointDefinition.AngleLimits.Y + SharedRandom.DrawGaussian(sigma), JointDefinition.MinAngleLimit.Y, JointDefinition.MaxAngleLimit.Y),
                Math.Clamp(node.JointDefinition.AngleLimits.Z + SharedRandom.DrawGaussian(sigma), JointDefinition.MinAngleLimit.Z, JointDefinition.MaxAngleLimit.Z)
            );
            JointDefinition newJointDefinition = new(node.JointDefinition.JointType, node.JointDefinition.AnchorOnParentFace, newAngleLimits,
                node.JointDefinition.XAxisInputs, node.JointDefinition.YAxisInputs, node.JointDefinition.ZAxisInputs);
            Node newNode = new(node.Dimensions, newJointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }

        public static void MutateXAxisInputs(SimsGenotypeCreationContext context, int nodeIndex)
        {
            Node node = context.Nodes[nodeIndex];
            InputSetDefinition newXAxisInputs = new();
            InputSetDefinitionMutator.MutateInputSetDefinition(context, node.Gid, node.JointDefinition.XAxisInputs, newInputs => newXAxisInputs = newInputs);
            JointDefinition newJointDefinition = new(node.JointDefinition.JointType, node.JointDefinition.AnchorOnParentFace, node.JointDefinition.AngleLimits,
                newXAxisInputs, node.JointDefinition.YAxisInputs, node.JointDefinition.ZAxisInputs);
            Node newNode = new(node.Dimensions, newJointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }

        public static void MutateYAxisInputs(SimsGenotypeCreationContext context, int nodeIndex)
        {
            Node node = context.Nodes[nodeIndex];
            InputSetDefinition newYAxisInputs = new();
            InputSetDefinitionMutator.MutateInputSetDefinition(context, node.Gid, node.JointDefinition.YAxisInputs, newInputs => newYAxisInputs = newInputs);
            JointDefinition newJointDefinition = new(node.JointDefinition.JointType, node.JointDefinition.AnchorOnParentFace, node.JointDefinition.AngleLimits,
                node.JointDefinition.XAxisInputs, newYAxisInputs, node.JointDefinition.ZAxisInputs);
            Node newNode = new(node.Dimensions, newJointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }

        public static void MutateZAxisInputs(SimsGenotypeCreationContext context, int nodeIndex)
        {
            Node node = context.Nodes[nodeIndex];
            InputSetDefinition newZAxisInputs = new();
            InputSetDefinitionMutator.MutateInputSetDefinition(context, node.Gid, node.JointDefinition.ZAxisInputs, newInputs => newZAxisInputs = newInputs);
            JointDefinition newJointDefinition = new(node.JointDefinition.JointType, node.JointDefinition.AnchorOnParentFace, node.JointDefinition.AngleLimits,
                node.JointDefinition.XAxisInputs, node.JointDefinition.YAxisInputs, newZAxisInputs);
            Node newNode = new(node.Dimensions, newJointDefinition, node.RecursiveLimit);
            context.Nodes[nodeIndex] = newNode;
        }
    }
}
