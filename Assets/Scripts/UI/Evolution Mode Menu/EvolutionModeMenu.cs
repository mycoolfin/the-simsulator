using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public enum RuntimeMenuTab
{
    None,
    Status,
    controls,
    Settings
}

public class EvolutionModeMenu : MonoBehaviour
{
    public EvolutionSimulator simulator;
    public PlayerController playerController;
    public CameraGrid cameraGrid;

    private UIDocument doc;

    private VisualElement initialisationMenuContainer;
    private Genotype seedGenotype;

    private VisualElement runtimeMenuBar;
    private VisualElement runtimeMenuContainer;
    private Button statusTabToggle;
    private Button controlsTabToggle;
    private Button settingsTabToggle;
    private VisualElement statusTab;
    private VisualElement controlsTab;
    private VisualElement settingsTab;
    private RuntimeMenuTab currentRuntimeMenuTab;

    private VisualElement exitMenuContainer;

    private ProgressBar throttledTime;
    private ProgressBar iterationProgress;
    private ProgressBar assessmentProgress;
    private int numberOfIterations;
    private Label bestFitness;
    private Label averageFitness;
    private VisualElement selectedPhenotypeOptions;

    private Color buttonInactiveColor = new Color(0.74f, 0.74f, 0.74f);
    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;
    private void Start()
    {
        doc = GetComponent<UIDocument>();

        InitialiseInitialisationMenu();
        InitialiseRuntimeMenu();
        InitialiseExitMenu();

        ShowInitialisationMenu(true);
        ShowRuntimeMenu(false);
        ShowExitMenu(false);
    }

    private void Update()
    {
        if (simulator.running)
        {
            if (WorldManager.Instance.throttledTimeScale <= 1f)
                throttledTime.value = WorldManager.Instance.throttledTimeScale / 2f;
            else
                throttledTime.value = 0.5f + ((WorldManager.Instance.throttledTimeScale - 1) / (WorldManager.maxTimeScale - 1) / 2f);

            assessmentProgress.value = simulator.assessmentProgress;
            assessmentProgress.title = simulator.assessmentProgress < 0f ? "Waiting for creatures to settle..." :
                simulator.assessmentProgress < 1f ? "Assessing fitness..." : "Fitness assessment complete.";
        }
    }

