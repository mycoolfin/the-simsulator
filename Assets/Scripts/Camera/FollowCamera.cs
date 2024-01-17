using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Camera cam;
    private ISelectable target;
    private float desiredHeight;
    private float desiredDistance;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (target as UnityEngine.Object != null)
        {
            Bounds bounds = target.GetBounds();
            Vector3 toTarget = (bounds.center - transform.position).normalized;
            Vector3 toTargetXZ = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
            Quaternion lookingRotation = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.position = bounds.center - Quaternion.Euler(0f, 5f * Time.unscaledDeltaTime, 0f) * toTargetXZ * desiredDistance + Vector3.up * desiredHeight;
            transform.rotation = Quaternion.Slerp(transform.rotation, lookingRotation, Time.unscaledDeltaTime * 1f);
        }
    }

    public void SetTarget(ISelectable target, string layerName)
    {
        this.target = target;
        if (target as UnityEngine.Object != null)
        {
            cam.cullingMask = LayerMask.GetMask("Default", layerName);
            Bounds bounds = target.GetBounds();
            Vector3 size = bounds.extents * 2f;
            float maxLength = Mathf.Max(Mathf.Max(size.x, size.y), size.z);
            desiredHeight = maxLength * 1.5f;
            desiredDistance = maxLength * 1.5f;
            transform.position = bounds.center - target.gameObject.transform.forward * desiredDistance + Vector3.up * desiredHeight;
            Vector3 toTarget = (bounds.center - transform.position).normalized;
            Quaternion lookingRotation = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = lookingRotation;
        }
    }
}
