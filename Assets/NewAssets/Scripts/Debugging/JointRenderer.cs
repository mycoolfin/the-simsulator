using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // For Handles.
#endif

public class JointRenderer : MonoBehaviour
{
    private mycoolfin.TheSimsulator.Sims.Joint Joint;

    private void Start()
    {
        if (Joint == null)
        {
            Debug.LogError("Joint is not assigned - did you call Initialize()?");
            return;
        }
    }

    public void Initialise(mycoolfin.TheSimsulator.Sims.Joint joint)
    {
        Joint = joint;
        Joint.ParentLimb.OnTransformChanged += UpdateFromLimb; // Note: Must only be invoked from the main thread.
        UpdateFromLimb();
    }

    private void UpdateFromLimb()
    {
        if (Joint != null)
        {
            var pos = Joint.ParentLimb.Position + Joint.ParentLimb.Rotation * Joint.ParentSpaceAnchor;
            Vector3 position = new(pos.X, pos.Y, pos.Z);
            transform.position = position;
            transform.localScale = new Vector3(Joint.ParentLimb.Dimensions.X, Joint.ParentLimb.Dimensions.Y, Joint.ParentLimb.Dimensions.Z) / 10f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Joint != null)
        {
            var pos = transform.position;

            var xAxis = Joint.ParentLimb.Rotation * Joint.ParentSpaceXAxis;
            var yAxis = Joint.ParentLimb.Rotation * Joint.ParentSpaceYAxis;
            var zAxis = Joint.ParentLimb.Rotation * Joint.ParentSpaceZAxis;

            Vector3 worldX = new(xAxis.X, xAxis.Y, xAxis.Z);
            Vector3 worldY = new(yAxis.X, yAxis.Y, yAxis.Z);
            Vector3 worldZ = new(zAxis.X, zAxis.Y, zAxis.Z);

            float arrowLength = transform.localScale.x * 2f;
            Gizmos.color = Color.red; Gizmos.DrawLine(pos, pos + worldX * arrowLength);
            Gizmos.color = Color.green; Gizmos.DrawLine(pos, pos + worldY * arrowLength);
            Gizmos.color = Color.blue; Gizmos.DrawLine(pos, pos + worldZ * arrowLength);

            if (Joint.XAxisController != null)
            {
                // Draw arc for X-axis rotation limits in YZ plane
                Handles.color = new Color(1, 0, 0, 0.2f);
                Handles.DrawSolidArc(pos, worldX, worldZ, Joint.AngleLimits.X * 2f, arrowLength);
            }

            if (Joint.YAxisController != null)
            {
                // Draw arc for Y-axis rotation limits in XZ plane
                Handles.color = new Color(0, 1, 0, 0.2f);
                Handles.DrawSolidArc(pos, worldY, worldZ, Joint.AngleLimits.Y * 2f, arrowLength);
            }

            if (Joint.ZAxisController != null)
            {
                // Draw arc for Z-axis rotation limits in XY plane
                Handles.color = new Color(0, 0, 1, 0.2f);
                Handles.DrawSolidArc(pos, worldZ, worldX, Joint.AngleLimits.Z * 2f, arrowLength);
            }
        }
    }
#endif

    private void OnDestroy()
    {
        if (Joint != null)
            Joint.ParentLimb.OnTransformChanged -= UpdateFromLimb;
    }
}
