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
                // Input preferences are in the range (0.0 to 1.0).
                // < switchThreshold -> the receiver will choose from the emitter pool.
                // > switchThreshold -> the receiver will take a constant value.
                float inputPreference = receiverInputPreferences[inputSlotIndex];
                float switchThreshold = 0.5f;
                float minConstantValue = -1f;
                float maxConstantValue = 1f;

                if (inputPreference < switchThreshold)
                {
                    // Map this receiver's input preference to an emitter in the provided selection pool.
                    // That emitter then becomes the receiver's input in this slot.
                    int choiceIndex = Mathf.RoundToInt(Mathf.Lerp(0, emitters.Length - 1, Mathf.InverseLerp(0f, switchThreshold, inputPreference)));
                    receiver.Inputs[inputSlotIndex] = emitters[choiceIndex];
                }
                else
                {
                    float constantValue = Mathf.Lerp(minConstantValue, maxConstantValue, Mathf.InverseLerp(switchThreshold, 1f, inputPreference));
                    receiver.InputOverrides[inputSlotIndex] = constantValue;
                }
            }
        }
    }
}