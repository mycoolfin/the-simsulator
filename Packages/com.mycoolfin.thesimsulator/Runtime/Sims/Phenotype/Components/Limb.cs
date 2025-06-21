using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Limb : ISensorContainer, IActuatorContainer, INeuronContainer
    {
        public Matrix4x4 TransformMatrix { get; private set; } // World space.
        public Vector3 Dimensions => TransformMatrix.GetScale();
        public Vector3 Position => TransformMatrix.GetTranslation();
        public Quaternion Rotation => TransformMatrix.GetRotation();
        public JointBase Joint { get; private set; }
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

        public Limb(Vector3 dimensions)
        {
            TransformMatrix = Matrix4x4.TRS(Vector3.Zero, Quaternion.Identity, dimensions);
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
            TransformMatrix = Matrix4x4.TRS(position, rotation, TransformMatrix.GetScale());
            OnTransformChanged?.Invoke();
        }
    }
}
