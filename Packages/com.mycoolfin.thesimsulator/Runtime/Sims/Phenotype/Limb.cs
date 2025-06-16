using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Limb : ISensorContainer, IActuatorContainer, INeuronContainer
    {
        public Vector3 Dimensions { get; private set; }
        public float Mass => Dimensions.X * Dimensions.Y * Dimensions.Z; // Mass is proportional to volume.
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public JointBase Joint { get; private set; }
        private readonly List<Neuron> neurons = new();

        public IEnumerable<SensorBase> Sensors => Joint.Sensors;
        public IEnumerable<ActuatorBase> Actuators => Joint.Actuators;
        public IEnumerable<Neuron> Neurons => neurons;

        public Limb(Vector3 dimensions)
        {
            Dimensions = dimensions;
        }

        public void SetJoint(JointBase joint)
        {
            Joint = joint;
            Joint.OnDispose += () => Joint = null; // Limb joints can break.
        }

        public void SetNeurons(List<Neuron> neurons)
        {
            this.neurons.Clear();
            if (neurons != null)
                this.neurons.AddRange(neurons);
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
