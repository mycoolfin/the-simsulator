using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player.FPS
{
    using ThreeD;

    public class TooltipUIController : MonoBehaviour
    {
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float displayDelay = 0.25f;

        private VisualElement container;
        private Label label;

        private float hoverTimer = 0f;

        private void Start()
        {
            container = uiDocument.rootVisualElement;
            label = uiDocument.rootVisualElement.Q<Label>();
            label.text = string.Empty;
            container.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (container == null || label == null || playerController == null) return;

            if (playerController.LookingAtSomething && playerController.RaycastHit.collider.TryGetComponent(out Tooltip tooltip))
            {
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= displayDelay)
                {
                    label.text = tooltip.Text;
                    container.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                hoverTimer = 0f;
                label.text = string.Empty;
                container.style.display = DisplayStyle.None;
            }
        }
    }
}
