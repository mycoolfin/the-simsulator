public static class GenotypeParameters
{
    public const int MinLimbNodes = 1;
    public const int MaxLimbNodes = 10;
    public const int MinBrainNeurons = 0;
    public const int MaxBrainNeurons = 10;
}

public static class GenotypeGenerationParameters
{
    public const float ConnectionAttemptChance = 0.5f;
    public const int MaxConnectionAttempts = 5;
}

public static class LimbNodeParameters
{
    public const float MinSize = 0.1f;
    public const float MaxSize = 2f;
    public const int MaxRecursiveLimit = 5;
    public const int MinNeurons = 0;
    public const int MaxNeurons = 10;
}

public static class LimbConnectionParameters
{
    public const float MinScale = 0.1f;
    public const float MaxScale = 2f;
    public const float MinAngle = -45f;
    public const float MaxAngle = 45f;
}

public static class JointDefinitionParameters
{
    public const float MinAngle = 0f;
    public const float MaxAngle = 90f;
}

public static class InputDefinitionParameters
{
    public const float MinWeight = -2f;
    public const float MaxWeight = 2f;
}

public static class LimbParameters
{
    public const float MinSize = 0.1f;
    public const float MaxSize = 2f;
}

public static class JointParameters
{
    public const float StrengthMultiplier = 1f;
    public const float AngularVelocityMultiplier = 1f;
    public const float SmoothingMultiplier = 10f;
}

public static class PhenotypeParameters
{
    public const float MaxSize = 10f;
}

public static class PhenotypeBuilderParameters
{
    public const int MaxLimbs = 12;
}

public static class ReproductionParameters
{
    public const float AsexualProbability = 0.4f;
    public const float CrossoverProbability = 0.3f;
    public const float GraftingProbability = 0.3f;
    public const int CrossoverInterval = 2;
}

public static class MutationParameters
{
    // The average number of mutations per child.
    public const float MutationRate = 10f;

    public static class Root
    {
        public const float ChangeLimbNodes = 1f;
        public const float ChangeBrainNeuronDefinitions = 1f;
    }

    public static class LimbNode
    {
        public const float ChangeLimbDimensions = 1f;
        public const float ChangeJointType = 1f;
        public const float ChangeJointAxisDefinition = 1f;
        public const float ChangeRecursiveLimit = 1f;
        public const float ChangeNeuronDefinition = 1f;
        public const float AddLimbConnection = 1f;
        public const float RemoveLimbConnection = 1f;
        public const float ChangeLimbConnection = 1f;
    }

    public static class JointAxisDefinition
    {
        public const float ChangeJointLimit = 1f;
        public const float ChangeInputDefinition = 1f;
    }

    public static class LimbConnection
    {
        public const float ChangeChildNode = 1f;
        public const float ChangeParentFace = 1f;
        public const float ChangePosition = 1f;
        public const float ChangeOrientation = 1f;
        public const float ChangeScale = 1f;
        public const float ChangeReflectionX = 1f;
        public const float ChangeReflectionY = 1f;
        public const float ChangeReflectionZ = 1f;
        public const float ChangeTerminalOnly = 1f;
    }

    public static class NeuronDefinition
    {
        public const float AddNeuron = 1f;
        public const float RemoveNeuron = 1f;
        public const float ChangeNeuronType = 1f;
        public const float ChangeInputDefinition = 1f;
    }

    public static class InputDefinition
    {
        public const float ChangeInputDefinitionInputSetLocation = 1f;
        public const float ChangeInputDefinitionChildLimbIndex = 1f;
        public const float ChangeInputDefinitionInstanceId = 1f;
        public const float ChangeInputDefinitionEmitterIndex = 1f;
        public const float ChangeInputDefinitionWeight = 1f;
    }
}