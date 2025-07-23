namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    using Genotype;

    public class Neuron : ISignalReceiver, ISignalEmitter
    {
        public readonly ActivationFunction ActivationFunction;

        public SignalInput InputA { get; set; }
        public SignalInput InputB { get; set; }
        public SignalInput InputC { get; set; }

        public Neuron(ActivationFunction activationFunction)
        {
            ActivationFunction = activationFunction;
        }
    }
}
