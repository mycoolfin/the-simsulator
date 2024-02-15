using UnityEngine;

public class ForceZone : MonoBehaviour
{
    public float forceMagnitude = 1f;

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out Rigidbody rb))
            rb.AddForce(transform.forward * forceMagnitude);
    }
}