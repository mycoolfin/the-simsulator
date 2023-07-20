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
        List<ISignalEmitter> brainEmitters = brain.GetSignalEmitters();

        brain.GetSignalReceivers().ForEach(receiver =>
        {
            for (int inputSlot = 0; inputSlot < receiver.Inputs.Count; inputSlot++)
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
        List<ISignalEmitter> sameLimbEmitters = limb.GetSignalEmitters();
        List<ISignalEmitter> brainEmitters = brain.GetSignalEmitters();
        List<ISignalEmitter> parentLimbEmitters = limb.parentLimb?.GetSignalEmitters();
        List<List<ISignalEmitter>> emittersPerChild = limb.childLimbs.Select(childLimb => childLimb?.GetSignalEmitters()).ToList();

        limb.GetSignalReceivers().ForEach((System.Action<ISignalReceiver>)(receiver =>
        {
            for (int inputSlot = 0; inputSlot < receiver.Inputs.Count; inputSlot++)
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
        }));

        DisableUnusedEmitters(brainEmitters);
        DisableUnusedEmitters(sameLimbEmitters);
        DisableUnusedEmitters(parentLimbEmitters);
        emittersPerChild.ForEach(childLimbEmitters => DisableUnusedEmitters(childLimbEmitters));
    }

    private static void ConnectInSameLimb(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null)
            throw new System.Exception("THIS SHOULD NOT HAPPEN!");

        if (emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void ConnectInBrain(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null || emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void ConnectInParentLimb(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null || emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void ConnectInChildLimb(ISignalReceiver receiver, int inputSlot, List<List<ISignalEmitter>> emittersPerChild, int childLimbIndex, int emitterIndex, Limb limb)
    {
        if (emittersPerChild == null || childLimbIndex >= emittersPerChild.Count || emittersPerChild[childLimbIndex] == null 
            || emitterIndex >= emittersPerChild[childLimbIndex].Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emittersPerChild[childLimbIndex][emitterIndex]);
    }

    private static void ConnectInLimbInstance(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null || emitterIndex >= emitters.Count)
            Connect(receiver, inputSlot, null);
        else
            Connect(receiver, inputSlot, emitters[emitterIndex]);
    }

    private static void Connect(ISignalReceiver receiver, int inputSlot, ISignalEmitter emitter)
    {
        if (emitter == null)
            receiver.Weights[inputSlot] = 0f;
        else
        {
            receiver.Inputs[inputSlot] = emitter;
            emitter.Consumers.Add(receiver);
        }
    }

    private static void DisableUnusedEmitters(List<ISignalEmitter> emitters)
    {
        HashSet<ISignalEmitter> set = new();
        Queue<ISignalEmitter> queue = new();

        emitters?.ForEach(e => queue.Enqueue(e));

        while (queue.Count > 0)
        {
            ISignalEmitter e = queue.Dequeue();
            if (set.Contains(e))
                continue;
            set.Add(e);
            e.Disabled = e.Consumers.Count == 0 || e.Consumers.All(c => (c as NeuronBase)?.Disabled ?? false);
            (e as NeuronBase)?.Inputs.ForEach(i =>
            {
                if (i != null)
                    queue.Enqueue(i);
            });
        }
    }
}
