using System;
using Unity.Collections;
using Unity.Entities;
using mycoolfin.TheSimsulator.Sims.Phenotype;
using mycoolfin.TheSimsulator.Sims.Genotype;

public static class NeuralCompiler
{
    const int MAX_INPUTS_PER_NEURON = 3;

    public static BlobAssetReference<CompiledNeuralGraph> Compile(SimsPhenotype phenotype, BlobBuilder builder)
    {
        ref CompiledNeuralGraph root = ref builder.ConstructRoot<CompiledNeuralGraph>();

        int limbCount = phenotype.Limbs.Count;
        BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbSensorSlices = builder.Allocate(ref root.LimbSensorSlices, limbCount);
        BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices = builder.Allocate(ref root.LimbNeuronSlices, limbCount);
        BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbActuatorSlices = builder.Allocate(ref root.LimbActuatorSlices, limbCount);
        ushort sensorSliceStartIndex = 0;
        ushort neuronSliceStartIndex = 0;
        ushort actuatorSliceStartIndex = 0;
        for (int i = 0; i < limbCount; i++)
        {
            mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb = phenotype.Limbs[i];
            ushort sensorCount = (ushort)limb.Sensors.Count;
            ushort neuronCount = (ushort)limb.Neurons.Count;
            ushort actuatorCount = (ushort)limb.Actuators.Count;
            limbSensorSlices[i] = new()
            {
                startIndex = sensorSliceStartIndex,
                count = sensorCount
            };
            sensorSliceStartIndex += sensorCount;
            limbNeuronSlices[i] = new()
            {
                startIndex = neuronSliceStartIndex,
                count = (ushort)(neuronCount + actuatorCount) // Actuators have their own neurons.
            };
            neuronSliceStartIndex += (ushort)(neuronCount + actuatorCount);
            limbActuatorSlices[i] = new()
            {
                startIndex = actuatorSliceStartIndex,
                count = actuatorCount
            };
            actuatorSliceStartIndex += actuatorCount;
        }

        ushort totalSensorCount = (ushort)(limbSensorSlices[^1].startIndex + limbSensorSlices[^1].count);
        ushort totalActuatorCount = (ushort)(limbActuatorSlices[^1].startIndex + limbActuatorSlices[^1].count);
        ushort totalLimbNeuronCount = (ushort)(limbNeuronSlices[^1].startIndex + limbNeuronSlices[^1].count);
        ushort totalNeuronCount = (ushort)(totalLimbNeuronCount + phenotype.Brain.Neurons.Count);
        ushort maxInputCount = (ushort)(totalNeuronCount * MAX_INPUTS_PER_NEURON);

        root.SensorCount = totalSensorCount;
        root.NeuronCount = totalNeuronCount;
        root.ActuatorCount = totalActuatorCount;

        BlobBuilderArray<CompiledNeuralGraph.SensorMeta> sensors = builder.Allocate(ref root.Sensors, totalSensorCount);
        BlobBuilderArray<CompiledNeuralGraph.NeuronMeta> neurons = builder.Allocate(ref root.Neurons, totalNeuronCount);
        BlobBuilderArray<CompiledNeuralGraph.ActuatorMeta> actuators = builder.Allocate(ref root.Actuators, totalActuatorCount);
        BlobBuilderArray<CompiledNeuralGraph.InputMeta> inputs = builder.Allocate(ref root.Inputs, maxInputCount);

        // LAYOUTS:
        // Sensor Metas => [...[limb0.sensors], ...[limb1.sensors], ...]
        // Neuron Metas => [...[limb0.neurons], ...[limb1.neurons], ...[brain.neurons], ...]
        // Actuator Metas => [...[limb0.actuators], ...[limb1.actuators], ...]
        // Input Metas => [...[neuron0.inputs], ...[neuron1.inputs], ...]

        ushort sensorIndex = 0;
        ushort neuronIndex = 0;
        ushort actuatorIndex = 0;
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

            // Limb neurons.
            foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Neuron neuron in limb.Neurons)
            {
                CreateNeuronMeta(
                    neuron, neuron.ActivationFunction, neurons, ref neuronIndex, inputs, ref inputIndex,
                    totalSensorCount, totalLimbNeuronCount, totalNeuronCount, limbSensorSlices, limbNeuronSlices
                );
            }

