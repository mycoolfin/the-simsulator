using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private GameObject target;

    private void Update()
    {
        if (target)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            Quaternion lookingRotation = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.position = target.transform.position - Quaternion.Euler(0f, 5f * Time.unscaledDeltaTime, 0f) * toTarget;
            transform.rotation = Quaternion.Slerp(transform.rotation, lookingRotation, Time.unscaledDeltaTime * 1f);
        }
    }

    public void SetTarget(GameObject targetObject)
    {
        target = targetObject;
    }
}
