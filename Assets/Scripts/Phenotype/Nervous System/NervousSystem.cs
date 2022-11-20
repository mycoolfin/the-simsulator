using System.Linq;
using UnityEngine;

public static class NervousSystem
{
    public static void Configure(Brain brain, Limb[] limbs)
    {
        ConfigureBrainNeurons(brain, limbs);
        foreach (Limb limb in limbs)
            ConfigureLimbNervousSystem(limb, brain);
    }

    private static void ConfigureBrainNeurons(Brain brain, Limb[] limbs)
    {
        // The pool of emitters to choose from.
        // Brain signal receivers can connect to signal emitters located anywhere.
        ISignalEmitter[] emitterPool = brain.neurons
        .Concat(limbs.SelectMany(limb => limb.neurons))
        .Concat(limbs.Where(limb => limb.joint != null).SelectMany(limb => limb.joint.sensors as ISignalEmitter[]))
        .ToArray();

        NervousSystem.ConfigureSignalReceivers(brain.neurons, brain.neuronInputPreferences, emitterPool);
    }

    private static void ConfigureLimbNervousSystem(Limb limb, Brain brain)
    {
        // The pool of emitters to choose from.
        // Limb signal receivers can connect to signal emitters located in:
        // - This limb
        // - The parent limb
        // - A child limb
        // - The brain
        ISignalEmitter[] emitterPool = (limb.parentLimb?.neurons ?? new ISignalEmitter[0])
        .Concat(limb.neurons)
        .Concat(limb.joint?.sensors ?? new ISignalEmitter[0])
        .Concat(limb.childLimbs.Where(childLimb => childLimb.joint != null).SelectMany(childLimb => childLimb.neurons))
        .Concat(brain.neurons)
        .ToArray();

        // The receivers in this limb.
        ISignalReceiver[] limbSignalReceivers = limb.neurons
        .Concat(limb.joint?.effectors ?? new ISignalReceiver[0])
        .ToArray();

        // The input preferences of each receiver.
        float[][] inputPreferences = limb.neuronInputPreferences
        .Concat(limb.jointEffectorInputPreferences ?? new float[0][])
        .ToArray();

        NervousSystem.ConfigureSignalReceivers(limbSignalReceivers, inputPreferences, emitterPool);
    }

    private static void ConfigureSignalReceivers(ISignalReceiver[] receivers, float[][] inputPreferences, ISignalEmitter[] emitters)
    {
        for (int receiverIndex = 0; receiverIndex < receivers.Length; receiverIndex++)
        {
            ISignalReceiver receiver = receivers[receiverIndex];
            float[] receiverInputPreferences = inputPreferences[receiverIndex];
            for (int inputSlotIndex = 0; inputSlotIndex < receiver.Inputs.Length; inputSlotIndex++)
            {
                // Input preferences are in the range (0.0 to 1.0).
                // < switchThreshold -> the receiver will choose from the emitter pool.
                // > switchThreshold -> the receiver will take a constant value.
                float inputPreference = receiverInputPreferences[inputSlotIndex];
                if (inputPreference < NervousSystemParameters.SwitchThreshold)
                {
                    // Map this receiver's input preference to an emitter in the provided selection pool.
                    // That emitter then becomes the receiver's input in this slot.
                    int choiceIndex = Mathf.RoundToInt(Mathf.Lerp(0, emitters.Length - 1, Mathf.InverseLerp(0f, NervousSystemParameters.SwitchThreshold, inputPreference)));
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
