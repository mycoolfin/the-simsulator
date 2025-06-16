using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Neuron : ISignalReceiver, ISignalEmitter
    {
        public readonly ActivationFunction ActivationFunction;

        public SignalInput InputA { get; set; }
        public SignalInput InputB { get; set; }
        public SignalInput InputC { get; set; }

        public float Output { get; set; }

        public Neuron(ActivationFunction activationFunction)
        {
            ActivationFunction = activationFunction;
        }
    }
}
