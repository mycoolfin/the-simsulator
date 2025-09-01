using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.TwoD
{
    using Player;
    using UnityEngine.XR.Interaction.Toolkit.Interactors;

    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(FPSPlayerController))]
    public class ReticleUIController : MonoBehaviour
    {
        [Header("Reticle Settings")]
        public Color reticleColor = Color.white;
        public Color interactableColor = Color.blue;
        public Color grabbableColor = Color.purple;
        public float reticleSize = 4f;
        public bool showReticle = true;

        private VisualElement reticle;
        private UIDocument uiDocument;
        private FPSPlayerController playerController;

        private void Start()
        {
            playerController = GetComponent<FPSPlayerController>();
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
            if (playerController.playerCamera != null)
            {
                Ray ray = playerController.playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, playerController.interactionRange, playerController.interactionLayerMask))
                {
                    bool lookingAtInteractable = hit.collider.GetComponent<ISelectable>() != null;
                    bool lookingAtGrabbable = hit.collider.GetComponentInParent<XRGrabInteractable>() != null;

                    if (lookingAtInteractable)
                    {
                        targetColor = interactableColor;
                    }
                    else if (lookingAtGrabbable)
                    {
                        targetColor = grabbableColor;
                    }
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
