using UnityEngine;

public class ParameterManager : MonoBehaviour
{
    private static ParameterManager _instance;
    public static ParameterManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public readonly GenotypeParameters Genotype = new();
    public readonly GenotypeGenerationParameters GenotypeGeneration = new();
    public readonly LimbNodeParameters LimbNode = new();
    public readonly LimbConnectionParameters LimbConnection = new();
    public readonly JointDefinitionParameters JointDefinition = new();
    public readonly InputDefinitionParameters InputDefinition = new();
    public readonly LimbParameters Limb = new();
    public readonly JointParameters Joint = new();
    public readonly PhenotypeParameters Phenotype = new();
    public readonly PhenotypeBuilderParameters PhenotypeBuilder = new();
    public readonly ReproductionParameters Reproduction = new();
    public readonly RecombinationParameters Recombination = new();
    public readonly MutationParameters Mutation = new();
}


public class GenotypeParameters
{
    public int MinLimbNodes = 1;
    public int MaxLimbNodes = 4;
    public int MinBrainNeurons = 0;
    public int MaxBrainNeurons = 10;
}

public class GenotypeGenerationParameters
{
    public float ConnectionAttemptChance = 0.5f;
    public int MaxConnectionAttempts = 5;
}

public class LimbNodeParameters
{
    public float MinSize = 0.1f;
    public float MaxSize = 2f;
    public int MaxRecursiveLimit = 5;
    public int MinNeurons = 0;
    public int MaxNeurons = 10;
    public int MaxLimbConnections = 4;
}

public class LimbConnectionParameters
{
    public float MinScale = 0.1f;
    public float MaxScale = 2f;
    public float MinAngle = -45f;
    public float MaxAngle = 45f;
}

public class JointDefinitionParameters
{
    public float MinAngle = 0f;
    public float MaxAngle = 90f;
}

public class InputDefinitionParameters
{
    public float MinWeight = -2f;
    public float MaxWeight = 2f;
}

public class LimbParameters
{
    public float MinSize = 0.1f;
    public float MaxSize = 2f;
}

public class JointParameters
{
    public float StrengthMultiplier = 50f;
    public float AngularVelocityMultiplier = 1f;
    public float SmoothingFactor = 0.05f;
}

public class PhenotypeParameters
{
    public float MaxSize = 20f;
}

public class PhenotypeBuilderParameters
{
    public int MaxLimbs = 16;
}

public class ReproductionParameters
{
    public bool LockMorphologies = false; // Whether recombination and mutation can affect creature morphologies.
}

public class RecombinationParameters
{
    public float AsexualProbability = 0.4f;
    public float CrossoverProbability = 0.3f;
    public float GraftingProbability = 0.3f;
    public int CrossoverInterval = 2;
}

public class MutationParameters
{
    // The average number of mutations per child.
    public float MutationRate = 10f;

    public RootParameters Root = new();
    public BrainParameters Brain = new();
    public LimbNodeParameters LimbNode = new();
    public JointDefinitionParameters JointDefinition = new();
    public JointAxisDefinitionParameters JointAxisDefinition = new();
    public LimbConnectionParameters LimbConnection = new();
    public NeuronDefinitionParameters NeuronDefinition = new();
    public InputDefinitionParameters InputDefinition = new();

    public class RootParameters
    {
        public float ChangeBrain = 0.1f;
        public float ChangeLimbNode = 0.9f;
    }

    public class BrainParameters
    {
        public float AddNeuron = 1f;
        public float RemoveNeuron = 1f;
        public float ChangeNeuronDefinition = 1f;
    }

    public class LimbNodeParameters
    {
        public float ChangeDimensions = 1f;
        public float ChangeJointDefinition = 1f;
        public float ChangeRecursiveLimit = 1f;
        public float AddNeuron = 1f;
        public float RemoveNeuron = 1f;
        public float ChangeNeuronDefinition = 1f;
        public float AddLimbConnection = 1f;
        public float RemoveLimbConnection = 1f;
        public float ChangeLimbConnection = 1f;
    }

    public class JointDefinitionParameters
    {
        public float ChangeJointType = 1f;
        public float ChangeJointAxisDefinition = 1f;
    }

    public class JointAxisDefinitionParameters
    {
        public float ChangeJointLimit = 1f;
        public float ChangeInputDefinition = 1f;
    }

    public class LimbConnectionParameters
    {
        public float ChangeChildNode = 1f;
        public float ChangeParentFace = 1f;
        public float ChangePosition = 1f;
        public float ChangeOrientation = 1f;
        public float ChangeScale = 1f;
        public float ChangeReflectionX = 1f;
        public float ChangeReflectionY = 1f;
        public float ChangeReflectionZ = 1f;
        public float ChangeTerminalOnly = 1f;
    }

    public class NeuronDefinitionParameters
    {
        public float ChangeNeuronType = 1f;
        public float ChangeInputDefinition = 1f;
    }

    public class InputDefinitionParameters
    {
        public float ChangeEmitterSetLocation = 1f;
        public float ChangeChildLimbIndex = 1f;
        public float ChangeInstanceId = 1f;
        public float ChangeEmitterIndex = 1f;
        public float ChangeWeight = 1f;
    }
}
