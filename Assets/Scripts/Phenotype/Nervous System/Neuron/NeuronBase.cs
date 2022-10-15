using System.Linq;

public enum NeuronType
{
    Abs,
    Atan,
    Cos,
    Differentiate,
    Divide,
    Expt,
    GreaterThan,
    If,
    Integrate,
    Interpolate,
    Log,
    Max,
    Memory,
    Min,
    OscillateSaw,
    OscillateWave,
    Product,
    Sigmoid,
    SignOf,
    Sin,
    Smooth,
    Sum,
    SumThreshold
}

public abstract class NeuronBase : ISignalEmitter, ISignalReceiver
{
    protected readonly int numberOfInputs;
    private ISignalEmitter[] inputs;
    public ISignalEmitter[] Inputs
    {
        get { return inputs; }
        set
        {
            if (value.Length == numberOfInputs)
                inputs = value;
            else
                throw new System.ArgumentOutOfRangeException("Expected " + numberOfInputs.ToString() + " inputs");
        }
    }
    private float?[] inputOverrides;
    public float?[] InputOverrides
    {
        get { return inputOverrides; }
        set
        {
            if (value.Length == numberOfInputs)
                inputOverrides = value;
            else
                throw new System.ArgumentOutOfRangeException("Expected " + numberOfInputs.ToString() + " input overrides");
        }
    }
    private float[] weights;
    public float[] Weights
    {
        get { return weights; }
        set
        {
            if (value.Length == numberOfInputs)
                weights = value;
            else
                throw new System.ArgumentOutOfRangeException("Expected " + numberOfInputs.ToString() + " weights");
        }
    }
    public float[] WeightedInputValues
    {
        get { return Inputs.Select((input, i) => (inputOverrides[i] ?? input.OutputValue) * Weights[0]).ToArray(); }
    }
    public float OutputValue { get; set; }
    private float nextOutputValue;

    protected NeuronBase(int numberOfInputs)
    {
        this.numberOfInputs = numberOfInputs;
        Inputs = new ISignalEmitter[numberOfInputs];
        InputOverrides = new float?[numberOfInputs];
        Weights = new float[numberOfInputs];
    }

    public static NeuronBase CreateNeuron(NeuronType neuronType, float[] weights)
    {
        NeuronBase neuron;
        switch (neuronType)
        {
            case NeuronType.Abs:
                neuron = new AbsNeuron();
                break;
            case NeuronType.Atan:
                neuron = new AtanNeuron();
                break;
            case NeuronType.Cos:
                neuron = new CosNeuron();
                break;
            // case NeuronType.Differentiate:
            //     neuron = new DifferentiateNeuron();
            //     break;
            case NeuronType.Divide:
                neuron = new DivideNeuron();
                break;
            case NeuronType.Expt:
                neuron = new ExptNeuron();
                break;
            case NeuronType.GreaterThan:
                neuron = new GreaterThanNeuron();
                break;
            case NeuronType.If:
                neuron = new AbsNeuron();
                break;
            // case NeuronType.Integrate:
            //     neuron = new IntegrateNeuron();
            //     break;
            case NeuronType.Interpolate:
                neuron = new InterpolateNeuron();
                break;
            case NeuronType.Log:
                neuron = new LogNeuron();
                break;
            case NeuronType.Max:
                neuron = new MaxNeuron();
                break;
            case NeuronType.Memory:
                neuron = new MemoryNeuron();
                break;
            case NeuronType.Min:
                neuron = new MinNeuron();
                break;
            case NeuronType.OscillateSaw:
                neuron = new OscillateSawNeuron();
                break;
            case NeuronType.OscillateWave:
                neuron = new OscillateWaveNeuron();
                break;
            case NeuronType.Product:
                neuron = new ProductNeuron();
                break;
            case NeuronType.Sigmoid:
                neuron = new SigmoidNeuron();
                break;
            case NeuronType.SignOf:
                neuron = new SignOfNeuron();
                break;
            case NeuronType.Sin:
                neuron = new SinNeuron();
                break;
            case NeuronType.Smooth:
                neuron = new SmoothNeuron();
                break;
            case NeuronType.Sum:
                neuron = new SumNeuron();
                break;
            case NeuronType.SumThreshold:
                neuron = new SumThresholdNeuron();
                break;
            default:
                throw new System.ArgumentException("Unknown neuron type '" + neuronType + "'");
        }
        neuron.Weights = weights;
        return neuron;
    }

    protected abstract float Evaluate();

    // Signal propagation is split into two phases, allowing us to wait
    // until all neurons have computed their next state before modifying
    // neuron outputs.
    public void PropagatePhaseOne()
    {
        nextOutputValue = Evaluate();
    }

    public void PropagatePhaseTwo()
    {
        OutputValue = nextOutputValue;
    }
}
