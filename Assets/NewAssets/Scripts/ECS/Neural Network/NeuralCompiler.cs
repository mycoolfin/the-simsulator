using System;
using Unity.Collections;
using Unity.Entities;
using mycoolfin.TheSimsulator.Sims.Phenotype;
using mycoolfin.TheSimsulator.Sims.Genotype;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.NeuralNetwork
{
    public static class NeuralCompiler
    {
        const int MAX_INPUTS_PER_NEURON = 3;

        public static BlobAssetReference<CompiledNeuralGraph> Compile(SimsPhenotype phenotype, BlobBuilder builder)
        {
            ref CompiledNeuralGraph root = ref builder.ConstructRoot<CompiledNeuralGraph>();

            int limbCount = phenotype.Limbs.Count;
            BlobBuilderArray<CompiledNeuralGraph.ArraySlice> sensorSlices = builder.Allocate(ref root.SensorSlices, limbCount);
            BlobBuilderArray<CompiledNeuralGraph.ArraySlice> actuatorSlices = builder.Allocate(ref root.ActuatorSlices, limbCount);
            BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices = builder.Allocate(ref root.LimbNeuronSlices, limbCount);
            ushort sensorSliceStartIndex = 0;
            ushort actuatorSliceStartIndex = 0;
            ushort limbNeuronSliceStartIndex = 0;
            for (int i = 0; i < limbCount; i++)
            {
                mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb = phenotype.Limbs[i];
                ushort sensorCount = (ushort)limb.Sensors.Count;
                ushort actuatorCount = (ushort)limb.Actuators.Count;
                ushort limbNeuronCount = (ushort)limb.Neurons.Count;
                sensorSlices[i] = new()
                {
                    startIndex = sensorSliceStartIndex,
                    count = sensorCount
                };
                sensorSliceStartIndex += sensorCount;
                actuatorSlices[i] = new()
                {
                    startIndex = actuatorSliceStartIndex,
                    count = actuatorCount
                };
                actuatorSliceStartIndex += actuatorCount;
                limbNeuronSlices[i] = new()
                {
                    startIndex = limbNeuronSliceStartIndex,
                    count = limbNeuronCount
                };
                limbNeuronSliceStartIndex += limbNeuronCount;
            }

            ushort totalSensorCount = sensorSlices.Length == 0 ? (ushort)0 : (ushort)(sensorSlices[^1].startIndex + sensorSlices[^1].count);
            ushort totalActuatorCount = actuatorSlices.Length == 0 ? (ushort)0 : (ushort)(actuatorSlices[^1].startIndex + actuatorSlices[^1].count);
            ushort totalLimbNeuronCount = limbNeuronSlices.Length == 0 ? (ushort)0 : (ushort)(limbNeuronSlices[^1].startIndex + limbNeuronSlices[^1].count);
            ushort totalBrainNeuronCount = (ushort)phenotype.Brain.Neurons.Count;
            ushort totalReceiverCount = (ushort)(totalActuatorCount + totalLimbNeuronCount + totalBrainNeuronCount);
            ushort maxInputCount = (ushort)(totalReceiverCount * MAX_INPUTS_PER_NEURON);

            root.SensorCount = totalSensorCount;
            root.ActuatorCount = totalActuatorCount;
            root.LimbNeuronCount = totalLimbNeuronCount;
            root.BrainNeuronCount = totalBrainNeuronCount;

            BlobBuilderArray<CompiledNeuralGraph.SensorMeta> sensors = builder.Allocate(ref root.Sensors, totalSensorCount);
            BlobBuilderArray<CompiledNeuralGraph.ActuatorMeta> actuators = builder.Allocate(ref root.Actuators, totalActuatorCount);
            BlobBuilderArray<CompiledNeuralGraph.NeuronMeta> limbNeurons = builder.Allocate(ref root.LimbNeurons, totalLimbNeuronCount);
            BlobBuilderArray<CompiledNeuralGraph.NeuronMeta> brainNeurons = builder.Allocate(ref root.BrainNeurons, totalBrainNeuronCount);
            BlobBuilderArray<CompiledNeuralGraph.InputMeta> inputs = builder.Allocate(ref root.Inputs, maxInputCount);

            ushort sensorIndex = 0;
            ushort actuatorIndex = 0;
            ushort limbNeuronIndex = 0;
            ushort brainNeuronIndex = 0;
            ushort inputIndex = 0;

            for (byte i = 0; i < limbCount; i++)
            {
                mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb = phenotype.Limbs[i];

                // Limb sensors.
                foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Sensor sensor in limb.Sensors)
                {
                    sensors[sensorIndex++] = new()
                    {
                        type = sensor.Type
                    };
                }

                // Limb actuators.
                foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Actuator actuator in limb.Actuators)
                {
                    // Create a neuron meta for this actuator.
                    CompiledNeuralGraph.NeuronMeta neuronMeta = CreateNeuronMeta(
                        actuator, mycoolfin.TheSimsulator.Sims.Genotype.ActivationFunction.Sum, inputs, ref inputIndex,
                        totalSensorCount, totalActuatorCount, totalLimbNeuronCount, totalBrainNeuronCount,
                        sensorSlices, limbNeuronSlices
                    );

                    // Create an actuator meta.
                    actuators[actuatorIndex++] = new()
                    {
                        type = actuator.Type,
                        neuronMeta = neuronMeta
                    };
                }

                // Limb neurons.
                foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Neuron neuron in limb.Neurons)
                {
                    CompiledNeuralGraph.NeuronMeta neuronMeta = CreateNeuronMeta(
                        neuron, neuron.ActivationFunction, inputs, ref inputIndex,
                        totalSensorCount, totalActuatorCount, totalLimbNeuronCount, totalBrainNeuronCount,
                        sensorSlices, limbNeuronSlices
                    );
                    limbNeurons[limbNeuronIndex++] = neuronMeta;
                }
            }

            // Brain neurons.
            foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Neuron neuron in phenotype.Brain.Neurons)
            {
                CompiledNeuralGraph.NeuronMeta neuronMeta = CreateNeuronMeta(
                    neuron, neuron.ActivationFunction, inputs, ref inputIndex,
                    totalSensorCount, totalActuatorCount, totalLimbNeuronCount, totalBrainNeuronCount,
                    sensorSlices, limbNeuronSlices
                );
                brainNeurons[brainNeuronIndex++] = neuronMeta;
            }

            return builder.CreateBlobAssetReference<CompiledNeuralGraph>(Allocator.Persistent);
        }

        private static CompiledNeuralGraph.NeuronMeta CreateNeuronMeta(
            ISignalReceiver receiver, ActivationFunction activationFunction,
            BlobBuilderArray<CompiledNeuralGraph.InputMeta> inputs, ref ushort inputIndex,
            ushort totalSensorCount, ushort totalActuatorCount, ushort totalLimbNeuronCount, ushort totalBrainNeuronCount,
            BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbSensorSlices, BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices
        )
        {
            ushort startInputIndex = inputIndex;
            byte actualInputs = CreateInputMetas(receiver, inputs, ref inputIndex, totalSensorCount, totalActuatorCount, totalLimbNeuronCount, totalBrainNeuronCount, limbSensorSlices, limbNeuronSlices);
            return new()
            {
                firstInput = startInputIndex,
                inputCount = actualInputs,
                activationFunction = activationFunction
            };
        }

        private static byte CreateInputMetas(ISignalReceiver receiver, BlobBuilderArray<CompiledNeuralGraph.InputMeta> inputs, ref ushort inputIndex, ushort totalSensorCount, ushort totalActuatorCount, ushort totalLimbNeuronCount, ushort totalBrainNeuronCount, BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbSensorSlices, BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices)
        {
            byte actualInputs = 0;
            for (int k = 0; k < MAX_INPUTS_PER_NEURON; k++)
            {
                SignalInput srcInput = k == 0 ? receiver.InputA : k == 1 ? receiver.InputB : receiver.InputC;
                if (srcInput.Port == AbsoluteSignalPort.Disconnected)
                    continue; // Skip disconnected inputs.
                ushort globalEmitterIndex = srcInput.Port switch
                {
                    AbsoluteSignalPort.Bias => CompiledNeuralGraph.BIAS_INDEX,
                    AbsoluteSignalPort.Brain => GetEmitterIndexInBrain(srcInput.SlotIndex, totalSensorCount, totalActuatorCount, totalLimbNeuronCount, totalBrainNeuronCount),
                    AbsoluteSignalPort.Limb => GetEmitterIndexInLimb(srcInput.SlotIndex, totalSensorCount, totalActuatorCount, limbSensorSlices[srcInput.LimbIndex], limbNeuronSlices[srcInput.LimbIndex]),
                    _ => throw new ArgumentOutOfRangeException(nameof(srcInput.Port), "Unknown signal port."),
                };
                inputs[inputIndex++] = new()
                {
                    sourceEmitterGlobalIndex = globalEmitterIndex,
                    weight = srcInput.Weight
                };
                actualInputs++;
            }
            return actualInputs;
        }

        // Emitter indexing uses [...sensors, ...actuatorNeurons, ...limbNeurons, ...brainNeurons].
        private static ushort GetEmitterIndexInBrain(int slotIndex, int totalSensorCount, int totalActuatorCount, int totalLimbNeuronCount, int totalBrainNeuronCount)
        {
            if (slotIndex < 0 || slotIndex >= totalBrainNeuronCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index out of range. Index: " + slotIndex + ", Count: " + totalBrainNeuronCount);
            int emitterIndex = (ushort)(totalSensorCount + totalActuatorCount + totalLimbNeuronCount + slotIndex);
            int totalCount = totalSensorCount + totalActuatorCount + totalLimbNeuronCount + totalBrainNeuronCount;
            if (emitterIndex >= totalCount)
                throw new ArgumentOutOfRangeException(nameof(emitterIndex), "Emitter index out of range. Index: " + emitterIndex + ", Count: " + totalCount);
            return (ushort)emitterIndex;
        }
        private static ushort GetEmitterIndexInLimb(int slotIndex, int totalSensorCount, int totalActuatorCount, CompiledNeuralGraph.ArraySlice sensorSlice, CompiledNeuralGraph.ArraySlice limbNeuronSlice)
        {
            ushort limbSensorCount = sensorSlice.count;
            ushort limbNeuronCount = limbNeuronSlice.count;
            if (slotIndex < limbSensorCount) // It's a sensor.
            {
                return (ushort)(sensorSlice.startIndex + slotIndex);
            }
            else if (slotIndex < limbSensorCount + limbNeuronCount) // It's a limb neuron.
            {
                return (ushort)(totalSensorCount + totalActuatorCount + limbNeuronSlice.startIndex + slotIndex - limbSensorCount);
            }
            else
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index out of range. Index: " + slotIndex + ", Count: " + (limbSensorCount + limbNeuronCount));
        }
    }
}
