using UnityEngine;

public class FluidForces : MonoBehaviour
{
    private Rigidbody rb;
    private Bounds bounds;
    private Vector3 surfaceAreas;

    private Vector3 forces;
    private Vector3 xForcePosition;
    private Vector3 yForcePosition;
    private Vector3 zForcePosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        bounds = GetComponent<BoxCollider>().bounds;
        surfaceAreas = new Vector3(
            transform.localScale.y * transform.localScale.z,
            transform.localScale.x * transform.localScale.z,
            transform.localScale.x * transform.localScale.y
        );
    }

    private void FixedUpdate()
    {
        if (WorldManager.Instance.simulateFluid)
        {
            Vector3 relativeVelocity = transform.InverseTransformDirection(rb.velocity);
            forces.x = -relativeVelocity.x * surfaceAreas.x * WorldManager.Instance.fluidViscosity;
            forces.y = -relativeVelocity.y * surfaceAreas.y * WorldManager.Instance.fluidViscosity;
            forces.z = -relativeVelocity.z * surfaceAreas.z * WorldManager.Instance.fluidViscosity;

            xForcePosition = transform.position - (Mathf.Sign(forces.x) * transform.right * transform.localScale.x / 2);
            yForcePosition = transform.position - (Mathf.Sign(forces.y) * transform.up * transform.localScale.y / 2);
            zForcePosition = transform.position - (Mathf.Sign(forces.z) * transform.forward * transform.localScale.z / 2);

            rb.AddForceAtPosition(transform.right * forces.x, xForcePosition, ForceMode.Force);
            rb.AddForceAtPosition(transform.up * forces.y, yForcePosition, ForceMode.Force);
            rb.AddForceAtPosition(transform.forward * forces.z, zForcePosition, ForceMode.Force);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(xForcePosition, xForcePosition - transform.right * forces.x);
        Gizmos.DrawLine(yForcePosition, yForcePosition - transform.up * forces.y);
        Gizmos.DrawLine(zForcePosition, zForcePosition - transform.forward * forces.z);

        Gizmos.DrawCube(xForcePosition, Vector3.one * 0.1f);
        Gizmos.DrawCube(yForcePosition, Vector3.one * 0.1f);
        Gizmos.DrawCube(zForcePosition, Vector3.one * 0.1f);
    }
}
