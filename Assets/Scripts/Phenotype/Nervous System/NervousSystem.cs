using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NervousSystem
{
    public static void Configure(Brain brain, List<Limb> limbs)
    {
        ConfigureBrainNeurons(brain, limbs);
        foreach (Limb limb in limbs)
            ConfigureLimbNervousSystem(limb, brain);
    }

    private static void ConfigureBrainNeurons(Brain brain, List<Limb> limbs)
    {
        // The pool of emitters to choose from.
        // Brain signal receivers can connect to signal emitters located anywhere.
        List<ISignalEmitter> emitterPool = new List<ISignalEmitter>()
        .Concat(brain.neurons)
        .Concat(limbs.Where(limb => limb.joint != null).SelectMany(limb => limb.joint.sensors))
        .Concat(limbs.SelectMany(limb => limb.neurons))
        .ToList();

        NervousSystem.ConfigureSignalReceivers(brain.neurons.Cast<ISignalReceiver>().ToList(), brain.neuronInputPreferences, emitterPool);
    }

    private static void ConfigureLimbNervousSystem(Limb limb, Brain brain)
    {
        // The pool of emitters to choose from.
        // Limb signal receivers can connect to signal emitters located in:
        // - This limb
        // - The parent limb
        // - A child limb
        // - The brain
        List<ISignalEmitter> emitterPool = new List<ISignalEmitter>()
        .Concat(limb.joint?.sensors.Cast<ISignalEmitter>() ?? new List<ISignalEmitter>())
        .Concat(limb.neurons)
        .Concat(limb.parentLimb?.joint?.sensors.Cast<ISignalEmitter>() ?? new List<ISignalEmitter>())
        .Concat(limb.parentLimb?.neurons.Cast<ISignalEmitter>() ?? new List<ISignalEmitter>())
        .Concat(brain.neurons)
        .Concat(limb.childLimbs.Where(childLimb => childLimb.joint != null).SelectMany(childLimb => childLimb.joint?.sensors))
        .Concat(limb.childLimbs.Where(childLimb => childLimb.joint != null).SelectMany(childLimb => childLimb.neurons))
        .ToList();

        // The receivers in this limb.
        List<ISignalReceiver> limbSignalReceivers = limb.neurons
        .Concat(limb.joint?.effectors.Cast<ISignalReceiver>() ?? new List<ISignalReceiver>())
        .ToList();

        // The input preferences of each receiver.
        List<List<float>> inputPreferences = limb.neuronInputPreferences
        .Concat(limb.jointEffectorInputPreferences ?? new List<List<float>>())
        .ToList();

        NervousSystem.ConfigureSignalReceivers(limbSignalReceivers, inputPreferences, emitterPool);
    }

    private static void ConfigureSignalReceivers(List<ISignalReceiver> receivers, List<List<float>> inputPreferences, List<ISignalEmitter> emitters)
    {
        for (int receiverIndex = 0; receiverIndex < receivers.Count; receiverIndex++)
        {
            ISignalReceiver receiver = receivers[receiverIndex];
            List<float> receiverInputPreferences = inputPreferences[receiverIndex];
            for (int inputSlotIndex = 0; inputSlotIndex < receiver.Inputs.Count; inputSlotIndex++)
            {
                // Input preferences are in the range (0.0 to 1.0).
                // < switchThreshold -> the receiver will choose from the emitter pool.
                // > switchThreshold -> the receiver will take a constant value.
                float inputPreference = receiverInputPreferences[inputSlotIndex];
                if (inputPreference < NervousSystemParameters.SwitchThreshold)
                {
                    // Map this receiver's input preference to an emitter in the provided selection pool.
                    // That emitter then becomes the receiver's input in this slot.
                    int choiceIndex = Mathf.RoundToInt(Mathf.Lerp(0, emitters.Count - 1, Mathf.InverseLerp(0f, NervousSystemParameters.SwitchThreshold, inputPreference)));
                    receiver.Inputs[inputSlotIndex] = emitters[choiceIndex];
                }
                else
                {
                    float constantValue = Mathf.Lerp(NervousSystemParameters.MinConstantValue, NervousSystemParameters.MaxConstantValue, Mathf.InverseLerp(NervousSystemParameters.SwitchThreshold, 1f, inputPreference));
                    receiver.InputOverrides[inputSlotIndex] = constantValue;
                }
            }
        }
    }
}
