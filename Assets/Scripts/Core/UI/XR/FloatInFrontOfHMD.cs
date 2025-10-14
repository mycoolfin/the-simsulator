using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.XR
{
    public class FloatInFrontOfHMD : MonoBehaviour
    {
        public Transform hmd;

        [Header("Placement (meters)")]
        [Min(0.1f)] public float distance = 0.70f;
        public float verticalOffset = -0.05f;
        public float horizontalOffset = 0.00f;

        [Header("Motion")]
        public float positionLerp = 12f;
        public float rotationLerp = 12f;

        [Header("Keep panel upright (ignore HMD roll)")]
        public bool keepUpright = true;

        void LateUpdate()
        {
            if (!hmd) return;

            Vector3 flatForward = Vector3.ProjectOnPlane(hmd.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 1e-4f) flatForward = hmd.forward.normalized;

            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            Vector3 targetPos = hmd.position
                              + flatForward * distance
                              + Vector3.up * verticalOffset
                              + right * horizontalOffset;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionLerp);

            Quaternion targetRot = keepUpright
                ? Quaternion.LookRotation(flatForward, Vector3.up)
                : Quaternion.LookRotation((transform.position - hmd.position).normalized, hmd.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerp);
        }
    }
}