    private void InitialiseInitialisationMenu()
    {
        initialisationMenuContainer = doc.rootVisualElement.Q<VisualElement>("initialisation-menu-container");
        DropdownField trialType = initialisationMenuContainer.Q<DropdownField>("trial-type");
        trialType.choices = Enum.GetNames(typeof(TrialType)).Select(name => Utilities.PascalToSentenceCase(name)).ToList();
        trialType.index = 0;
        SliderInt maxIterations = initialisationMenuContainer.Q<SliderInt>("max-iterations");
        SliderInt populationSize = initialisationMenuContainer.Q<SliderInt>("population-size");
        SliderInt survivalPercentage = initialisationMenuContainer.Q<SliderInt>("survival-percentage");
        Button seedGenotypeButton = initialisationMenuContainer.Q<Button>("seed-genotype");
        Button removeSeedButton = initialisationMenuContainer.Q<Button>("remove-seed");
        seedGenotypeButton.clicked += () =>
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
                seedGenotypeButton.text = "Validation Error";
                seedGenotypeButton.style.backgroundColor = buttonErrorColor;
                removeSeedButton.style.display = DisplayStyle.None;
                seedGenotype = null;
            }
            else
            {
                seedGenotypeButton.text = "Loaded '" + genotype.Id + "'";
                seedGenotypeButton.style.backgroundColor = buttonActiveColor;
                removeSeedButton.style.display = DisplayStyle.Flex;
                seedGenotype = genotype;
            }
        };
        removeSeedButton.clicked += () =>
        {
            seedGenotypeButton.text = "<none>";
            seedGenotypeButton.style.backgroundColor = buttonInactiveColor;
            removeSeedButton.style.display = DisplayStyle.None;
            seedGenotype = null;
        };
        removeSeedButton.style.display = DisplayStyle.None;

        initialisationMenuContainer.Q<Button>("run").clicked += () =>
        {
            if (!simulator.running)
            {
                ShowInitialisationMenu(false);
                ShowRuntimeMenu(true);
                TrialType t;
                Enum.TryParse<TrialType>(Utilities.SentenceToPascalCase(trialType.value), out t);
                numberOfIterations = maxIterations.value;
                StartCoroutine(simulator.Run(
                    t,
                    maxIterations.value,
                    populationSize.value,
                    (float)survivalPercentage.value / 100f,
                    seedGenotype
                ));
            }
        };

        initialisationMenuContainer.Q<Button>("exit").clicked += () => SceneManager.LoadScene("MainMenu");

        simulator.OnSimulationStart += () => playerController.transform.position += simulator.trialOrigin.position;
    }

    private void InitialiseRuntimeMenu()
    {
        runtimeMenuBar = doc.rootVisualElement.Q<VisualElement>("runtime-menu-bar");
        statusTabToggle = runtimeMenuBar.Q<Button>("status-tab-toggle");
        controlsTabToggle = runtimeMenuBar.Q<Button>("controls-tab-toggle");
        settingsTabToggle = runtimeMenuBar.Q<Button>("settings-tab-toggle");
        Button exit = runtimeMenuBar.Q<Button>("exit");

        statusTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Status);
        controlsTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.controls);
        settingsTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Settings);
        exit.clicked += () => ShowExitMenu(true);

        runtimeMenuContainer = doc.rootVisualElement.Q<VisualElement>("runtime-menu-container");
        InitialiseStatusTab();
        InitialiseControlsTab();
        InitialiseSettingsTab();
    }

    private void InitialiseExitMenu()
    {
        exitMenuContainer = doc.rootVisualElement.Q<VisualElement>("exit-menu-container");
        Button exit = exitMenuContainer.Q<Button>("exit");
        Button cancel = exitMenuContainer.Q<Button>("cancel");

        exit.clicked += () => SceneManager.LoadScene("MainMenu");
        cancel.clicked += () => ShowExitMenu(false);
    }

    private void InitialiseStatusTab()
    {
        statusTab = runtimeMenuContainer.Q<VisualElement>("status");
        iterationProgress = statusTab.Q<ProgressBar>("iteration-progress");
        assessmentProgress = statusTab.Q<ProgressBar>("assessment-progress");
        bestFitness = statusTab.Q<Label>("best-fitness");
        averageFitness = statusTab.Q<Label>("average-fitness");

        simulator.OnIterationStart += () => UpdateStatus(false);
        simulator.OnIterationEnd += () => iterationProgress.title += " (Loading...)";
        simulator.OnSimulationEnd += () => UpdateStatus(true);
    }

    private void InitialiseControlsTab()
    {
        controlsTab = runtimeMenuContainer.Q<VisualElement>("controls");
        Button pauseButton = controlsTab.Q<Button>("pause-simulation");
        Button playButton = controlsTab.Q<Button>("play-simulation");
        Button fastForwardButton = controlsTab.Q<Button>("fastforward-simulation");
        pauseButton.style.backgroundColor = buttonInactiveColor;
        playButton.style.backgroundColor = buttonActiveColor;
        fastForwardButton.style.backgroundColor = buttonInactiveColor;
        pauseButton.clicked += () =>
        {
            WorldManager.Instance.timeScale = 0f;
            pauseButton.style.backgroundColor = buttonActiveColor;
            playButton.style.backgroundColor = buttonInactiveColor;
            fastForwardButton.style.backgroundColor = buttonInactiveColor;
        };
        playButton.clicked += () =>
        {
            WorldManager.Instance.timeScale = 1f;
            pauseButton.style.backgroundColor = buttonInactiveColor;
            playButton.style.backgroundColor = buttonActiveColor;
            fastForwardButton.style.backgroundColor = buttonInactiveColor;
        };
        fastForwardButton.clicked += () =>
        {
            WorldManager.Instance.timeScale = 5f;
            pauseButton.style.backgroundColor = buttonInactiveColor;
            playButton.style.backgroundColor = buttonInactiveColor;
            fastForwardButton.style.backgroundColor = buttonActiveColor;
        };
        throttledTime = controlsTab.Q<ProgressBar>("throttled-time");
        controlsTab.Q<Toggle>("pause-iterating").RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.pauseIterating = e.newValue);
        controlsTab.Q<Button>("reset").clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void InitialiseSettingsTab()
    {
        settingsTab = runtimeMenuContainer.Q<VisualElement>("settings");
        Toggle colourByFitness = settingsTab.Q<Toggle>("colour-by-fitness");
        colourByFitness.value = simulator.colourByRelativeFitness;
        colourByFitness.RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.colourByRelativeFitness = e.newValue);
        DropdownField show = settingsTab.Q<DropdownField>("show");
        show.choices = Enum.GetNames(typeof(DisplayFilter)).Select(name => Utilities.PascalToSentenceCase(name)).ToList();
        show.index = 0;
        show.RegisterValueChangedCallback((ChangeEvent<string> e) => Enum.TryParse<DisplayFilter>(Utilities.SentenceToPascalCase(e.newValue), out simulator.filterBy));
        Toggle orbitCamera = settingsTab.Q<Toggle>("orbit-camera");
        orbitCamera.value = playerController.orbitTarget != null;
        orbitCamera.RegisterValueChangedCallback((ChangeEvent<bool> e) => playerController.orbitTarget = e.newValue ? simulator.trialOrigin : null);
    }

    private void ShowInitialisationMenu(bool show)
    {
        initialisationMenuContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowRuntimeMenu(bool show)
    {
        runtimeMenuBar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        ToggleRuntimeMenuTab(RuntimeMenuTab.None);
    }

    private void ShowExitMenu(bool show)
    {
        exitMenuContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ToggleRuntimeMenuTab(RuntimeMenuTab tab)
    {
        if (tab == currentRuntimeMenuTab)
            tab = RuntimeMenuTab.None;

        if (tab == RuntimeMenuTab.None) runtimeMenuContainer.style.display = DisplayStyle.None; else runtimeMenuContainer.style.display = DisplayStyle.Flex;
        if (tab == RuntimeMenuTab.Status) EnableRuntimeTab(statusTab, statusTabToggle); else DisableRuntimeTab(statusTab, statusTabToggle);
        if (tab == RuntimeMenuTab.controls) EnableRuntimeTab(controlsTab, controlsTabToggle); else DisableRuntimeTab(controlsTab, controlsTabToggle);
        if (tab == RuntimeMenuTab.Settings) EnableRuntimeTab(settingsTab, settingsTabToggle); else DisableRuntimeTab(settingsTab, settingsTabToggle);

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

    private void UpdateStatus(bool simulationComplete)
    {
        iterationProgress.value = (float)simulator.currentIteration / (float)numberOfIterations;
        iterationProgress.title = simulationComplete
        ? "Simulation Complete (" + numberOfIterations + " iteration" + (numberOfIterations != 1 ? "s" : "") + ")"
        : "Iteration " + simulator.currentIteration + "/" + numberOfIterations;

        bestFitness.text = simulator.bestFitnesses.Count == 0 ? "?" : simulator.bestFitnesses[simulator.bestFitnesses.Count - 1].ToString("0.000");
        averageFitness.text = simulator.averageFitnesses.Count == 0 ? "?" : simulator.averageFitnesses[simulator.averageFitnesses.Count - 1].ToString("0.000");
    }

    // private void InitialiseSelectedPhenotypeOptionsPanel()
    // {
    //     selectedPhenotypeOptions = doc.rootVisualElement.Q<VisualElement>("selected-phenotype-options");
    //     Button saveToFileButton = doc.rootVisualElement.Q<Button>("save-to-file");
    //     Button protectButton = doc.rootVisualElement.Q<Button>("protect");
    //     Button cullButton = doc.rootVisualElement.Q<Button>("cull");
    //     Label phenotypeOptionsLog = doc.rootVisualElement.Q<Label>("phenotype-options-log");
    //     saveToFileButton.clicked += () =>
    //     {
    //         Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
    //         string savePath = FileBrowser.Instance.SaveFile(
    //             "Save Genotype",
    //             FileBrowser.Instance.CurrentSaveFile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    //             selectedPhenotype.genotype.Id,
    //             "genotype"
    //         );
    //         string savedName = Path.GetFileNameWithoutExtension(savePath);
    //         bool saveSuccess = false;
    //         if (!string.IsNullOrEmpty(savedName))
    //         {
    //             Genotype genotypeToSave = Genotype.Construct(
    //                 savedName,
    //                 selectedPhenotype.genotype.Lineage,
    //                 selectedPhenotype.genotype.BrainNeuronDefinitions,
    //                 selectedPhenotype.genotype.LimbNodes
    //             );
    //             saveSuccess = !string.IsNullOrEmpty(genotypeToSave.SaveToFile(savePath));
    //         }
    //         phenotypeOptionsLog.style.display = DisplayStyle.Flex;
    //         phenotypeOptionsLog.text = saveSuccess ? "Saved genotype to " + savePath : "Error saving genotype to file.";
    //         phenotypeOptionsLog.style.color = saveSuccess ? Color.black : Color.red;
    //     };
    //     protectButton.clicked += () =>
    //     {
    //         Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
    //         bool isProtected = simulator.TogglePhenotypeProtection(selectedPhenotype);
    //         phenotypeOptionsLog.style.display = DisplayStyle.Flex;
    //         phenotypeOptionsLog.text = isProtected ? "Protected phenotype." : "Removed phenotype protection.";
    //         phenotypeOptionsLog.style.color = Color.black;
    //     };
    //     cullButton.clicked += () =>
    //     {
    //         Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
    //         Destroy(selectedPhenotype.gameObject);
    //         SelectionManager.Instance.Selected = null;
    //     };

    //     SelectionManager.Instance.OnSelection += () =>
    //     {
    //         selectedPhenotypeOptions.style.display = ((Phenotype)SelectionManager.Instance.Selected) != null ? DisplayStyle.Flex : DisplayStyle.None;
    //         phenotypeOptionsLog.text = "";
    //         phenotypeOptionsLog.style.display = DisplayStyle.None;
    //     };

    //     simulator.OnIterationStart += () => selectedPhenotypeOptions.style.display = DisplayStyle.None;
    //     simulator.OnSimulationEnd += () => selectedPhenotypeOptions.style.display = DisplayStyle.None;
    // }
}
