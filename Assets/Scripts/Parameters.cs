public static class GenotypeGenerationParameters
{
    public const int MinLimbNodes = 1;
    public const int MaxLimbNodes = 3;
    public const float ConnectionAttemptChance = 0.5f;
    public const int MaxConnectionAttempts = 5;
    public const int MinBrainNeurons = 0;
    public const int MaxBrainNeurons = 10;
}

public static class LimbNodeGenerationParameters
{
    public const float MinSize = 0.1f;
    public const float MaxSize = 2f;
    public const int MinRecursiveLimit = 0;
    public const int MaxRecursiveLimit = 5;
    public const int MinNeurons = 0;
    public const int MaxNeurons = 10;
}

public static class LimbConnectionGenerationParameters
{
    public const float MinAngle = -90f;
    public const float MaxAngle = 90f;
    public const float MinScale = 0.1f;
    public const float MaxScale = 2f;
}

public static class NeuronDefinitionGenerationParameters
{
    public const float MinWeight = -1f;
    public const float MaxWeight = 1f;
}

public static class JointDefinitionGenerationParameters
{
    public const float MinAngle = 0f;
    public const float MaxAngle = 90f;
    public const float MinWeight = -1f;
    public const float MaxWeight = 1f;
}

public static class NeuronDefinitionParameters
{
    public const float MinWeight = -10f;
    public const float MaxWeight = 10f;
}

public static class LimbParameters
{
    public const float MinScale = 0.1f;
    public const float MaxScale = 2f;
}

public static class JointParameters
{
    public const float StrengthMultiplier = 1f;
    public const float AngularVelocityMultiplier = 1f;
    public const float SmoothingMultiplier = 10f;
}

public static class LimbNodeParameters
{
    public const float MinSize = 0.1f;
    public const float MaxSize = 2f;
}

public static class LimbConnectionParameters
{
    public const float MinAngle = -90f;
    public const float MaxAngle = 90f;
}

public static class NervousSystemParameters
{
    public const float SwitchThreshold = 0.5f;
    public const float MinConstantValue = -1f;
    public const float MaxConstantValue = 1f;
}

public static class PhenotypeParameters
{
    public const float MaxSize = 10f;
}

public static class PhenotypeBuilderParameters
{
    public const int MaxLimbs = 10;
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
    public const float Multiplier = 1f;
    public const float AddNeuron = 0.1f;
    public const float RemoveNeuron = 0.1f;
    public const float ChangeNeuronType = 0.1f;
    public const float ChangeNeuronInputPreference = 0.1f;
    public const float ChangeNeuronInputWeight = 0.1f;
    public const float ChangeLimbDimensions = 0.1f;
    public const float ChangeJointType = 0.1f;
    public const float ChangeJointLimit = 0.1f;
    public const float ChangeJointEffectorInputPreference = 0.1f;
    public const float ChangeJointEffectorInputWeight = 0.1f;
    public const float ChangeRecursiveLimit = 0.1f;
    public const float AddLimbConnection = 0.1f;
    public const float RemoveLimbConnection = 0.1f;
    public const float ChangeLimbConnectionChildNode = 0.1f;
    public const float ChangeLimbConnectionParentFace = 0.1f;
    public const float ChangeLimbConnectionPosition = 0.1f;
    public const float ChangeLimbConnectionOrientation = 0.1f;
    public const float ChangeLimbConnectionScale = 0.1f;
    public const float ChangeLimbConnectionReflection = 0.1f;
    public const float ChangeLimbConnectionTerminalOnly = 0.1f;
}