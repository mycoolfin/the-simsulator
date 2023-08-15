using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public class AncestryExplorer : MonoBehaviour
{
    public FollowCamera followCamera;
    public VisualTreeAsset genotypeTemplate;
    public VisualTreeAsset offspringSpecificationTemplate;
    public VisualTreeAsset mutationOperationTemplate;
    public VisualTreeAsset integrityStatusTemplate;

    private UIDocument doc;

    private VisualElement familyTree;
    private Label ancestryMissingText;

    private Genotype loadedGenotype;
    private Genotype focusedGenotype;
    private Button focusedGenotypeButton;
    private Phenotype phenotype;

    private Color buttonInactiveColor = new(0.74f, 0.74f, 0.74f);
    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;

    private void Start()
    {
        doc = GetComponent<UIDocument>();

        InitialiseFamilyTree();
        InitialisePhenotypeWindow();
        InitialiseFooter();

        WorldManager.Instance.gravity = true;
        WorldManager.Instance.simulateFluid = false;
    }

    private void InitialiseFamilyTree()
    {
        familyTree = doc.rootVisualElement.Q<VisualElement>("family-tree");
        ancestryMissingText = doc.rootVisualElement.Q<Label>("ancestry-missing");
    }

    private void InitialisePhenotypeWindow()
    {
        VisualElement phenotypeWindow = doc.rootVisualElement.Q<VisualElement>("phenotype-window");
        Toggle water = phenotypeWindow.Q<Toggle>("water");
        water.value = WorldManager.Instance.simulateFluid;
        water.RegisterValueChangedCallback((ChangeEvent<bool> e) =>
        {
            WorldManager.Instance.gravity = !e.newValue;
            WorldManager.Instance.simulateFluid = e.newValue;
        });
    }

    private void InitialiseFooter()
    {
        VisualElement footer = doc.rootVisualElement.Q<VisualElement>("footer");
        Button loadedGenotypeButton = footer.Q<Button>("loaded-genotype");
        Button removeLoadedGenotypeButton = footer.Q<Button>("remove-loaded-genotype");
        loadedGenotypeButton.clicked += () =>
        {
            string seedGenotypePath = FileBrowser.Instance.OpenSingleFile(
                "Load Genotype",
                FileBrowser.Instance.CurrentOpenSingleFile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                string.Empty,
                "genotype"
            );
            if (string.IsNullOrEmpty(seedGenotypePath))
                return;

            Genotype genotype = GenotypeSerializer.ReadGenotypeFromFile(seedGenotypePath);
            if (genotype == null)
            {
                loadedGenotypeButton.text = "Validation Error";
                loadedGenotypeButton.style.backgroundColor = buttonErrorColor;
                removeLoadedGenotypeButton.style.display = DisplayStyle.None;
                SetLoadedGenotype(null);
            }
            else
            {
                loadedGenotypeButton.text = "Loaded '" + genotype.Id + "'";
                loadedGenotypeButton.style.backgroundColor = buttonActiveColor;
                removeLoadedGenotypeButton.style.display = DisplayStyle.Flex;
                SetLoadedGenotype(genotype);
            }
        };
        removeLoadedGenotypeButton.clicked += () =>
        {
            loadedGenotypeButton.text = "Select a genotype...";
            loadedGenotypeButton.style.backgroundColor = buttonInactiveColor;
            removeLoadedGenotypeButton.style.display = DisplayStyle.None;
            SetLoadedGenotype(null);
        };
        removeLoadedGenotypeButton.style.display = DisplayStyle.None;

        Button exit = footer.Q<Button>("exit");
        exit.clicked += () => SceneManager.LoadScene("MainMenu");
    }

    private void SetLoadedGenotype(Genotype genotype)
    {
        loadedGenotype = genotype;

        ancestryMissingText.style.display = DisplayStyle.None;

        if (loadedGenotype == null)
        {
            familyTree.Clear();
            ClearPhenotypeWindow();
        }
        else
        {
            if (genotype.Ancestry == null)
            {
                ancestryMissingText.style.display = DisplayStyle.Flex;
            }
            else
            {
                Genotype last = genotype.Ancestry.First.ToGenotype();
                familyTree.Add(InstantiateGenotypeElement(last));
                int childCount = 0;
                foreach (OffspringSpecification offspringSpecification in genotype.Ancestry.OffspringSpecifications)
                {
                    familyTree.Add(InstantiateOffspringSpecification(offspringSpecification));
                    last = Reproduction.CreateOffspringFromSpecification(last, offspringSpecification, "Child #" + ++childCount);
                    familyTree.Add(InstantiateGenotypeElement(last));
                }

                VisualElement integrityStatus = integrityStatusTemplate.Instantiate();
                familyTree.Add(integrityStatus);
                if (GenotypeWithoutAncestry.TestStructuralEquality(loadedGenotype, last)) integrityStatus.Q<VisualElement>("verification-fail").style.display = DisplayStyle.None;
                else integrityStatus.Q<VisualElement>("verification-success").style.display = DisplayStyle.None;
            }

            familyTree.Add(InstantiateGenotypeElement(loadedGenotype));
        }
    }

    private VisualElement InstantiateGenotypeElement(Genotype genotype)
    {
        VisualElement genotypeElement = genotypeTemplate.Instantiate();
        Button genotypeButton = genotypeElement.Q<Button>();
        genotypeButton.text = genotype.Id;
        genotypeButton.clicked += () => SetFocusedGenotype(genotypeButton, genotype);
        return genotypeElement;
    }

    private VisualElement InstantiateOffspringSpecification(OffspringSpecification offspringSpecification)
    {
        VisualElement specification = offspringSpecificationTemplate.Instantiate();

        specification.Q<Label>("recombination-type").text = offspringSpecification.RecombinationOperation.Type.ToString();

        VisualElement mateElement = specification.Q<VisualElement>("mate");
        if (offspringSpecification.RecombinationOperation.Type == RecombinationOperationType.Asexual)
            mateElement.style.display = DisplayStyle.None;
        else
            mateElement.Add(InstantiateGenotypeElement(offspringSpecification.RecombinationOperation.Mate.ToGenotype()));

        VisualElement mutations = specification.Q<VisualElement>("mutations");
        mutations.Q<Foldout>().text = "Mutations (" + offspringSpecification.MutationOperations.Count + ")";
        foreach (MutationOperation mutationOperation in offspringSpecification.MutationOperations)
        {
            VisualElement mutationOperationElement = mutationOperationTemplate.Instantiate();
            Foldout foldout = mutationOperationElement.Q<Foldout>();
            foldout.text = mutationOperation.path;
            foldout.Q<Label>("new-value").text = mutationOperation.newValue;
            mutations.Add(mutationOperationElement);
        }

        return specification;
    }

    private void SetFocusedGenotype(Button button, Genotype genotype)
    {
        focusedGenotypeButton?.RemoveFromClassList("active");
        focusedGenotypeButton = button;
        focusedGenotypeButton?.AddToClassList("active");

        focusedGenotype = genotype;
        if (focusedGenotype != null)
        {
            ClearPhenotypeWindow();
            phenotype = Phenotype.Construct(focusedGenotype);
            PickAndPlaceManager.Instance.PickUp(phenotype, null, null);
            PickAndPlaceManager.Instance.Place(Vector3.zero);
            followCamera.SetTarget(phenotype, "Phenotype");
        }
        else
        {
            phenotype = null;
            followCamera.SetTarget(null, "");
        }
    }

    private void ClearPhenotypeWindow()
    {
        if (phenotype != null)
        {
            Destroy(phenotype.gameObject);
        }
    }
}
