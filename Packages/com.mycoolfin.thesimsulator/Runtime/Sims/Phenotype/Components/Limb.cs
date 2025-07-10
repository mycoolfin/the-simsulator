using System;
using System.Numerics;
using System.Collections.Generic;

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

        public event Action OnTransformChanged;

        // DEBUG.
        public readonly bool DebugMirroredX;
        public readonly bool DebugMirroredY;
        public readonly bool DebugMirroredZ;
        public readonly Vector4 Color;

        public Limb(Vector3 dimensions, bool mirroredX, bool mirroredY, bool mirroredZ)
        {
            Dimensions = dimensions;
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Joint = null;

            Neurons = new();
            Sensors = new();
            Actuators = new();

            DebugMirroredX = mirroredX;
            DebugMirroredY = mirroredY;
            DebugMirroredZ = mirroredZ;

            Color = new Vector4(DebugMirroredX ? 1f : 0f,
                                DebugMirroredY ? 1f : 0f,
                                DebugMirroredZ ? 1f : 0f,
                                1f);
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
    }
}
