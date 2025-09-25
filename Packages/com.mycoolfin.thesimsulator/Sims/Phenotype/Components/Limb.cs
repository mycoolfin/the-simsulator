using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class Limb : INeuronContainer, ISensorContainer, IActuatorContainer
    {
        public Vector3 Dimensions { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Joint Joint { get; private set; }
        public ContactSensorArray ContactSensors { get; private set; }
        public LightSensorArray LightSensors { get; private set; }

        public float Mass => Dimensions.X * Dimensions.Y * Dimensions.Z; // Mass is proportional to volume.

        private readonly List<Neuron> neurons;
        public IEnumerable<Neuron> Neurons => neurons;
        public IEnumerable<Sensor> Sensors => ContactSensors.Sensors.Concat(LightSensors.Sensors).Concat(Joint != null ? Joint.Sensors : Enumerable.Empty<Sensor>());
        public IEnumerable<Actuator> Actuators => Joint != null ? Joint.Actuators : Enumerable.Empty<Actuator>();

        public Vector4 Color { get; private set; }

        public event Action OnTransformChanged;

        public Limb(Vector3 dimensions)
        {
            Dimensions = dimensions;
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Joint = null;
            ContactSensors = new();
            LightSensors = new();

            neurons = new();
        }

        public void SetJoint(Joint joint)
        {
            Joint = joint;
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
        
        public void SetColor(Vector4 color)
        {
            Color = color;
        }
    }
}
