using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Player.FPS;

    public class WebLinkButton : MonoBehaviour
    {
        [SerializeField] private PushButton button;
        [SerializeField] private string url = string.Empty;
        [SerializeField] private FPSPlayerController playerController;

        private void Start()
        {
            button.OnButtonPressed += (_) =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                    playerController.SetCursorLock(false);
                }
            };
        }
    }
}
