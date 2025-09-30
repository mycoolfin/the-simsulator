using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player.FPS
{
    using ThreeD;

    [RequireComponent(typeof(UIDocument))]
    public class ReticleUIController : MonoBehaviour
    {
        [SerializeField] private FPSPlayerController playerController;

        [Header("Reticle Settings")]
        public Color reticleColor = Color.white;
        public Color interactableColor = Color.blue;
        public Color grabbableColor = Color.purple;
        public Color tooltipColor = Color.yellowNice;
        public float reticleSize = 10f;
        public bool showReticle = true;

        private VisualElement reticle;
        private UIDocument uiDocument;

        private void Start()
        {
            uiDocument = GetComponent<UIDocument>();

            CreateReticle();
        }

        private void CreateReticle()
        {
            var root = uiDocument.rootVisualElement;

            reticle = new();
            reticle.name = "reticle";

            reticle.style.position = Position.Absolute;
            reticle.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            reticle.style.top = new StyleLength(new Length(50, LengthUnit.Percent));
            reticle.style.width = reticleSize;
            reticle.style.height = reticleSize;
            reticle.style.backgroundColor = reticleColor;
            reticle.style.borderTopLeftRadius = reticleSize / 2;
            reticle.style.borderTopRightRadius = reticleSize / 2;
            reticle.style.borderBottomLeftRadius = reticleSize / 2;
            reticle.style.borderBottomRightRadius = reticleSize / 2;

            reticle.style.translate = new StyleTranslate(
                new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent))
            );

            root.Add(reticle);

            SetReticleVisibility(showReticle);
        }

        private void Update()
        {
            if (playerController != null && reticle != null)
            {
                bool shouldShow = showReticle && playerController.IsCursorLocked;
                SetReticleVisibility(shouldShow);

                UpdateReticleColor();
            }
        }

        private void UpdateReticleColor()
        {
            Color targetColor = reticleColor;
            if (playerController.rayEndTransform != null)
            {
                if (playerController.rayEndTransform.GetComponent<ISelectable>() != null)
                {
                    targetColor = interactableColor;
                }
                else if (playerController.rayEndTransform.GetComponentInParent<XRGrabInteractable>() != null)
                {
                    targetColor = grabbableColor;
                }
                else if (playerController.rayEndTransform.GetComponent<Tooltip>() != null)
                {
                    targetColor = tooltipColor;
                }
            }

            reticle.style.backgroundColor = targetColor;
        }

        public void SetReticleVisibility(bool visible)
        {
            if (reticle != null)
            {
                reticle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetReticleColor(Color color)
        {
            reticleColor = color;
            if (reticle != null)
            {
                reticle.style.backgroundColor = color;
            }
        }

        public void SetReticleSize(float size)
        {
            reticleSize = size;
            if (reticle != null)
            {
                reticle.style.width = size;
                reticle.style.height = size;
                reticle.style.borderTopLeftRadius = size / 2;
                reticle.style.borderTopRightRadius = size / 2;
                reticle.style.borderBottomLeftRadius = size / 2;
                reticle.style.borderBottomRightRadius = size / 2;
            }
        }
    }
}
