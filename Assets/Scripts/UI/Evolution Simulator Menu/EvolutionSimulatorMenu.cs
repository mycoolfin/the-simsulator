using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public class EvolutionModeMenu : MonoBehaviour
{
    public EvolutionSimulator simulator;
    public PlayerController playerController;
    public SelectedPhenotypeMenu selectedPhenotypeMenu;

    private enum RuntimeMenuTab
    {
        None,
        Parameters,
        Status,
        controls,
        Settings
    }

    private UIDocument doc;

    private VisualElement initialisationMenuContainer;
    private Genotype seedGenotype;

    private VisualElement runtimeMenuBar;
    private VisualElement runtimeMenuContainer;
    private Button parametersTabToggle;
    private Button statusTabToggle;
    private Button controlsTabToggle;
    private Button settingsTabToggle;
    private VisualElement parametersTab;
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
    private LineGraph bestFitnessGraph;
    private LineGraph averageFitnessGraph;

    private Color buttonInactiveColor = new Color(0.74f, 0.74f, 0.74f);
    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;

    private void Start()
    {
        doc = GetComponent<UIDocument>();

        InitialiseInitialisationMenu();
        InitialiseRuntimeMenu();
        InitialiseExitMenu();
        InitialiseSelectedPhenotypeMenu();

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
        SliderInt mutationRate = initialisationMenuContainer.Q<SliderInt>("mutation-rate");
        mutationRate.value = Mathf.FloorToInt(MutationParameters.MutationRate);
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
                MutationParameters.MutationRate = mutationRate.value;
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

        simulator.OnSimulationStart += () => playerController.transform.position += simulator.GetSimulationOrigin().position;
    }

    private void InitialiseRuntimeMenu()
    {
        runtimeMenuBar = doc.rootVisualElement.Q<VisualElement>("runtime-menu-bar");
        parametersTabToggle = runtimeMenuBar.Q<Button>("parameters-tab-toggle");
        statusTabToggle = runtimeMenuBar.Q<Button>("status-tab-toggle");
        controlsTabToggle = runtimeMenuBar.Q<Button>("controls-tab-toggle");
        settingsTabToggle = runtimeMenuBar.Q<Button>("settings-tab-toggle");
        Button exit = runtimeMenuBar.Q<Button>("exit");

        parametersTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Parameters);
        statusTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Status);
        controlsTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.controls);
        settingsTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Settings);
        exit.clicked += () => ShowExitMenu(true);

        runtimeMenuContainer = doc.rootVisualElement.Q<VisualElement>("runtime-menu-container");
        InitialiseParametersTab();
        InitialiseStatusTab();
        InitialiseControlsTab();
        InitialiseSettingsTab();
    }

    private void InitialiseExitMenu()
    {
        exitMenuContainer = doc.rootVisualElement.Q<VisualElement>("exit-menu-container");
        Button reset = exitMenuContainer.Q<Button>("reset");
        Button exit = exitMenuContainer.Q<Button>("exit");
        Button cancel = exitMenuContainer.Q<Button>("cancel");

        reset.clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        exit.clicked += () => SceneManager.LoadScene("MainMenu");
        cancel.clicked += () => ShowExitMenu(false);
    }

    private void InitialiseParametersTab()
    {
        parametersTab = runtimeMenuContainer.Q<VisualElement>("parameters");
        Label trialType = parametersTab.Q<Label>("trial-type");
        Label maxIterations = parametersTab.Q<Label>("max-iterations");
        Label populationSize = parametersTab.Q<Label>("population-size");
        Label survivalPercentage = parametersTab.Q<Label>("survival-percentage");
        Label mutationRate = parametersTab.Q<Label>("mutation-rate");

        simulator.OnSimulationStart += () =>
        {
            trialType.text = Utilities.PascalToSentenceCase(simulator.Trial.ToString());
            maxIterations.text = simulator.MaxIterations == -1 ? "∞" : simulator.MaxIterations.ToString();
            populationSize.text = simulator.PopulationSize.ToString();
            survivalPercentage.text = Mathf.FloorToInt(simulator.SurvivalPercentage * 100f).ToString() + "%";
            mutationRate.text = MutationParameters.MutationRate == 0 ? "Mutation disabled" : "~" + MutationParameters.MutationRate.ToString() + " mutation" + (MutationParameters.MutationRate == 1f ? "" : "s") + "/child";
        };
    }

    private void InitialiseStatusTab()
    {
        statusTab = runtimeMenuContainer.Q<VisualElement>("status");
        iterationProgress = statusTab.Q<ProgressBar>("iteration-progress");
        assessmentProgress = statusTab.Q<ProgressBar>("assessment-progress");
        bestFitness = statusTab.Q<Label>("best-fitness");
        averageFitness = statusTab.Q<Label>("average-fitness");
        VisualElement bestFitnessGraphContainer = statusTab.Q<VisualElement>("best-fitness-graph");
        VisualElement averageFitnessGraphContainer = statusTab.Q<VisualElement>("average-fitness-graph");
        bestFitnessGraph = new LineGraph(bestFitnessGraphContainer, Color.cyan);
        averageFitnessGraph = new LineGraph(averageFitnessGraphContainer, Color.yellow);

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
    }

    private void InitialiseSettingsTab()
    {
        settingsTab = runtimeMenuContainer.Q<VisualElement>("settings");
        Toggle colourByFitness = settingsTab.Q<Toggle>("colour-by-fitness");
        colourByFitness.value = simulator.colourByRelativeFitness;
        colourByFitness.RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.colourByRelativeFitness = e.newValue);
        SliderInt focusBestCreatures = settingsTab.Q<SliderInt>("focus-best");
        focusBestCreatures.RegisterValueChangedCallback((ChangeEvent<int> e) => simulator.focusBestCreatures = e.newValue);
        Toggle orbitCamera = settingsTab.Q<Toggle>("orbit-camera");
        orbitCamera.value = playerController.orbitTarget != null;
        orbitCamera.RegisterValueChangedCallback((ChangeEvent<bool> e) => playerController.orbitTarget = e.newValue ? simulator.GetSimulationOrigin() : null);
    }

    private void InitialiseSelectedPhenotypeMenu()
    {
        selectedPhenotypeMenu.EnableSaveButton(() => selectedPhenotypeMenu.SelectedPhenotype?.SaveGenotypeToFile());
        selectedPhenotypeMenu.EnableProtectButton(() =>
        {
            Individual individual = simulator.GetIndividualByPhenotype(selectedPhenotypeMenu.SelectedPhenotype);
            if (individual != null)
                individual.isProtected = !individual.isProtected;
        });
        selectedPhenotypeMenu.EnableCullButton(() => simulator.GetIndividualByPhenotype(selectedPhenotypeMenu.SelectedPhenotype)?.Cull());

        SelectionManager.Instance.OnSelection += (previouslySelected, selected) =>
            selectedPhenotypeMenu.SetTarget(selected?.gameObject.GetComponent<Phenotype>());

        simulator.OnIterationStart += () => selectedPhenotypeMenu.SetTarget(null);
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
        if (tab == RuntimeMenuTab.Parameters) EnableRuntimeTab(parametersTab, parametersTabToggle); else DisableRuntimeTab(parametersTab, parametersTabToggle);
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
        : "Iteration " + simulator.currentIteration + "/" + (numberOfIterations == -1 ? "∞" : numberOfIterations);

        bestFitness.text = simulator.bestFitnesses.Count == 0 ? "?" : simulator.bestFitnesses[simulator.bestFitnesses.Count - 1].ToString("0.000");
        averageFitness.text = simulator.averageFitnesses.Count == 0 ? "?" : simulator.averageFitnesses[simulator.averageFitnesses.Count - 1].ToString("0.000");

        bestFitnessGraph.SetPoints(simulator.bestFitnesses);
        averageFitnessGraph.SetPoints(simulator.averageFitnesses);
    }
}
