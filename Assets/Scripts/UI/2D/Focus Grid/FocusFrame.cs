using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI.Screen
{
    using Evolution;

    public class FocusFrame
    {
        public VisualElement frameElement;
        public IAssessableCreature focusedIndividual;
        public FollowCamera followCamera;
        private VisualElement viewport;
        private Label nameLabel;
        private Label fitnessLabel;

        public FocusFrame(Transform parent, VisualElement grid, VisualTreeAsset frameElementAsset, GameObject followCameraPrefab)
        {
            frameElement = frameElementAsset.Instantiate();
            grid.Add(frameElement);

            viewport = frameElement.Q<VisualElement>("viewport");
            nameLabel = frameElement.Q<Label>("name");
            fitnessLabel = frameElement.Q<Label>("fitness");
            Button saveButton = frameElement.Q<Button>("save");
            Button protectButton = frameElement.Q<Button>("protect");
            Button cullButton = frameElement.Q<Button>("cull");

            saveButton.clicked += () => focusedIndividual.SaveGenotypeToFile();
            protectButton.clicked += () => focusedIndividual.Protect(!focusedIndividual.IsProtected);
            cullButton.clicked += () => focusedIndividual.Cull();

            // followCamera = GameObject.Instantiate(followCameraPrefab).GetComponent<FollowCamera>();
            // followCamera.transform.parent = parent;
            // ConfigureRenderTexture();
            // frameElement.RegisterCallback<GeometryChangedEvent>((e) => ConfigureRenderTexture());

            SetActive(false);
            SetTarget(null, "", "");
        }

        public void SetActive(bool active)
        {
            frameElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            // followCamera.cam.enabled = active;
        }

        public void SetTarget(IAssessableCreature target, string layerName, string prefix)
        {
            focusedIndividual = target;
            nameLabel.text = target?.Name ?? "<none>";
            fitnessLabel.text = "Fitness: " + (target?.Fitness.ToString("0.000") ?? "<none>");
            // TODO: New method of tracking.
            // followCamera.SetTarget(target?.Phenotype, layerName);
        }

        private void ConfigureRenderTexture()
        {
            RenderTexture rt = new(UnityEngine.Device.Screen.width / 4, (int)((UnityEngine.Device.Screen.width / 4) * (2f / 3f)), 16, RenderTextureFormat.ARGB32);
            frameElement.style.width = 300;
            frameElement.style.height = 200;
            viewport.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            followCamera.GetComponent<Camera>().targetTexture = rt;
        }
    }
}
