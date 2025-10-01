using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.XR
{
    using ThreeD;

    public class XRControllerHider : MonoBehaviour
    {
        [SerializeField] private GameObject leftController;
        [SerializeField] private GameObject rightController;

        private bool ShouldHide => EditableText.AnyEditing;

        private void Update()
        {
            if (leftController != null && leftController.activeSelf != !ShouldHide) leftController.SetActive(!ShouldHide);
            if (rightController != null && rightController.activeSelf != !ShouldHide) rightController.SetActive(!ShouldHide);
        }
    }
}
