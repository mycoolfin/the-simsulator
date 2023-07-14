using System;
using UnityEngine;
using UnityEngine.UIElements;

public class SelectedPhenotypeMenu : MonoBehaviour
{
    public Camera cam;

    private Phenotype selectedPhenotype;
    public Phenotype SelectedPhenotype => selectedPhenotype;

    private UIDocument doc;
    private VisualElement menu;
    private VisualElement cursor;
    private BlockingPanel blockingPanel;
    private Label titleLabel;
    private Label infoLabel;
    private Button saveButton;
    private Button protectButton;
    private Button cullButton;
    private Button breedButton;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        menu = doc.rootVisualElement.Q<VisualElement>("menu");
        menu.style.display = DisplayStyle.None;
        blockingPanel = gameObject.AddComponent<BlockingPanel>();
        blockingPanel.SetPanel(menu.Q<VisualElement>("blocking-panel"));
        titleLabel = menu.Q<Label>("title");
        infoLabel = menu.Q<Label>("info");
        saveButton = menu.Q<Button>("save");
        protectButton = menu.Q<Button>("protect");
        cullButton = menu.Q<Button>("cull");
        breedButton = menu.Q<Button>("breed");

        saveButton.style.display = DisplayStyle.None;
        protectButton.style.display = DisplayStyle.None;
        cullButton.style.display = DisplayStyle.None;
        breedButton.style.display = DisplayStyle.None;

        cursor = doc.rootVisualElement.Q<VisualElement>("cursor");
        cursor.generateVisualContent += OnGenerateVisualContent;

        SetTarget(null);
        SetInfoText(null);
    }

    private void Update()
    {
        if (selectedPhenotype != null)
        {
            Vector3 uiPos = WorldToUI(selectedPhenotype.GetBounds().center);
            cursor.style.left = uiPos.x;
            cursor.style.top = uiPos.y;
            cursor.MarkDirtyRepaint();
        }
        else
            SetTarget(null);
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        float anchorX;
        float anchorY;
        if (cursor.worldBound.center.x < menu.worldBound.xMin) anchorX = menu.worldBound.xMin;
        else if (cursor.worldBound.center.x < menu.worldBound.xMax) anchorX = menu.worldBound.center.x;
        else anchorX = menu.worldBound.xMax;
        if (cursor.worldBound.center.y < menu.worldBound.yMin) anchorY = menu.worldBound.yMin;
        else if (cursor.worldBound.center.y < menu.worldBound.yMax) anchorY = menu.worldBound.center.y;
        else anchorY = menu.worldBound.yMax;

        mgc.painter2D.strokeColor = Color.black;
        mgc.painter2D.lineJoin = LineJoin.Round;
        mgc.painter2D.lineCap = LineCap.Round;
        mgc.painter2D.BeginPath();
        mgc.painter2D.MoveTo(new Vector2(cursor.worldBound.width / 2f, cursor.worldBound.height / 2f));
        mgc.painter2D.LineTo(new Vector2(anchorX + cursor.worldBound.width / 2f, anchorY + cursor.worldBound.height / 2f) - cursor.worldBound.center);
        mgc.painter2D.Stroke();
    }

    private Vector2 WorldToUI(Vector3 worldPosition)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(selectedPhenotype.GetBounds().center);
        float xScalingFactor = (float)Screen.width / (float)doc.panelSettings.referenceResolution.x; // Screen match mode set to width only.
        return new Vector2(screenPos.x / xScalingFactor, (Screen.height - screenPos.y) / xScalingFactor);
    }

    public void SetTarget(Phenotype selectedPhenotype)
    {
        this.selectedPhenotype = selectedPhenotype;
        menu.style.display = selectedPhenotype != null ? DisplayStyle.Flex : DisplayStyle.None;
        cursor.style.display = selectedPhenotype != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (selectedPhenotype != null)
        {
            Vector2 uiPos = WorldToUI(selectedPhenotype.GetBounds().center);
            menu.style.left = uiPos.x + (uiPos.x < doc.panelSettings.referenceResolution.x / 2f ? 100 : -100);
            menu.style.top = uiPos.y + (uiPos.y < doc.panelSettings.referenceResolution.y / 2f ? 100 : -100);
            titleLabel.text = selectedPhenotype.name;
            blockingPanel.Block();
        }
    }

    public void SetInfoText(string text)
    {
        infoLabel.style.display = !string.IsNullOrEmpty(text) ? DisplayStyle.Flex : DisplayStyle.None;
        infoLabel.text = text;        
    }

    public void EnableSaveButton(Action onSave)
    {
        saveButton.clicked += onSave;
        saveButton.style.display = DisplayStyle.Flex;
    }

    public void EnableProtectButton(Action onProtect)
    {
        protectButton.clicked += onProtect;
        protectButton.style.display = DisplayStyle.Flex;
    }

    public void EnableCullButton(Action onCull)
    {
        cullButton.clicked += onCull;
        cullButton.style.display = DisplayStyle.Flex;
    }

    public void EnableBreedButton(Action onBreed)
    {
        breedButton.clicked += onBreed;
        breedButton.style.display = DisplayStyle.Flex;
    }
}
