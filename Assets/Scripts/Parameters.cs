public static class GenotypeParameters
{
    public static int MinLimbNodes = 1;
    public static int MaxLimbNodes = 10;
    public static int MinBrainNeurons = 0;
    public static int MaxBrainNeurons = 10;
}

public static class GenotypeGenerationParameters
{
    public static float ConnectionAttemptChance = 0.5f;
    public static int MaxConnectionAttempts = 5;
}

public static class LimbNodeParameters
{
    public static float MinSize = 0.1f;
    public static float MaxSize = 2f;
    public static int MaxRecursiveLimit = 5;
    public static int MinNeurons = 0;
    public static int MaxNeurons = 10;
    public static int MaxLimbConnections = 4;
}

public static class LimbConnectionParameters
{
    public static float MinScale = 0.1f;
    public static float MaxScale = 2f;
    public static float MinAngle = -45f;
    public static float MaxAngle = 45f;
}

public static class JointDefinitionParameters
{
    public static float MinAngle = 0f;
    public static float MaxAngle = 90f;
}

public static class InputDefinitionParameters
{
    public static float MinWeight = -2f;
    public static float MaxWeight = 2f;
}

public static class LimbParameters
{
    public static float MinSize = 0.1f;
    public static float MaxSize = 2f;
}

public static class JointParameters
{
    public static float StrengthMultiplier = 50f;
    public static float AngularVelocityMultiplier = 1f;
    public static float SmoothingFactor = 0.05f;
}

public static class PhenotypeParameters
{
    public static float MaxSize = 10f;
}

public static class PhenotypeBuilderParameters
{
    public static int MaxLimbs = 12;
}

public static class ReproductionParameters
{
    public static float AsexualProbability = 0.4f;
    public static float CrossoverProbability = 0.3f;
    public static float GraftingProbability = 0.3f;
    public static int CrossoverInterval = 2;
}

public static class MutationParameters
{
    // The average number of mutations per child.
    public static float MutationRate = 10f;

    public static class Root
    {
        public static float ChangeBrain = 0.1f;
        public static float ChangeLimbNode = 0.9f;
    }

    public static class Brain
    {
        public static float AddNeuron = 1f;
        public static float RemoveNeuron = 1f;
        public static float ChangeNeuronDefinition = 1f;
    }

    public static class LimbNode
    {
        public static float ChangeDimensions = 1f;
        public static float ChangeJointDefinition = 1f;
        public static float ChangeRecursiveLimit = 1f;
        public static float AddNeuron = 1f;
        public static float RemoveNeuron = 1f;
        public static float ChangeNeuronDefinition = 1f;
        public static float AddLimbConnection = 1f;
        public static float RemoveLimbConnection = 1f;
        public static float ChangeLimbConnection = 1f;
    }

    public static class JointDefinition
    {
        public static float ChangeJointType = 1f;
        public static float ChangeJointAxisDefinition = 1f;
    }

    public static class JointAxisDefinition
    {
        public static float ChangeJointLimit = 1f;
        public static float ChangeInputDefinition = 1f;
    }

    public static class LimbConnection
    {
        public static float ChangeChildNode = 1f;
        public static float ChangeParentFace = 1f;
        public static float ChangePosition = 1f;
        public static float ChangeOrientation = 1f;
        public static float ChangeScale = 1f;
        public static float ChangeReflectionX = 1f;
        public static float ChangeReflectionY = 1f;
        public static float ChangeReflectionZ = 1f;
        public static float ChangeTerminalOnly = 1f;
    }

    public static class NeuronDefinition
    {
        public static float ChangeNeuronType = 1f;
        public static float ChangeInputDefinition = 1f;
    }

    public static class InputDefinition
    {
        public static float ChangeEmitterSetLocation = 1f;
        public static float ChangeChildLimbIndex = 1f;
        public static float ChangeInstanceId = 1f;
        public static float ChangeEmitterIndex = 1f;
        public static float ChangeWeight = 1f;
    }
}
