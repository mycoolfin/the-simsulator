using System;
using System.Numerics;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Limb : ISensorContainer, IActuatorContainer, INeuronContainer
    {
        public Vector3 Dimensions { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Joint Joint { get; private set; }
        private readonly List<Neuron> neurons = new();

        public float Mass => Dimensions.X * Dimensions.Y * Dimensions.Z; // Mass is proportional to volume.

        public IEnumerable<SensorBase> Sensors => Joint?.Sensors ?? Array.Empty<SensorBase>();
        public IEnumerable<ActuatorBase> Actuators => Joint?.Actuators ?? Array.Empty<ActuatorBase>();
        public IEnumerable<Neuron> Neurons => neurons;

        public event Action OnTransformChanged;

        // DEBUG.
        public bool debugMirroredX;
        public bool debugMirroredY;
        public bool debugMirroredZ;
        public bool debugSwappedX;
        public Vector4 Color;

        public Limb(Vector3 dimensions)
        {
            Dimensions = dimensions;
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Joint = null;
        }

        public void SetJoint(Joint joint)
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
            OnTransformChanged?.Invoke();
        }
    }
}
