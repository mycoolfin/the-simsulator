using System.Linq;
using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public static class WiringTool
    {
        public static void WireUpNervousSystem(
            Brain brain,
            List<Limb> limbs,
            Dictionary<Limb, List<Limb>> parentToChildLimbsMap,
            Dictionary<Limb, Limb> childToParentLimbMap,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetMap
        )
        {
            foreach (Neuron neuron in brain.Neurons)
            {
                InputSetDefinition inputsDef = receiverToInputDefinitionSetMap[neuron];
                WireReceiver(neuron, inputsDef.A, inputsDef.B, inputsDef.C, limbs, -1, -1, null, brain);
            }
            foreach (Limb thisLimb in limbs)
            {
                childToParentLimbMap.TryGetValue(thisLimb, out Limb parentLimb);
                parentToChildLimbsMap.TryGetValue(thisLimb, out List<Limb> childLimbs);
                int thisLimbIndex = limbs.IndexOf(thisLimb);
                int parentLimbIndex = parentLimb != null ? limbs.IndexOf(parentLimb) : -1;
                List<int> childLimbIndices = childLimbs?.Select(l => limbs.IndexOf(l)).ToList();
                foreach (Neuron neuron in thisLimb.Neurons)
                {
                    InputSetDefinition inputsDef = receiverToInputDefinitionSetMap[neuron];
                    WireReceiver(neuron, inputsDef.A, inputsDef.B, inputsDef.C, limbs, thisLimbIndex, parentLimbIndex, childLimbIndices, brain);
                }
                foreach (Actuator actuator in thisLimb.Actuators)
                {
                    InputSetDefinition inputsDef = receiverToInputDefinitionSetMap[actuator];
                    WireReceiver(actuator, inputsDef.A, inputsDef.B, inputsDef.C, limbs, thisLimbIndex, parentLimbIndex, childLimbIndices, brain);
                }
            }
        }

        private static void WireReceiver(ISignalReceiver receiver, SignalInputDefinition inputA, SignalInputDefinition inputB, SignalInputDefinition inputC, IReadOnlyList<Limb> limbs, int thisLimbIndex, int parentLimbIndex, List<int> childLimbIndices, Brain brain)
        {
            receiver.InputA = WireInput(inputA, limbs, thisLimbIndex, parentLimbIndex, childLimbIndices, brain);
            receiver.InputB = WireInput(inputB, limbs, thisLimbIndex, parentLimbIndex, childLimbIndices, brain);
            receiver.InputC = WireInput(inputC, limbs, thisLimbIndex, parentLimbIndex, childLimbIndices, brain);
        }

        private static SignalInput WireInput(SignalInputDefinition inputDefinition, IReadOnlyList<Limb> limbs, int thisLimbIndex, int parentLimbIndex, IReadOnlyList<int> childLimbIndices, Brain brain)
        {
            RelativeSignalPort relativePort = inputDefinition.SignalEmitterAddress.Port;
            byte slot = inputDefinition.SignalEmitterAddress.Slot; // Maps from [0, 255] to [0, N] available slots for ChildLimb and AnyLimb ports, specific slot otherwise.
            byte limbInstance = inputDefinition.SignalEmitterAddress.LimbInstance; // Maps from [0, 255] to [0, N] available limb instances.

            AbsoluteSignalPort absolutePort = AbsoluteSignalPort.Disconnected;
            int limbIndex = -1;
            int slotIndex = -1;
            switch (relativePort)
            {
                case RelativeSignalPort.Bias:
                    absolutePort = AbsoluteSignalPort.Bias;
                    break;
                case RelativeSignalPort.ThisLimb:
                    if (!IsValidLimbIndex(thisLimbIndex, limbs) || !IsValidSlotIndex(slot, limbs[thisLimbIndex]))
                        break;
                    absolutePort = AbsoluteSignalPort.Limb;
                    limbIndex = thisLimbIndex;
                    slotIndex = slot;
                    break;
                case RelativeSignalPort.ParentLimb:
                    if (!IsValidLimbIndex(parentLimbIndex, limbs) || !IsValidSlotIndex(slot, limbs[parentLimbIndex]))
                        break;
                    absolutePort = AbsoluteSignalPort.Limb;
                    limbIndex = parentLimbIndex;
                    slotIndex = slot;
                    break;
                case RelativeSignalPort.ChildLimb:
                    if (childLimbIndices == null || childLimbIndices.Count == 0)
                        break;
                    int childLimbIndex = childLimbIndices[MapToRange(limbInstance, (uint)childLimbIndices.Count)];
                    Limb childLimb = limbs[childLimbIndex];
                    slot = MapToRange(slot, (uint)(childLimb.Sensors.Count + childLimb.Neurons.Count));
                    if (!IsValidLimbIndex(childLimbIndex, limbs) || !IsValidSlotIndex(slot, limbs[childLimbIndex]))
                        break;
                    absolutePort = AbsoluteSignalPort.Limb;
                    limbIndex = childLimbIndex;
                    slotIndex = slot;
                    break;
                case RelativeSignalPort.AnyLimb:
                    int chosenLimbIndex = MapToRange(limbInstance, (uint)limbs.Count);
                    Limb chosenLimb = limbs[chosenLimbIndex];
                    slot = MapToRange(slot, (uint)(chosenLimb.Sensors.Count + chosenLimb.Neurons.Count));
                    if (!IsValidLimbIndex(chosenLimbIndex, limbs) || !IsValidSlotIndex(slot, limbs[chosenLimbIndex]))
                        break;
                    absolutePort = AbsoluteSignalPort.Limb;
                    limbIndex = chosenLimbIndex;
                    slotIndex = slot;
                    break;
                case RelativeSignalPort.Brain:
                    if (!IsValidSlotIndex(slot, brain))
                        break;
                    absolutePort = AbsoluteSignalPort.Brain;
                    slotIndex = slot;
                    break;
                default:
                    throw new System.NotSupportedException($"Unsupported relative signal port: {relativePort}");
            }

            return new SignalInput(absolutePort, limbIndex, slotIndex, inputDefinition.Weight);
        }

        private static bool IsValidLimbIndex(int index, IReadOnlyList<Limb> limbs)
        {
            return index >= 0 && index < limbs.Count;
        }

        private static bool IsValidSlotIndex(int index, Limb limb)
        {
            return index >= 0 && index < (limb.Sensors.Count + limb.Neurons.Count);
        }

        private static bool IsValidSlotIndex(int index, Brain brain)
        {
            return index >= 0 && index < brain.Neurons.Count;
        }

        private static byte MapToRange(byte value, uint maxExclusive)
        {
            return (byte)((value * maxExclusive) >> 8); // Maps [0, 255] to [0, maxExclusive-1].
        }
    }
}
