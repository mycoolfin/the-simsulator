namespace mycoolfin.TheSimsulator.Sims
{
    public class Limb
    {
        public Vector3 Dimensions { get; private set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public Limb(Vector3 dimensions, Vector3 position, Quaternion rotation)
        {
            Dimensions = dimensions;
            Position = position;
            Rotation = rotation;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
