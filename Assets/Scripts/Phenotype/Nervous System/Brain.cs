using System.Linq;

public class Brain
{
    public NeuronBase[] neurons;

    public Brain(NeuronNode[] neuronNodes, Limb[] limbs)
    {
        neurons = new NeuronBase[neuronNodes.Length];
        float[][] neuronInputPreferences = new float[neuronNodes.Length][];
        for (int i = 0; i < neuronNodes.Length; i++)
        {
            neurons[i] = NeuronBase.CreateNeuron(neuronNodes[i].type, neuronNodes[i].inputWeights);
            neuronInputPreferences[i] = neuronNodes[i].inputPreferences;
        }
        ConfigureBrainNeurons(limbs, neuronInputPreferences);
    }

    public void ConfigureBrainNeurons(Limb[] limbs, float[][] inputPreferences)
    {
        // The pool of emitters to choose from.
        // Brain signal receivers can connect to signal emitters located anywhere.
        ISignalEmitter[] emitterPool = neurons
        .Concat(limbs.SelectMany(limb => limb.neurons))
        .Concat(limbs.Where(limb => limb.joint != null).SelectMany(limb => limb.joint.sensors as ISignalEmitter[]))
        .ToArray();

        NervousSystem.ConfigureSignalReceivers(neurons, inputPreferences, emitterPool);
    }
}