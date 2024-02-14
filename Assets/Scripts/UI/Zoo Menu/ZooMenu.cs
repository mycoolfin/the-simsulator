using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public class ZooModeMenu : MonoBehaviour
{
    public PlayerController playerController;
    public SelectedPhenotypeMenu selectedPhenotypeMenu;

    private enum RuntimeMenuTab
    {
        None,
        Creature,
        Interaction,
        Environment
    }

    private UIDocument doc;

    private VisualElement runtimeMenuBar;
    private VisualElement runtimeMenuContainer;
    private Button creatureTabToggle;
    private Button interactionTabToggle;
    private Button environmentTabToggle;
    private VisualElement creatureTab;
    private VisualElement interactionTab;
    private VisualElement environmentTab;
    private RuntimeMenuTab currentRuntimeMenuTab;

    private VisualElement exitMenuContainer;

    private Genotype loadedGenotype;
    Button placeCreatureButton;
    private bool placingCreature;
    private bool breedingCreature;
    private Phenotype readyParent;

    private Color buttonInactiveColor = new Color(0.74f, 0.74f, 0.74f);
    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;

    private void Start()
    {
        doc = GetComponent<UIDocument>();
        InitialiseRuntimeMenu();
        InitialiseExitMenu();
        InitialiseSelectedPhenotypeMenu();

        ToggleRuntimeMenuTab(RuntimeMenuTab.Creature);
        ShowExitMenu(false);
    }

    private void InitialiseRuntimeMenu()
    {
        runtimeMenuBar = doc.rootVisualElement.Q<VisualElement>("runtime-menu-bar");
        creatureTabToggle = runtimeMenuBar.Q<Button>("creature-tab-toggle");
        interactionTabToggle = runtimeMenuBar.Q<Button>("interaction-tab-toggle");
        environmentTabToggle = runtimeMenuBar.Q<Button>("environment-tab-toggle");
        Button exit = runtimeMenuBar.Q<Button>("exit");

        creatureTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Creature);
        interactionTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Interaction);
        environmentTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Environment);
        exit.clicked += () => ShowExitMenu(true);

        runtimeMenuContainer = doc.rootVisualElement.Q<VisualElement>("runtime-menu-container");
        InitialiseCreatureTab();
        InitialiseInteractionTab();
        InitialiseEnvironmentTab();
    }

    private void InitialiseCreatureTab()
    {
        creatureTab = doc.rootVisualElement.Q<VisualElement>("creature");
        Button genotypeButton = creatureTab.Q<Button>("genotype");
        Button removeGenotypeButton = creatureTab.Q<Button>("remove-genotype");
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
            genotypeButton.text = "Select a genotype...";
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

    private void InitialiseInteractionTab()
    {
        interactionTab = doc.rootVisualElement.Q<VisualElement>("interaction");
        Toggle emitLightFromSelf = interactionTab.Q<Toggle>("emit-light-from-self");
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

    private void InitialiseEnvironmentTab()
    {
        environmentTab = doc.rootVisualElement.Q<VisualElement>("environment");
        Toggle water = environmentTab.Q<Toggle>("water");
        water.value = WorldManager.Instance.simulateFluid;
        water.RegisterValueChangedCallback((ChangeEvent<bool> e) =>
        {
            WorldManager.Instance.gravity = !e.newValue;
            WorldManager.Instance.simulateFluid = e.newValue;
        });
        DropdownField timeOfDay = environmentTab.Q<DropdownField>("time-of-day");
        timeOfDay.choices = Enum.GetNames(typeof(TimeOfDay)).Select(name => Utilities.PascalToSentenceCase(name)).ToList();
        timeOfDay.index = 1;
        timeOfDay.RegisterValueChangedCallback((ChangeEvent<string> e) =>
        {
            TimeOfDay t;
            Enum.TryParse<TimeOfDay>(Utilities.SentenceToPascalCase(timeOfDay.value), out t);
            WorldManager.Instance.ChangeTimeOfDay(t);
        });
        WorldManager.Instance.ChangeTimeOfDay((TimeOfDay)Enum.GetValues(typeof(TimeOfDay)).GetValue(timeOfDay.index));
        SliderInt mutationRate = environmentTab.Q<SliderInt>("mutation-rate");
        mutationRate.value = Mathf.FloorToInt(MutationParameters.MutationRate);
        mutationRate.RegisterValueChangedCallback((ChangeEvent<int> e) => MutationParameters.MutationRate = e.newValue);
    }

    private void InitialiseExitMenu()
    {
        exitMenuContainer = doc.rootVisualElement.Q<VisualElement>("exit-menu-container");
        Button exit = exitMenuContainer.Q<Button>("exit");
        Button cancel = exitMenuContainer.Q<Button>("cancel");

        exit.clicked += () => SceneManager.LoadScene("MainMenu");
        cancel.clicked += () => ShowExitMenu(false);
    }

    private void ShowExitMenu(bool show)
    {
        exitMenuContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void InitialiseSelectedPhenotypeMenu()
    {
        selectedPhenotypeMenu.EnableSaveButton(() => selectedPhenotypeMenu.SelectedPhenotype?.SaveGenotypeToFile());
        selectedPhenotypeMenu.EnableCullButton(() => Destroy(selectedPhenotypeMenu.SelectedPhenotype.gameObject));
        selectedPhenotypeMenu.EnableBreedButton(() =>
        {
            if (breedingCreature)
                ToggleBreeding(false);
            else if (selectedPhenotypeMenu.SelectedPhenotype != null)
                ToggleBreeding(true);
        });

        SelectionManager.Instance.OnSelectionChange += (previouslySelected, selected) =>
        {
            selectedPhenotypeMenu.SetTarget(selected.Count > 0 ? selected[0].gameObject.GetComponent<Phenotype>() : null);
        };

        SelectionManager.Instance.OnSelectionChange += (previouslySelected, selected) =>
        {
            if (readyParent != null)
            {
                Phenotype potentialParent = selected.Count == 0 ? null : (Phenotype)selected[0];
                if (potentialParent != null)
                {
                    Genotype childGenotype = Reproduction.CreateOffspringWithChance(readyParent.genotype, potentialParent.genotype, 0f, 0.5f, 0.5f);
                    Phenotype child = Phenotype.Construct(childGenotype);
                    TogglePlacing(true);
                    PickAndPlaceManager.Instance.PickUp(
                        child,
                        () => { TogglePlacing(false); selectedPhenotypeMenu.SetTarget(child); },
                        () => { TogglePlacing(false); Destroy(child.gameObject); }
                    );
                }
                ToggleBreeding(false);
            }
        };
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
        playerController.toggleOnSelect = !breeding;
        selectedPhenotypeMenu.SetInfoText(breedingCreature ? "Select a creature to breed with..." : null);
        if (breeding)
        {
            TogglePlacing(false);
            readyParent = selectedPhenotypeMenu.SelectedPhenotype;
        }
        else
            readyParent = null;
    }

    private void ToggleRuntimeMenuTab(RuntimeMenuTab tab)
    {
        if (tab == currentRuntimeMenuTab)
            tab = RuntimeMenuTab.None;

        if (tab == RuntimeMenuTab.None) runtimeMenuContainer.style.display = DisplayStyle.None; else runtimeMenuContainer.style.display = DisplayStyle.Flex;
        if (tab == RuntimeMenuTab.Creature) EnableRuntimeTab(creatureTab, creatureTabToggle); else DisableRuntimeTab(creatureTab, creatureTabToggle);
        if (tab == RuntimeMenuTab.Interaction) EnableRuntimeTab(interactionTab, interactionTabToggle); else DisableRuntimeTab(interactionTab, interactionTabToggle);
        if (tab == RuntimeMenuTab.Environment) EnableRuntimeTab(environmentTab, environmentTabToggle); else DisableRuntimeTab(environmentTab, environmentTabToggle);

        currentRuntimeMenuTab = tab;
    }

    private void EnableRuntimeTab(VisualElement tab, Button toggle)
    {
        tab.style.display = DisplayStyle.Flex;
        toggle.AddToClassList("active");
    }

    private void DisableRuntimeTab(VisualElement tab, Button toggle)
    {
        tab.style.display = DisplayStyle.None;
        toggle.RemoveFromClassList("active");
    }
}
