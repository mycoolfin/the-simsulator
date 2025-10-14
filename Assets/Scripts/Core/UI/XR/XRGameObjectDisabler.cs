using UnityEngine;
using UnityEngine.XR;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player
{
    public class XRGameObjectDisabler : MonoBehaviour
    {
        [SerializeField] private GameObject objectToDisable;

        private void OnEnable()
        {
            if (objectToDisable != null && XRSettings.enabled)
                objectToDisable.SetActive(false);
        }
    }
}
