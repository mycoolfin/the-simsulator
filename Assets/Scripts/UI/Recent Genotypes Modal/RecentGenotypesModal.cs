using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class RecentGenotypesModal : MonoBehaviour
{
    public VisualTreeAsset recentGenotypeTemplate;

    public event Action<Genotype> OnSelect = delegate { };

    private UIDocument doc;

    private void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        Initialise();
    }

    private void Initialise()
    {
        VisualElement modal = doc.rootVisualElement.Q<VisualElement>("modal");
        Button closeButton = modal.Q<Button>("close");
        closeButton.clicked += () => gameObject.SetActive(false);
        VisualElement noRecentsWarning = modal.Q<VisualElement>("no-recents-warning");
        ScrollView scrollView = modal.Q<ScrollView>();

        noRecentsWarning.style.display = GenotypeMemory.RecentGenotypes.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;
        scrollView.style.display = GenotypeMemory.RecentGenotypes.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        foreach (RecordedGenotype g in GenotypeMemory.RecentGenotypes.Reverse())
        {
            VisualElement recentGenotypeElement = recentGenotypeTemplate.Instantiate();
            scrollView.Add(recentGenotypeElement);

            Label id = recentGenotypeElement.Q<Label>("id");
            Label recordedAt = recentGenotypeElement.Q<Label>("recorded-at");
            Label description = recentGenotypeElement.Q<Label>("description");
            VisualElement phenotypeDisplay = recentGenotypeElement.Q<VisualElement>("phenotype-display");
            Button selectButton = recentGenotypeElement.Q<Button>("select");

            id.text = g.genotype.Id;
            recordedAt.text = g.dateTime.ToShortTimeString();
            description.text = g.description;
            phenotypeDisplay.style.backgroundImage = g.image;
            selectButton.clicked += () => { OnSelect(g.genotype); gameObject.SetActive(false); };
        }
    }
}
