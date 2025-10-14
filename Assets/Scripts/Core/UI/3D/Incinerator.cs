using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public class Incinerator : MonoBehaviour
    {
        public void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<ICreatureCapsule>() != null
                || other.GetComponentInParent<CapsuleDock>() != null)
            {
                Destroy(other.transform.root.gameObject);
            }
        }
    }
}
