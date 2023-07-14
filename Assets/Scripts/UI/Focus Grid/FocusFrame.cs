using System;
using UnityEngine;
using UnityEngine.UIElements;

public class FocusFrame
{
    public VisualElement frameElement;
    public Individual focusedIndividual;

    private FollowCamera followCamera;
    private EvolutionSimulator simulator;
    private VisualElement viewport;
    private Label nameLabel;
    private Label fitnessLabel;

    public FocusFrame(Transform parent, VisualElement grid, VisualTreeAsset frameElementAsset, GameObject followCameraPrefab, EvolutionSimulator simulator)
    {
        frameElement = frameElementAsset.Instantiate();
        grid.Add(frameElement);

        viewport = frameElement.Q<VisualElement>("viewport");
        nameLabel = frameElement.Q<Label>("name");
        fitnessLabel = frameElement.Q<Label>("fitness");
        Button saveButton = frameElement.Q<Button>("save");
        Button protectButton = frameElement.Q<Button>("protect");
        Button cullButton = frameElement.Q<Button>("cull");

        saveButton.clicked += () => focusedIndividual.phenotype.SaveGenotypeToFile();
        protectButton.clicked += () => focusedIndividual.isProtected = !focusedIndividual.isProtected;
        cullButton.clicked += () => focusedIndividual.Cull();

        followCamera = UnityEngine.GameObject.Instantiate(followCameraPrefab).GetComponent<FollowCamera>();
        followCamera.transform.parent = parent;
        ConfigureRenderTexture();
        frameElement.RegisterCallback<GeometryChangedEvent>((e) => ConfigureRenderTexture());

        this.simulator = simulator;

        SetActive(false);
        SetTarget(null, "", "");
    }

    public void SetActive(bool active)
    {
        frameElement.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetTarget(Individual target, string layerName, string prefix)
    {
        focusedIndividual = target;
        nameLabel.text = target?.phenotype != null ? prefix + target.phenotype.name : "<none>";
        fitnessLabel.text = "Fitness: " + (target != null ? target.previousFitness.ToString("0.000") : "<none>");
        followCamera.SetTarget(target?.phenotype, layerName);
    }

    private void ConfigureRenderTexture()
    {
        RenderTexture rt = new RenderTexture(Screen.width / 4, (int)((Screen.width / 4) * (2f/3f)), 16, RenderTextureFormat.ARGB32);
        frameElement.style.width = 300;
        frameElement.style.height = 200;
        viewport.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        followCamera.GetComponent<Camera>().targetTexture = rt;
    }
}
