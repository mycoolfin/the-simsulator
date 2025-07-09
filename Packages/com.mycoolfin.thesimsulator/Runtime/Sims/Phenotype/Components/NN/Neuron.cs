namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class Neuron : ISignalReceiver, ISignalEmitter
    {
        public readonly Genotype.ActivationFunction ActivationFunction;

        public SignalInput InputA { get; set; }
        public SignalInput InputB { get; set; }
        public SignalInput InputC { get; set; }

        public Neuron(Genotype.ActivationFunction activationFunction)
        {
            ActivationFunction = activationFunction;
        }
    }
}