            // Limb actuators.
            foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Actuator actuator in limb.Actuators)
            {
                // Create a neuron meta for this actuator.
                ushort neuronGlobalIndex = CreateNeuronMeta(
                    actuator, mycoolfin.TheSimsulator.Sims.Genotype.ActivationFunction.Sum,
                    neurons, ref neuronIndex, inputs, ref inputIndex,
                    totalSensorCount, totalLimbNeuronCount, totalNeuronCount, limbSensorSlices, limbNeuronSlices
                );

                // Create an actuator meta, mapped to the neuron meta.
                actuators[actuatorIndex++] = new()
                {
                    neuronGlobalIndex = neuronGlobalIndex,
                    type = actuator.Type
                };
            }
        }

        // Brain neurons.
        foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Neuron neuron in phenotype.Brain.Neurons)
        {
            CreateNeuronMeta(
                neuron, neuron.ActivationFunction, neurons, ref neuronIndex, inputs, ref inputIndex,
                totalSensorCount, totalLimbNeuronCount, totalNeuronCount, limbSensorSlices, limbNeuronSlices
            );
        }

        return builder.CreateBlobAssetReference<CompiledNeuralGraph>(Allocator.Persistent);
    }

    private static ushort CreateNeuronMeta(
        ISignalReceiver neuron, ActivationFunction activationFunction,
        BlobBuilderArray<CompiledNeuralGraph.NeuronMeta> neurons, ref ushort neuronIndex,
        BlobBuilderArray<CompiledNeuralGraph.InputMeta> inputs, ref ushort inputIndex,
        ushort totalSensorCount, ushort totalLimbNeuronCount, ushort totalNeuronCount,
        BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbSensorSlices, BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices
    )
    {
        ushort startInputIndex = inputIndex;
        byte actualInputs = CreateInputMetas(neuron, inputs, ref inputIndex, totalSensorCount, totalLimbNeuronCount, totalNeuronCount, limbSensorSlices, limbNeuronSlices);
        ushort neuronGlobalIndex = neuronIndex; // Capture before incrementing.
        neurons[neuronIndex++] = new()
        {
            firstInput = startInputIndex,
            inputCount = actualInputs,
            activationFunction = activationFunction
        };
        return neuronGlobalIndex;
    }

    private static byte CreateInputMetas(ISignalReceiver receiver, BlobBuilderArray<CompiledNeuralGraph.InputMeta> inputs, ref ushort inputIndex, ushort totalSensorCount, ushort totalLimbNeuronCount, ushort totalNeuronCount, BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbSensorSlices, BlobBuilderArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices)
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
                AbsoluteSignalPort.Brain => GetEmitterIndexInBrain(srcInput.SlotIndex, totalSensorCount, totalLimbNeuronCount),
                AbsoluteSignalPort.Limb => GetEmitterIndexInLimb(srcInput.SlotIndex, totalSensorCount, limbSensorSlices[srcInput.LimbIndex], limbNeuronSlices[srcInput.LimbIndex]),
                _ => throw new ArgumentOutOfRangeException(nameof(srcInput.Port), "Unknown signal port."),
            };
            if (globalEmitterIndex != CompiledNeuralGraph.BIAS_INDEX && globalEmitterIndex >= totalSensorCount + totalNeuronCount)
                throw new ArgumentOutOfRangeException(nameof(globalEmitterIndex), "Global emitter index out of range. Index: " + globalEmitterIndex + ", Count: " + (totalSensorCount + totalNeuronCount));
            inputs[inputIndex++] = new()
            {
                sourceEmitterGlobalIndex = globalEmitterIndex,
                weight = srcInput.Weight
            };
            actualInputs++;
        }
        return actualInputs;
    }

    // Emitter indexing uses [...sensors, ...limbNeurons, ...brainNeurons].
    private static ushort GetEmitterIndexInBrain(int slotIndex, int totalSensorCount, int totalLimbNeuronCount)
    {
        return (ushort)(totalSensorCount + totalLimbNeuronCount + slotIndex);
    }
    private static ushort GetEmitterIndexInLimb(int slotIndex, int totalSensorCount, CompiledNeuralGraph.ArraySlice limbSensorSlice, CompiledNeuralGraph.ArraySlice limbNeuronSlice)
    {
        if (slotIndex < limbSensorSlice.count) // It's a sensor.
        {
            return (ushort)(limbSensorSlice.startIndex + slotIndex);
        }
        else // It's a neuron.
        {
            return (ushort)(totalSensorCount + limbNeuronSlice.startIndex + slotIndex - limbSensorSlice.count);
        }
    }
}
