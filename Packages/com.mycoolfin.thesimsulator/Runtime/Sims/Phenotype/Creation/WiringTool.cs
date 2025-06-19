using System.Linq;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class WiringTool
    {
        public static void WireUp(ISignalReceiver receiver, SignalInputDefinition inputA, SignalInputDefinition inputB, SignalInputDefinition inputC, Limb thisLimb, Limb parentLimb, List<Limb> childLimbs, Brain brain)
        {
            receiver.InputA = WireInput(inputA, thisLimb, parentLimb, childLimbs, brain);
            receiver.InputB = WireInput(inputB, thisLimb, parentLimb, childLimbs, brain);
            receiver.InputC = WireInput(inputC, thisLimb, parentLimb, childLimbs, brain);
        }

        private static SignalInput WireInput(SignalInputDefinition inputDefinition, Limb thisLimb, Limb parentLimb, List<Limb> childLimbs, Brain brain)
        {
            Limb childLimb = childLimbs?.ElementAtOrDefault(inputDefinition.SignalEmitterAddress.ChildIndex) ?? null;
            IEnumerable<ISignalEmitter> emitterSet = inputDefinition.SignalEmitterAddress.Port switch
            {
                SignalPort.Bias => null,
                SignalPort.ThisLimb => thisLimb?.Sensors.Concat<ISignalEmitter>(thisLimb?.Neurons) ?? null,
                SignalPort.ParentLimb => parentLimb?.Sensors.Concat<ISignalEmitter>(parentLimb?.Neurons) ?? null,
                SignalPort.ChildLimb => childLimb?.Sensors.Concat<ISignalEmitter>(childLimb?.Neurons) ?? null,
                SignalPort.Brain => brain.Neurons,
                _ => throw new System.NotSupportedException($"Unsupported signal port: {inputDefinition.SignalEmitterAddress.Port}")
            };

            ISignalEmitter emitter = null;
            if (emitterSet != null)
            {
                emitter = emitterSet.ElementAtOrDefault(inputDefinition.SignalEmitterAddress.Slot);
            }

            return new SignalInput(emitter, inputDefinition.Weight);
        }
    }
}
