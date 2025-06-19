using UnityEngine;

public class LimbRenderer : MonoBehaviour
{
    private mycoolfin.TheSimsulator.Sims.Limb Limb;

    public bool DebugMirroredX = false;
    public bool DebugMirroredY = false;
    public bool DebugMirroredZ = false; // TODO: Remove these.

    private void Start()
    {
        if (Limb == null)
        {
            Debug.LogError("Limb is not assigned - did you call Initialize()?");
            return;
        }
    }

    public void Initialise(mycoolfin.TheSimsulator.Sims.Limb limb)
    {
        Limb = limb;
        Limb.OnTransformChanged += UpdateFromLimb; // Note: Must only be invoked from the main thread.
        UpdateFromLimb();
    }

    private void UpdateFromLimb()
    {
        if (Limb != null)
        {
            transform.SetPositionAndRotation(
                new Vector3(Limb.Position.X, Limb.Position.Y, Limb.Position.Z),
                new Quaternion(Limb.Rotation.X, Limb.Rotation.Y, Limb.Rotation.Z, Limb.Rotation.W)
            );
            transform.localScale = new Vector3(Limb.Dimensions.X, Limb.Dimensions.Y, Limb.Dimensions.Z);

            // Debug mirroring.
            DebugMirroredX = Limb.debugMirroredX;
            DebugMirroredY = Limb.debugMirroredY;
            DebugMirroredZ = Limb.debugMirroredZ;

            GetComponent<Renderer>().material.color = new Color(DebugMirroredX ? 1f : 0f,
                                                               DebugMirroredY ? 1f : 0f,
                                                               DebugMirroredZ ? 1f : 0f,
                                                               1f); // Set color based on mirroring flags.
        }
    }

    private void OnDestroy()
    {
        if (Limb != null)
            Limb.OnTransformChanged -= UpdateFromLimb;
    }
}
