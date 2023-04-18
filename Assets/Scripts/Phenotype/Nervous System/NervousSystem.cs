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
        // Only create lists as needed.
        List<ISignalEmitter> brainEmitters = null;

        brain.neurons.ToList()
        .ForEach(receiver =>
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
                        if (brainEmitters == null)
                            brainEmitters = brain.neurons.Cast<ISignalEmitter>().ToList();
                        ConnectInBrain(receiver, inputSlot, brainEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.LimbInstances:
                        Limb limb = limbs.Find(l => l.instanceId == instanceId);
                        ConnectInLimbInstance(receiver, inputSlot, GetLimbSignalEmitters(limb), emitterIndex);
                        break;
                }
            }
        });
    }

    private static void ConfigureLimbNervousSystem(Limb limb, Brain brain)
    {
        // Only create lists as needed.
        List<ISignalEmitter> sameLimbEmitters = null;
        List<ISignalEmitter> brainEmitters = null;
        List<ISignalEmitter> parentLimbEmitters = null;
        List<List<ISignalEmitter>> emittersPerChild = null;

        limb.neurons
        .Concat(limb.joint?.effectors.Cast<ISignalReceiver>() ?? new List<ISignalReceiver>())
        .ToList()
        .ForEach((System.Action<ISignalReceiver>)(receiver =>
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
                        if (sameLimbEmitters == null)
                            sameLimbEmitters = GetLimbSignalEmitters(limb);
                        ConnectInSameLimb(receiver, inputSlot, sameLimbEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.Brain:
                        if (brainEmitters == null)
                            brainEmitters = brain.neurons.Cast<ISignalEmitter>().ToList();
                        ConnectInBrain(receiver, inputSlot, brainEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.ParentLimb:
                        if (parentLimbEmitters == null)
                            parentLimbEmitters = GetLimbSignalEmitters(limb.parentLimb);
                        ConnectInParentLimb(receiver, inputSlot, parentLimbEmitters, emitterIndex);
                        break;
                    case EmitterSetLocation.ChildLimbs:
                        if (emittersPerChild == null)
                            emittersPerChild = limb.childLimbs.Select(childLimb => GetLimbSignalEmitters(childLimb)).ToList();
                        ConnectInChildLimb(receiver, inputSlot, emittersPerChild, childLimbIndex, emitterIndex, limb);
                        break;
                }
            }
        }));
    }

    private static void ConnectInSameLimb(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null)
            throw new System.Exception("THIS SHOULD NOT HAPPEN!");
        else if (emitterIndex >= emitters.Count)
        {
            // Should only occur in a root limb that is missing joint sensors.
            receiver.Weights[inputSlot] = 0f;
        }
        else
            receiver.Inputs[inputSlot] = emitters[emitterIndex];
    }

    private static void ConnectInBrain(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null)
            throw new System.Exception("THIS SHOULD NOT HAPPEN!");
        else if (emitterIndex >= emitters.Count)
            throw new System.Exception("THIS SHOULD NOT HAPPEN! " + emitterIndex + " : " + emitters.Count);
        else
            receiver.Inputs[inputSlot] = emitters[emitterIndex];
    }

    private static void ConnectInParentLimb(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null)
        {
            // Should only occur when the receiver limb is root (and therefore has no parent), and this limb is in a recursive chain.
            receiver.Weights[inputSlot] = 0f;
        }
        else if (emitterIndex >= emitters.Count)
        {
            // Should only occur when the source limb is a root limb that is missing joint sensors.
            receiver.Weights[inputSlot] = 0f;
        }
        else
            receiver.Inputs[inputSlot] = emitters[emitterIndex];
    }

    private static void ConnectInChildLimb(ISignalReceiver receiver, int inputSlot, List<List<ISignalEmitter>> emittersPerChild, int childLimbIndex, int emitterIndex, Limb limb)
    {
        if (emittersPerChild == null)
            throw new System.Exception("THIS SHOULD NOT HAPPEN!");
        else if (childLimbIndex >= emittersPerChild.Count || emittersPerChild[childLimbIndex] == null)
        {
            // The specified child limb was not present. This should only be due to the child limb being...
            //   - in a recursive loop (the limb at the end won't have a child), or
            //   - not in a recursive loop, and marked terminal only, or
            //   - a limb that failed during phenotype creation due to limits / collisions.
            receiver.Weights[inputSlot] = 0f;
        }
        else if (emitterIndex >= emittersPerChild[childLimbIndex].Count)
            throw new System.Exception("THIS SHOULD NOT HAPPEN! " + " childIndex: " + childLimbIndex + " emittersPerChild(" + emittersPerChild.Count + "): " + string.Join(", ", emittersPerChild.Select(e => e?.Count.ToString() ?? "null")) + " emitterIndex: " + emitterIndex);
        else
            receiver.Inputs[inputSlot] = emittersPerChild[childLimbIndex][emitterIndex];
    }

    private static void ConnectInLimbInstance(ISignalReceiver receiver, int inputSlot, List<ISignalEmitter> emitters, int emitterIndex)
    {
        if (emitters == null)
        {
            // Limb instance creation failed.
            receiver.Weights[inputSlot] = 0f;
        }
        else if (emitterIndex >= emitters.Count)
            throw new System.Exception("THIS SHOULD NOT HAPPEN! " + emitterIndex + " : " + emitters.Count);
        else
            receiver.Inputs[inputSlot] = emitters[emitterIndex];
    }

    private static List<ISignalEmitter> GetLimbSignalEmitters(Limb limb)
    {
        return limb?.neurons.Concat(limb.joint?.sensors.Cast<ISignalEmitter>() ?? new List<ISignalEmitter>()).ToList();
    }
}
