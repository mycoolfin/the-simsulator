using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player.FPS
{
    using ThreeD;
    using UnityEngine.XR.Interaction.Toolkit;

    public class TooltipUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float displayDelay = 0.25f;

        private VisualElement container;
        private Label label;

        private Tooltip hoveredTooltip;
        private float hoverTimer = 0f;

        private void Start()
        {
            if (uiDocument == null)
            {
                Debug.LogError("UI Document is not assigned.", this);
                enabled = false;
                return;
            }

            container = uiDocument.rootVisualElement;
            label = uiDocument.rootVisualElement.Q<Label>();

            if (container == null || label == null)
            {
                Debug.LogError("UI Document does not contain the required elements.", this);
                enabled = false;
                return;
            }

            label.text = string.Empty;
            container.pickingMode = PickingMode.Ignore;
            Hide();
        }

        public void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out Tooltip tooltip))
                hoveredTooltip = tooltip;
            else
                hoveredTooltip = null;
        }

        public void OnHoverExited(HoverExitEventArgs args)
        {
            hoveredTooltip = null;
        }

        void Show(string text)
        {
            label.text = text;
            container.style.visibility = Visibility.Visible;
            container.style.opacity = 1f;
        }

        void Hide()
        {
            label.text = string.Empty;
            container.style.opacity = 0f;
            container.style.visibility = Visibility.Hidden;
        }

        private void Update()
        {
            if (hoveredTooltip != null)
            {
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= displayDelay)
                    Show(hoveredTooltip.Text);
            }
            else
            {
                Hide();
            }
        }
    }
}
