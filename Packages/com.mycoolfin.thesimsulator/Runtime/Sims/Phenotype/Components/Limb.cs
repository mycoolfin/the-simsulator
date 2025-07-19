using System;
using System.Numerics;
using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class Limb
    {
        public Vector3 Dimensions { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Joint Joint { get; private set; }
        public List<Neuron> Neurons { get; private set; }

        public float Mass => Dimensions.X * Dimensions.Y * Dimensions.Z; // Mass is proportional to volume.

        public List<Sensor> Sensors { get; private set; }
        public List<Actuator> Actuators { get; private set; }

        public Vector4 Color { get; private set; }

        public event Action OnTransformChanged;

        public Limb(Vector3 dimensions)
        {
            Dimensions = dimensions;
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Joint = null;

            Neurons = new();
            Sensors = new();
            Actuators = new();
        }

        public void SetJoint(Joint joint)
        {
            Joint = joint;
            if (joint != null)
            {
                Sensors.AddRange(joint.Sensors);
                Actuators.AddRange(joint.Actuators);
            }
            else
            {
                Sensors.Clear();
                Actuators.Clear();
            }
        }

        public void SetNeurons(List<Neuron> neurons)
        {
            Neurons.Clear();
            if (neurons != null)
                Neurons.AddRange(neurons);
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
