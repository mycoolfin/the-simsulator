using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public class AquariumModeMenu : MonoBehaviour
{
    public PlayerController playerController;

    private UIDocument doc;

    private Color buttonInactiveColor = new Color(0.74f, 0.74f, 0.74f);
    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;

    private Genotype loadedGenotype;
    Button placeCreatureButton;
    Button breedCreatureButton;
    private bool placingCreature;
    private bool breedingCreature;
    private Phenotype readyParent;

    private void Start()
    {
        doc = GetComponent<UIDocument>();
        InitialiseMenu();
        InitialiseEnvironmentPanel();
        InitialiseAddCreaturePanel();
        InitialiseInteractionPanel();
        InitialiseSelectedCreatureOptionsPanel();
    }

    private void InitialiseMenu()
    {
        VisualElement menu = doc.rootVisualElement.Q<VisualElement>("menu");
        doc.rootVisualElement.Q<Button>("menu-open").clicked += () => menu.style.left = 0;
        doc.rootVisualElement.Q<Button>("menu-close").clicked += () => menu.style.left = menu.resolvedStyle.width;
        doc.rootVisualElement.Q<Button>("exit").clicked += () => SceneManager.LoadScene("MainMenu");
    }

    private void InitialiseEnvironmentPanel()
    {
        Toggle gravity = doc.rootVisualElement.Q<Toggle>("gravity");
        gravity.value = WorldManager.Instance.gravity;
        gravity.RegisterValueChangedCallback((ChangeEvent<bool> e) => WorldManager.Instance.gravity = e.newValue);
        Toggle water = doc.rootVisualElement.Q<Toggle>("water");
        water.value = WorldManager.Instance.simulateFluid;
        water.RegisterValueChangedCallback((ChangeEvent<bool> e) => WorldManager.Instance.simulateFluid = e.newValue);
        DropdownField timeOfDay = doc.rootVisualElement.Q<DropdownField>("time-of-day");
        timeOfDay.choices = Enum.GetNames(typeof(TimeOfDay)).Select(name => Utilities.PascalToSentenceCase(name)).ToList();
        timeOfDay.index = 1;
        timeOfDay.RegisterValueChangedCallback((ChangeEvent<string> e) =>
        {
            TimeOfDay t;
            Enum.TryParse<TimeOfDay>(Utilities.SentenceToPascalCase(timeOfDay.value), out t);
            WorldManager.Instance.ChangeTimeOfDay(t);
        });
        WorldManager.Instance.ChangeTimeOfDay((TimeOfDay)Enum.GetValues(typeof(TimeOfDay)).GetValue(timeOfDay.index));
    }

    private void InitialiseAddCreaturePanel()
    {
        Button genotypeButton = doc.rootVisualElement.Q<Button>("genotype");
        Button removeGenotypeButton = doc.rootVisualElement.Q<Button>("remove-genotype");
        genotypeButton.clicked += () =>
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
                genotypeButton.text = "Validation Error";
                genotypeButton.style.backgroundColor = buttonErrorColor;
                removeGenotypeButton.style.display = DisplayStyle.None;
                loadedGenotype = null;
            }
            else
            {
                genotypeButton.text = "Loaded '" + genotype.Id + "'";
                genotypeButton.style.backgroundColor = buttonActiveColor;
                removeGenotypeButton.style.display = DisplayStyle.Flex;
                loadedGenotype = genotype;
            }
        };
        removeGenotypeButton.clicked += () =>
        {
            genotypeButton.text = "<none>";
            genotypeButton.style.backgroundColor = buttonInactiveColor;
            removeGenotypeButton.style.display = DisplayStyle.None;
            loadedGenotype = null;
        };
        removeGenotypeButton.style.display = DisplayStyle.None;

        placeCreatureButton = doc.rootVisualElement.Q<Button>("place-creature");
        placeCreatureButton.clicked += () =>
        {
            if (placingCreature)
                TogglePlacing(false);
            else if (loadedGenotype != null)
            {
                TogglePlacing(true);
                Phenotype p = Phenotype.Construct(loadedGenotype);
                PickAndPlaceManager.Instance.PickUp(
                    p,
                    () => TogglePlacing(false),
                    () => { TogglePlacing(false); Destroy(p.gameObject); }
                );
            }
        };
    }

    private void InitialiseInteractionPanel()
    {
        Toggle emitLightFromSelf = doc.rootVisualElement.Q<Toggle>("emit-light-from-self");
        emitLightFromSelf.RegisterValueChangedCallback((ChangeEvent<bool> e) => 
        {
            if (e.newValue)
            {
                WorldManager.Instance.pointLight.transform.position = playerController.transform.position;
                WorldManager.Instance.pointLight.transform.parent = playerController.transform;
                WorldManager.Instance.pointLight.SetActive(true);
            }
            else
                WorldManager.Instance.pointLight.SetActive(false);
        });
    }

    private void InitialiseSelectedCreatureOptionsPanel()
    {
        VisualElement selectedCreatureOptions = doc.rootVisualElement.Q<VisualElement>("selected-creature-options");
        Button saveToFileButton = doc.rootVisualElement.Q<Button>("save-to-file");
        breedCreatureButton = doc.rootVisualElement.Q<Button>("breed");
        Button removeButton = doc.rootVisualElement.Q<Button>("remove");
        Label creatureOptionsLog = doc.rootVisualElement.Q<Label>("creature-options-log");
        saveToFileButton.clicked += () =>
        {
            Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
            string savePath = FileBrowser.Instance.SaveFile(
                "Save Genotype",
                FileBrowser.Instance.CurrentSaveFile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                selectedPhenotype.genotype.Id,
                "genotype"
            );
            string savedName = Path.GetFileNameWithoutExtension(savePath);
            bool saveSuccess = false;
            if (!string.IsNullOrEmpty(savedName))
            {
                Genotype genotypeToSave = Genotype.Construct(
                    savedName,
                    selectedPhenotype.genotype.Lineage,
                    selectedPhenotype.genotype.BrainNeuronDefinitions,
                    selectedPhenotype.genotype.LimbNodes
                );
                saveSuccess = !string.IsNullOrEmpty(genotypeToSave.SaveToFile(savePath));
            }
            creatureOptionsLog.style.display = DisplayStyle.Flex;
            creatureOptionsLog.text = saveSuccess ? "Saved genotype to " + savePath : "Error saving genotype to file.";
            creatureOptionsLog.style.color = saveSuccess ? Color.black : Color.red;
        };
        breedCreatureButton.clicked += () =>
        {
            if (breedingCreature)
            {
                ToggleBreeding(false);
                creatureOptionsLog.text = "";
                creatureOptionsLog.style.display = DisplayStyle.None;
                return;
            }

            Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
            if (selectedPhenotype != null)
            {
                ToggleBreeding(true);
                creatureOptionsLog.style.display = DisplayStyle.Flex;
                creatureOptionsLog.text = "Select a creature to breed with...";
                creatureOptionsLog.style.color = Color.black;
            }
        };
        removeButton.clicked += () =>
        {
            Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
            Destroy(selectedPhenotype.gameObject);
            SelectionManager.Instance.Selected = null;
        };

        SelectionManager.Instance.OnSelection += () =>
        {
            selectedCreatureOptions.style.display = ((Phenotype)SelectionManager.Instance.Selected) != null ? DisplayStyle.Flex : DisplayStyle.None;
            creatureOptionsLog.text = "";
            creatureOptionsLog.style.display = DisplayStyle.None;
        };

        SelectionManager.Instance.OnSelection += () =>
        {
            if (readyParent != null)
            {
                Phenotype potentialParent = (Phenotype)SelectionManager.Instance.Selected;
                if (potentialParent != null)
                {
                    Genotype childGenotype = Reproduction.CreateOffspring(readyParent.genotype, potentialParent.genotype, 0f, 0.5f, 0.5f);
                    Phenotype child = Phenotype.Construct(childGenotype);
                    TogglePlacing(true);
                    PickAndPlaceManager.Instance.PickUp(
                        child,
                        () => TogglePlacing(false),
                        () => { TogglePlacing(false); Destroy(child.gameObject); }
                    );
                }
                ToggleBreeding(false);
            }

        };

        selectedCreatureOptions.style.display = DisplayStyle.None;
    }

    private void TogglePlacing(bool placing)
    {
        placingCreature = placing;
        placeCreatureButton.text = placingCreature ? "Placing..." : "Place Creature";
        if (placing)
            ToggleBreeding(false);
        if (!placing && PickAndPlaceManager.Instance.Held != null)
            PickAndPlaceManager.Instance.Cancel();
    }

    private void ToggleBreeding(bool breeding)
    {
        breedingCreature = breeding;
        breedCreatureButton.text = breedingCreature ? "Breeding..." : "Breed";
        if (breeding)
        {
            TogglePlacing(false);
            readyParent = (Phenotype)SelectionManager.Instance.Selected;
        }
        else
            readyParent = null;
    }
}
