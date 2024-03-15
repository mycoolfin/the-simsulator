using System.Linq;
using System.Collections.Generic;

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
        List<SignalEmitter> brainEmitters = brain.GetSignalEmitters();

        brain.GetSignalReceivers().ForEach(receiver =>
        {
            for (int inputSlot = 0; inputSlot < receiver.Inputs.Length; inputSlot++)
            {
                // Set input weight.
                receiver.Weights[inputSlot] = receiver.InputDefinitions[inputSlot].Weight;

                // Connect this input slot to its specified signal emitter.
                EmitterSetLocation emitterSetLocation = receiver.InputDefinitions[inputSlot].EmitterSetLocation;
                string instanceId = receiver.InputDefinitions[inputSlot].InstanceId;
                int emitterIndex = receiver.InputDefinitions[inputSlot].EmitterIndex;
                switch (emitterSetLocation)
                {
                    case EmitterSetLocation.None:
                        break;
                    case EmitterSetLocation.Brain:
                        ConnectInBrain(receiver, inputSlot, brainEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.LimbInstances:
                        Limb limb = limbs.Find(l => l.instanceId == instanceId);
                        ConnectInLimbInstance(receiver, inputSlot, limb?.GetSignalEmitters(), emitterIndex);
                        break;
                }
            }
        });
    }

    private static void ConfigureLimbNervousSystem(Limb limb, Brain brain)
    {
        List<SignalEmitter> attachedEmitters = new();

        List<SignalEmitter> sameLimbEmitters = limb.GetSignalEmitters();
        List<SignalEmitter> brainEmitters = brain.GetSignalEmitters();
        List<SignalEmitter> parentLimbEmitters = limb.parentLimb != null ? limb.parentLimb.GetSignalEmitters() : null;
        List<List<SignalEmitter>> emittersPerChild = limb.childLimbs.Select(childLimb => childLimb?.GetSignalEmitters()).ToList();

        limb.GetSignalReceivers().ForEach(receiver =>
        {
            for (int inputSlot = 0; inputSlot < receiver.Inputs.Length; inputSlot++)
            {
                // Set input weight.
                receiver.Weights[inputSlot] = receiver.InputDefinitions[inputSlot].Weight;

                // Connect this input slot to its specified signal emitter.
                EmitterSetLocation emitterSetLocation = receiver.InputDefinitions[inputSlot].EmitterSetLocation;
                int childLimbIndex = receiver.InputDefinitions[inputSlot].ChildLimbIndex;
                int emitterIndex = receiver.InputDefinitions[inputSlot].EmitterIndex;
                switch (emitterSetLocation)
                {
                    case EmitterSetLocation.None:
                        break;
                    case EmitterSetLocation.SameLimb:
                        ConnectInSameLimb(receiver, inputSlot, sameLimbEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.Brain:
                        ConnectInBrain(receiver, inputSlot, brainEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.ParentLimb:
                        ConnectInParentLimb(receiver, inputSlot, parentLimbEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.ChildLimbs:
                        ConnectInChildLimb(receiver, inputSlot, emittersPerChild, childLimbIndex, emitterIndex, limb);
                        break;
                }
            }
        });
    }

    private static void ConnectInSameLimb(SignalReceiver receiver, int inputSlot, List<SignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null)
            throw new System.Exception("THIS SHOULD NOT HAPPEN!");

        if (emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void ConnectInBrain(SignalReceiver receiver, int inputSlot, List<SignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null || emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void ConnectInParentLimb(SignalReceiver receiver, int inputSlot, List<SignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null || emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void ConnectInChildLimb(SignalReceiver receiver, int inputSlot, List<List<SignalEmitter>> emittersPerChild, int childLimbIndex, int emitterIndex, Limb limb)
    {
        if (emittersPerChild == null || childLimbIndex >= emittersPerChild.Count || emittersPerChild[childLimbIndex] == null 
            || emitterIndex >= emittersPerChild[childLimbIndex].Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emittersPerChild[childLimbIndex][emitterIndex]);
    }

    private static void ConnectInLimbInstance(SignalReceiver receiver, int inputSlot, List<SignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null || emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void Connect(SignalReceiver receiver, int inputSlot, SignalEmitter emitter)
    {
        if (emitter == null)
            receiver.Weights[inputSlot] = 0f;
        else
        {
            receiver.Inputs[inputSlot] = emitter;
            emitter.SetDisabled(false);
        }
    }
}
