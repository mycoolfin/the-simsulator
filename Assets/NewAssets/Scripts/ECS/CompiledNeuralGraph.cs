using Unity.Entities;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;

public struct CompiledNeuralGraph
{
    public const ushort BIAS_INDEX = ushort.MaxValue;

    public BlobArray<SensorMeta> Sensors;
    public BlobArray<NeuronMeta> Neurons;
    public BlobArray<ActuatorMeta> Actuators;
    public BlobArray<InputMeta> Inputs;
    public BlobArray<ArraySlice> LimbSensorSlices; // Sensor slices for each limb.
    public BlobArray<ArraySlice> LimbNeuronSlices; // Neuron slices for each limb.
    public BlobArray<ArraySlice> LimbActuatorSlices; // Actuator slices for each limb.

    public ushort SensorCount;
    public ushort NeuronCount;
    public ushort ActuatorCount;

    public struct SensorMeta
    {
        public mycoolfin.TheSimsulator.Sims.Phenotype.SensorType type;
    }

    public struct NeuronMeta
    {
        public ushort firstInput;
        public byte inputCount;
        public ActivationFunction activationFunction;
    }

    public struct ActuatorMeta
    {
        public ushort neuronGlobalIndex;
        public ActuatorType type;
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

    public ushort GetEmitterIndexOfSensor(int limbIndex, mycoolfin.TheSimsulator.Sims.Phenotype.SensorType sensorType, ushort limbSensorIndex)
    {
        ArraySlice sensorsSlice = LimbSensorSlices[limbIndex];
        for (ushort i = sensorsSlice.startIndex; i < sensorsSlice.startIndex + sensorsSlice.count; i++)
        {
            SensorMeta sensorMeta = Sensors[i];
            if (sensorMeta.type == sensorType)
                if (limbSensorIndex-- == 0)
                    return i;
        }
        return ushort.MaxValue; // Not found.
    }

    public ushort GetEmitterIndexOfActuatorNeuron(int limbIndex, ActuatorType actuatorType, ushort limbActuatorIndex)
    {
        ArraySlice actuatorsSlice = LimbActuatorSlices[limbIndex];
        for (ushort i = actuatorsSlice.startIndex; i < actuatorsSlice.startIndex + actuatorsSlice.count; i++)
        {
            ActuatorMeta actuatorMeta = Actuators[i];
            if (actuatorMeta.type == actuatorType)
                if (limbActuatorIndex-- == 0)
                    return actuatorMeta.neuronGlobalIndex;
        }
        return ushort.MaxValue; // Not found.
    }
}
