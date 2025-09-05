using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public class WebLinkButton : MonoBehaviour
    {
        [SerializeField] private PushButton button;
        [SerializeField] private string url = string.Empty;

        private void Start()
        {
            button.OnButtonPressed += (_) =>
            {
                if (!string.IsNullOrEmpty(url))
                    Application.OpenURL(url);
            };
        }
    }
}
