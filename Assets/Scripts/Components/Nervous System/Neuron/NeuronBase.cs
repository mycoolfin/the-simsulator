using UnityEngine;
using System.Collections;
using System.Linq;


public abstract class NeuronBase
{
    public NervousSystemConnection[] inputs;
    public NervousSystemConnection[] outputs;

    protected float[] GetInputValues()
    {
        return inputs.Select(input => input.parent.outputValue).ToArray();
    }

    protected float GetWeightedSumOfInputValues()
    {
        return inputs.Aggregate(0f, (sum, input) => sum + (input.parent.outputValue * input.weight));
    }

    public abstract float Evaluate();

    public void Propagate()
    {

    }
}
