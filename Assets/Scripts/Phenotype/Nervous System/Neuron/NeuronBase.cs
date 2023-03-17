using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

public abstract class NeuronBase : ISignalEmitter, ISignalReceiver
{
    protected abstract NeuronType TypeOfNeuron { get; }
    private ReadOnlyCollection<InputDefinition> inputDefinitions;
    public ReadOnlyCollection<InputDefinition> InputDefinitions
    {
        get { return inputDefinitions; }
        set
        {
            if (value.Count == TypeOfNeuron.NumberOfInputs())
                inputDefinitions = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfNeuron.NumberOfInputs().ToString() + " input definitions, got " + value.Count);
        }
    }
    private List<ISignalEmitter> inputs;
    public List<ISignalEmitter> Inputs
    {
        get { return inputs; }
        set
        {
            if (value.Count == TypeOfNeuron.NumberOfInputs())
                inputs = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfNeuron.NumberOfInputs().ToString() + " inputs, got " + value.Count);
        }
    }
    private List<float?> inputOverrides;
    public List<float?> InputOverrides
    {
        get { return inputOverrides; }
        set
        {
            if (value.Count == TypeOfNeuron.NumberOfInputs())
                inputOverrides = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfNeuron.NumberOfInputs().ToString() + " input overrides, got " + value.Count);
        }
    }
    private List<float> weights;
    public List<float> Weights
    {
        get { return weights; }
        set
        {
            if (value.Count == TypeOfNeuron.NumberOfInputs())
                weights = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfNeuron.NumberOfInputs().ToString() + " weights, got " + value.Count);
        }
    }
    public List<float> GetWeightedInputValues()
    {
        return Inputs.Select((input, i) => (inputOverrides[i] ?? input.OutputValue) * Weights[0]).ToList();
    }
    public float OutputValue { get; set; }
    private float nextOutputValue;

    protected NeuronBase()
    {
        InputDefinitions = new InputDefinition[TypeOfNeuron.NumberOfInputs()].ToList().AsReadOnly();
        Inputs = new ISignalEmitter[TypeOfNeuron.NumberOfInputs()].ToList();
        InputOverrides = new float?[TypeOfNeuron.NumberOfInputs()].ToList();
        Weights = new float[TypeOfNeuron.NumberOfInputs()].ToList();
    }

    public static NeuronBase CreateNeuron(NeuronDefinition neuronDefinition)
    {
        NeuronBase neuron;
        switch (neuronDefinition.Type)
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
            case NeuronType.Differentiate:
                neuron = new DifferentiateNeuron();
                break;
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
                neuron = new IfNeuron();
                break;
            case NeuronType.Integrate:
                neuron = new IntegrateNeuron();
                break;
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
                throw new System.ArgumentException("Unknown neuron type '" + neuronDefinition.Type + "'");
        }
        neuron.inputDefinitions = neuronDefinition.InputDefinitions;
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
