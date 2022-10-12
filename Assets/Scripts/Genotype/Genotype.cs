
public class Genotype
{
    public readonly NeuronNode[] brainNeuronNodes;
    public readonly LimbNode[] limbNodes;

    public Genotype(LimbNode[] limbNodes, NeuronNode[] brainNeuronNodes)
    {
        if (limbNodes?.Length == 0)
            throw new System.ArgumentException("Genotype cannot be specified without limbs");

        this.limbNodes = limbNodes;
        this.brainNeuronNodes = brainNeuronNodes == null ? new NeuronNode[0] : brainNeuronNodes;
    }
}
