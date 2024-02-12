using UnityEngine;

public class FluidForces : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 surfaceAreas;
    private Vector3[] facePositions;
    private Vector3[][] quadrantPositions;
    private Vector3[][] quadrantForceVectors;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        surfaceAreas = new Vector3(
            transform.localScale.y * transform.localScale.z,
            transform.localScale.x * transform.localScale.z,
            transform.localScale.x * transform.localScale.y
        );

        facePositions = new Vector3[6];
        quadrantPositions = new Vector3[6][];
        quadrantForceVectors = new Vector3[6][];
        for (int face = 0; face < 6; face++)
        {
            int perpendicularAxis = face % 3;
            Vector3 localFaceNormal = Vector3.zero;
            localFaceNormal[perpendicularAxis] = face > 2 ? 1f : -1f;

            Vector3 localFaceCenter = Vector3.one;
            localFaceCenter[perpendicularAxis] *= face > 2 ? 0.5f : -0.5f;
            localFaceCenter[(perpendicularAxis + 1) % 3] = 0f;
            localFaceCenter[(perpendicularAxis + 2) % 3] = 0f;
            facePositions[face] = localFaceCenter;

            quadrantPositions[face] = new Vector3[4];
            quadrantForceVectors[face] = new Vector3[4];
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                Vector3 localFaceQuadrant = localFaceCenter;
                localFaceQuadrant[(perpendicularAxis + 1) % 3] = quadrant < 2 ? -0.25f : 0.25f;
                localFaceQuadrant[(perpendicularAxis + 2) % 3] = quadrant % 2 == 0 ? -0.25f : 0.25f;
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

            for (int face = 0; face < 3; face++)
            {
                // If one face is experiencing drag, no need to calculate forces for its opposing face.
                Vector3 faceCenter = transform.TransformPoint(facePositions[face]);
                int activeFace = Vector3.Dot(-rb.GetPointVelocity(faceCenter), directions[face]) < 0 ? face : face + 3;
                float quadrantSurfaceArea = (surfaceAreas[activeFace % 3] / 4);

                // Splitting each face into quadrants allows for more accurate torque application.
                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    Vector3 faceQuadrantCenter = transform.TransformPoint(quadrantPositions[activeFace][quadrant]);
                    Vector3 pointVelocity = rb.GetPointVelocity(faceQuadrantCenter);
                    float velocityRelativeToFluid = Vector3.Dot(-pointVelocity, directions[activeFace]);
                    float dragForce = 0.5f * WorldManager.Instance.fluidDensity * -Mathf.Pow(velocityRelativeToFluid, 2) * quadrantSurfaceArea;

                    Vector3 dragForceVector = dragForce * pointVelocity.normalized;
                    quadrantForceVectors[activeFace][quadrant] = dragForceVector;
                    quadrantForceVectors[(activeFace + 3) % 6][quadrant] = Vector3.zero;

                    rb.AddForceAtPosition(dragForceVector, faceQuadrantCenter);
                }
            }
        }
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
        if (!enabled) return;
        if (WorldManager.Instance.simulateFluid)
        {
            if (rb.IsSleeping() || GetMassNormalizedKE() < rb.sleepThreshold) return;

            Gizmos.color = Color.cyan;

            for (int face = 0; face < 6; face++)
            {
                for (int quadrant = 0; quadrant < 4; quadrant++)
                {
                    Vector3 faceQuadrantCenter = transform.TransformPoint(quadrantPositions[face][quadrant]);
                    if (quadrantForceVectors[face][quadrant].magnitude != 0)
                    {
                        Gizmos.DrawCube(faceQuadrantCenter, Vector3.one * 0.1f);
                        Gizmos.DrawLine(faceQuadrantCenter, faceQuadrantCenter - quadrantForceVectors[face][quadrant]);
                    }
                }
            }
        }
    }
}
