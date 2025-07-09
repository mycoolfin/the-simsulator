using UnityEngine;

public class LimbRenderer : MonoBehaviour
{
    private mycoolfin.TheSimsulator.Sims.Phenotype.Limb Limb;

    // DEBUG.
    public bool DebugMirroredX = false;
    public bool DebugMirroredY = false;
    public bool DebugMirroredZ = false;

    private void Start()
    {
        if (Limb == null)
        {
            Debug.LogError("Limb is not assigned - did you call Initialize()?");
            return;
        }
    }

    public void Initialise(mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb, GameObject jointRendererPrefab)
    {
        Limb = limb;
        Limb.OnTransformChanged += UpdateFromLimb; // Note: Must only be invoked from the main thread.
        UpdateFromLimb();

        // Create joint renderer.
        if (Limb.Joint != null)
        {
            JointRenderer jointRenderer = Instantiate(jointRendererPrefab, transform).GetComponent<JointRenderer>();
            if (jointRenderer != null)
                jointRenderer.Initialise(Limb.Joint);
        }
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

            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = new Color(Limb.Color.X, Limb.Color.Y, Limb.Color.Z, Limb.Color.W);
        }
    }

    private void OnDestroy()
    {
        if (Limb != null)
            Limb.OnTransformChanged -= UpdateFromLimb;
    }
}
