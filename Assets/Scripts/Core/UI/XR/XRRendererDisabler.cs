using UnityEngine;
using UnityEngine.XR;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player
{
    // For renderers that reduce XR performance (e.g. fog).
    public class XRRendererDisabler : MonoBehaviour
    {
        [SerializeField] private Renderer rendererToDisable;

        private void OnEnable()
        {
            if (rendererToDisable != null && XRSettings.enabled)
                rendererToDisable.enabled = false;
        }
    }
}
