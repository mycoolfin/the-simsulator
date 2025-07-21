using Unity.Burst;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.NeuralNetwork
{
    using Sims.Genotype;
    using Sims.Phenotype;

    [BurstCompile]
    public struct CompiledNeuralGraph
    {
        public const ushort BIAS_INDEX = ushort.MaxValue;

        public BlobArray<SensorMeta> Sensors;
        public BlobArray<ActuatorMeta> Actuators;
        public BlobArray<NeuronMeta> LimbNeurons;
        public BlobArray<NeuronMeta> BrainNeurons;
        public BlobArray<InputMeta> Inputs;
        public BlobArray<ArraySlice> SensorSlices; // Sensor slices for each limb.
        public BlobArray<ArraySlice> ActuatorSlices; // Actuator slices for each limb.
        public BlobArray<ArraySlice> LimbNeuronSlices; // Neuron slices for each limb.

        public ushort SensorCount;
        public ushort ActuatorCount;
        public ushort LimbNeuronCount;
        public ushort BrainNeuronCount;
        public readonly ushort TotalEmitterCount => (ushort)(SensorCount + ActuatorCount + LimbNeuronCount + BrainNeuronCount);
        public readonly ushort SensorEmitterStartIndex => 0;
        public readonly ushort ActuatorNeuronEmitterStartIndex => SensorCount;
        public readonly ushort LimbNeuronEmitterStartIndex => (ushort)(SensorCount + ActuatorCount);
        public readonly ushort BrainNeuronEmitterStartIndex => (ushort)(SensorCount + ActuatorCount + LimbNeuronCount);

        public struct SensorMeta
        {
            public SensorType type;
        }

        public struct NeuronMeta
        {
            public ushort firstInput;
            public byte inputCount;
            public ActivationFunction activationFunction;
        }

        public struct ActuatorMeta
        {
            public ActuatorType type;
            public NeuronMeta neuronMeta;
        }

        public struct InputMeta
        {
            public ushort sourceEmitterGlobalIndex; // 0..S+N-1.
            public float weight;
        }

        public struct ArraySlice
        {
            public ushort startIndex;
            public ushort count;
        }

        // Emitter buffer is laid out as follows:
        // Sensors: 0..S-1
        // Actuator Neurons: S..S+A-1
        // Limb Neurons: S+A..S+A+L-1
        // Brain Neurons: S+A+L..S+A+L+B-1

        [BurstCompile]
        public ushort GetEmitterIndexOfSensor(int limbIndex, SensorType sensorType, ushort limbSensorIndex)
        {
            if (limbIndex >= 0 && limbIndex < SensorSlices.Length)
            {
                ref readonly ArraySlice sensorsSlice = ref SensorSlices[limbIndex];
                for (ushort i = sensorsSlice.startIndex; i < sensorsSlice.startIndex + sensorsSlice.count; i++)
                {
                    SensorMeta sensorMeta = Sensors[i];
                    if (sensorMeta.type == sensorType)
                        if (limbSensorIndex-- == 0)
                            return (ushort)(SensorEmitterStartIndex + i);
                }
            }
            return ushort.MaxValue; // Not found.
        }

        [BurstCompile]
        public ushort GetEmitterIndexOfActuatorNeuron(int limbIndex, ActuatorType actuatorType, ushort limbActuatorIndex)
        {
            if (limbIndex >= 0 && limbIndex < ActuatorSlices.Length)
            {
                ref readonly ArraySlice actuatorsSlice = ref ActuatorSlices[limbIndex];
                for (ushort i = actuatorsSlice.startIndex; i < actuatorsSlice.startIndex + actuatorsSlice.count; i++)
                {
                    ActuatorMeta actuatorMeta = Actuators[i];
                    if (actuatorMeta.type == actuatorType)
                        if (limbActuatorIndex-- == 0)
                            return (ushort)(ActuatorNeuronEmitterStartIndex + i);
                }
            }
            return ushort.MaxValue; // Not found.
        }
    }
}
