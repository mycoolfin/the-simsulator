using System.Linq;

public abstract class NeuronBase : NervousSystemNode
{
    public NervousSystemConnection[] inputs;

    protected NeuronBase(int numberOfInputs)
    {
        inputs = new NervousSystemConnection[numberOfInputs];
    }

    protected float[] GetInputValues()
    {
        return inputs.Select(input => input.parent.outputValue).ToArray();
    }

    public abstract float Evaluate();

    public void Propagate()
    {

    }
}
