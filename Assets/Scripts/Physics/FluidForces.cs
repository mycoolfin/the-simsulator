using UnityEngine;

public class FluidForces : MonoBehaviour
{
    private Rigidbody rb;
    private Bounds bounds;
    private Vector3 surfaceAreas;
    private Vector3[][] quadrantPositions;
    private Vector3[][] quadrantVelocities;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        bounds = GetComponent<BoxCollider>().bounds;

        surfaceAreas = new Vector3(
            transform.localScale.y * transform.localScale.z,
            transform.localScale.x * transform.localScale.z,
            transform.localScale.x * transform.localScale.y
        );

        quadrantPositions = new Vector3[6][];
        quadrantVelocities = new Vector3[6][];
        for (int face = 0; face < 6; face++)
        {
            int perpendicularAxis = face % 3;
            Vector3 localFaceNormal = Vector3.zero;
            localFaceNormal[perpendicularAxis] = face > 2 ? 1f : -1f;

            Vector3 localFaceCenter = Vector3.one;
            localFaceCenter[perpendicularAxis] *= face > 2 ? 0.5f : -0.5f;
            localFaceCenter[(perpendicularAxis + 1) % 3] = 0f;
            localFaceCenter[(perpendicularAxis + 2) % 3] = 0f;

            quadrantPositions[face] = new Vector3[4];
            quadrantVelocities[face] = new Vector3[4];
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                Vector3 localFaceQuadrant = localFaceCenter;
                localFaceQuadrant[(perpendicularAxis + 1) % 3] = (quadrant < 2 ? -0.25f : 0.25f);
                localFaceQuadrant[(perpendicularAxis + 2) % 3] = (quadrant % 2 == 0 ? -0.25f : 0.25f);
                quadrantPositions[face][quadrant] = localFaceQuadrant;
            }
        }
    }

    private void FixedUpdate()
    {
        if (WorldManager.Instance.simulateFluid)
        {
            if (rb.IsSleeping() || GetMassNormalizedKE() < rb.sleepThreshold) return;

            Vector3[] directions = GetTransformDirections();

            for (int face = 0; face < 6; face++)
            {
                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    Vector3 faceQuadrantCenter = transform.TransformPoint(quadrantPositions[face][quadrant]);
                    float surfaceArea = (surfaceAreas[face % 3] / 4);
                    Vector3 pointVelocity = rb.GetPointVelocity(faceQuadrantCenter);
                    float dragForce = GetSurfaceDragForce(faceQuadrantCenter, directions[face], pointVelocity, surfaceArea);
                    rb.AddForceAtPosition(dragForce * directions[face], faceQuadrantCenter);
                    quadrantVelocities[face][quadrant] = pointVelocity;
                }
            }
        }
    }

    private float GetSurfaceDragForce(Vector3 surfaceCenter, Vector3 surfaceNormal, Vector3 velocity, float surfaceArea)
    {
        float relativeFlowVelocity = Vector3.Dot(-velocity, surfaceNormal);
        float dragForce = relativeFlowVelocity < 0f ? 0.5f * WorldManager.Instance.fluidDensity * -Mathf.Pow(relativeFlowVelocity, 2) * surfaceArea : 0f;
        return dragForce;
    }

    private Vector3[] GetTransformDirections()
    {
        return new Vector3[] {
            -transform.right,
            -transform.up,
            -transform.forward,
            transform.right,
            transform.up,
            transform.forward
        };
    }

    private float GetMassNormalizedKE()
    {
        float e = 0.5f * rb.mass * rb.velocity.sqrMagnitude; // Linear KE

        // Angular KE
        Vector3 inertia = rb.inertiaTensor;
        Vector3 av = rb.angularVelocity;
        e += 0.5f * inertia.x * av.x * av.x;
        e += 0.5f * inertia.y * av.y * av.y;
        e += 0.5f * inertia.z * av.z * av.z;

        return e / rb.mass;
    }

    private void OnDrawGizmosSelected()
    {
        if (WorldManager.Instance.simulateFluid)
        {
            Vector3[] directions = GetTransformDirections();
            for (int face = 0; face < 6; face++)
            {
                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    Vector3 faceQuadrantCenter = transform.TransformPoint(quadrantPositions[face][quadrant]);
                    float surfaceArea = (surfaceAreas[face % 3] / 4);
                    Vector3 pointVelocity = rb.GetPointVelocity(faceQuadrantCenter);
                    float dragForce = GetSurfaceDragForce(faceQuadrantCenter, directions[face], pointVelocity, surfaceArea);
                    Gizmos.color = Color.magenta;
                    if (dragForce != 0)
                    {
                        Gizmos.DrawCube(faceQuadrantCenter, Vector3.one * 0.1f);
                        Gizmos.DrawLine(faceQuadrantCenter, faceQuadrantCenter - Mathf.Max(-rb.mass, dragForce) * directions[face]);
                    }
                }
            }
        }
    }
}
