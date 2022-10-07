using UnityEngine;

public class Tester : MonoBehaviour
{
    void Start()
    {
        PhenotypeConstructionTest();
    }

    private void PhenotypeConstructionTest()
    {
        GraphNode body = new()
        {
            dimensions = new Vector3(0.5f, 1f, 0.5f),
            jointType = JointType.Rigid
        };
        GraphNode head = new()
        {
            dimensions = new Vector3(0.3f, 0.5f, 0.3f),
            jointType = JointType.Twist,
            jointLimits = new float[] { 90f }
        };
        GraphNode limb = new()
        {
            dimensions = new Vector3(0.1f, 0.1f, 1f),
            jointType = JointType.TwistBend,
            jointLimits = new float[] { 10f, 10f },
            recursiveLimit = 1
        };

        GraphConnection headConnection = new()
        {
            childNode = head,
            parentFace = 4,
            position = new Vector2(0, 0),
            orientation = new Vector3(0f, 0f, 0f),
            scale = Vector3.one
        };
        GraphConnection leftArmConnection = new()
        {
            childNode = limb,
            parentFace = 0,
            position = new Vector2(0.8f, 0f),
            orientation = new Vector3(30f, 0f, 0f),
            scale = Vector3.one
        };
        GraphConnection rightArmConnection = new()
        {
            childNode = limb,
            parentFace = 0,
            position = new Vector2(0.8f, 0f),
            orientation = new Vector3(30f, 0f, 0f),
            scale = Vector3.one,
            reflection = true
        };
        GraphConnection leftLegConnection = new()
        {
            childNode = limb,
            parentFace = 1,
            position = new Vector2(0f, -0.8f),
            orientation = new Vector3(-30f, -90f, 0f),
            scale = Vector3.one
        };
        GraphConnection rightLegConnection = new()
        {
            childNode = limb,
            parentFace = 1,
            position = new Vector2(0f, 0.8f),
            orientation = new Vector3(-30f, 90f, 0f),
            scale = Vector3.one
        };
        GraphConnection limbConnection = new()
        {
            childNode = limb,
            parentFace = 5,
            position = new Vector2(0f, 0f),
            orientation = new Vector3(30f, 0f, 0f),
            scale = new Vector3(1f, 1f, 1f)
        };

        GraphConnection[] bodyConnections = new[] { headConnection, leftArmConnection, rightArmConnection, leftLegConnection, rightLegConnection };
        body.connections = bodyConnections;
        GraphConnection[] limbConnections = new[] { limbConnection };
        limb.connections = limbConnections;

        GameObject creature = PhenotypeBuilder.ConstructPhenotype(body);
        creature.transform.position = new Vector3(0, 5f, 0);

        // Physics.gravity = Vector3.zero;
    }
}
