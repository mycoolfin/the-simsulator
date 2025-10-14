namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public enum AbsoluteSignalPort : byte
    {
        Disconnected,
        Bias,
        Brain,
        Limb
    }

    public struct SignalInput
    {
        public const float MIN_WEIGHT = -1f;
        public const float MAX_WEIGHT = 1f;

        public AbsoluteSignalPort Port { get; private set; }
        public int LimbIndex { get; private set; }
        public int SlotIndex { get; private set; }
        public float Weight { get; private set; }

        public SignalInput(AbsoluteSignalPort port, int limbIndex, int slotIndex, float weight)
        {
            Port = port;
            LimbIndex = limbIndex;
            SlotIndex = slotIndex;
            Weight = System.Math.Clamp(weight, MIN_WEIGHT, MAX_WEIGHT);
        }
    }
}
