using UnityEngine;

public static class NervousSystem
{
    public static void ConfigureSignalReceivers(ISignalReceiver[] receivers, float[][] inputPreferences, ISignalEmitter[] emitters)
    {
        for (int receiverIndex = 0; receiverIndex < receivers.Length; receiverIndex++)
        {
            ISignalReceiver receiver = receivers[receiverIndex];
            float[] receiverInputPreferences = inputPreferences[receiverIndex];
            for (int inputSlotIndex = 0; inputSlotIndex < receiver.Inputs.Length; inputSlotIndex++)
            {
                // Map this receiver's input preference (0.0 to 1.0) to an emitter in the provided selection pool.
                // That emitter then becomes the receiver's input in this slot.
                int choiceIndex = Mathf.RoundToInt(Mathf.Lerp(0, emitters.Length - 1, receiverInputPreferences[inputSlotIndex]));
                receiver.Inputs[inputSlotIndex] = emitters[choiceIndex];
            }
        }
    }
}