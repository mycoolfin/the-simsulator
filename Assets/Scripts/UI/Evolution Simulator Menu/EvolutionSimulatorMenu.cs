using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public class EvolutionSimulatorMenu : MonoBehaviour
{
    public EvolutionSimulator simulator;
    public PlayerController playerController;
    public SelectedPhenotypeMenu selectedPhenotypeMenu;
    public FocusGrid focusGrid;

    private enum RuntimeMenuTab
    {
        None,
        Parameters,
        Status,
        Controls,
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
    private VisualElement tooltipElement;

    private ProgressBar throttledTime;
    private ProgressBar iterationProgress;
    private ProgressBar assessmentProgress;
    private int numberOfIterations;
    private Label bestFitness;
    private Label averageFitness;
    private LineGraph bestFitnessGraph;
    private LineGraph averageFitnessGraph;

    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;

    private void Start()
    {
        doc = GetComponent<UIDocument>();

        InitialiseTooltip();
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
        if (simulator.Running)
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

    private void InitialiseTooltip()
    {
        tooltipElement = doc.rootVisualElement.Q<VisualElement>("tooltip");
    }

    private void InitialiseInitialisationMenu()
    {
        initialisationMenuContainer = doc.rootVisualElement.Q<VisualElement>("initialisation-menu-container");

        VisualElement trialTypeContainer = initialisationMenuContainer.Q<VisualElement>("trial-type");
        EnumField trialType = trialTypeContainer.Q<EnumField>();
        trialType.value = TrialType.GroundDistance;
        new Tooltip(
            tooltipElement,
            trialTypeContainer.Q<Button>("tooltip-button"),
            "The method used to assess creature fitnesses."
        );

        VisualElement maxIterationsContainer = initialisationMenuContainer.Q<VisualElement>("max-iterations");
        SliderInt maxIterations = maxIterationsContainer.Q<SliderInt>();
        new Tooltip(
            tooltipElement,
            maxIterationsContainer.Q<Button>("tooltip-button"),
            "The maximum number of iterations to run for. Set to 0 to run forever."
        );

        VisualElement populationSizeContainer = initialisationMenuContainer.Q<VisualElement>("population-size");
        SliderInt populationSize = populationSizeContainer.Q<SliderInt>();
        new Tooltip(
            tooltipElement,
            populationSizeContainer.Q<Button>("tooltip-button"),
            "The number of creatures competing together each iteration."
        );

        VisualElement survivalPercentageContainer = initialisationMenuContainer.Q<VisualElement>("survival-percentage");
        SliderInt survivalPercentage = survivalPercentageContainer.Q<SliderInt>();
        new Tooltip(
            tooltipElement,
            survivalPercentageContainer.Q<Button>("tooltip-button"),
            "The top percentage of creatures that are selected to survive each iteration."
        );

        VisualElement mutationRateContainer = initialisationMenuContainer.Q<VisualElement>("mutation-rate");
        SliderInt mutationRate = mutationRateContainer.Q<SliderInt>();
        mutationRate.value = Mathf.FloorToInt(ParameterManager.Instance.Mutation.MutationRate);
        new Tooltip(
            tooltipElement,
            mutationRateContainer.Q<Button>("tooltip-button"),
            "The average number of mutations per child."
        );

        VisualElement seedGenotypeContainer = initialisationMenuContainer.Q<VisualElement>("seed-genotype");
        Button seedGenotypeButton = seedGenotypeContainer.Q<Button>("seed-genotype");
        Button removeSeedButton = seedGenotypeContainer.Q<Button>("remove-seed");
        new Tooltip(
            tooltipElement,
            seedGenotypeContainer.Q<Button>("tooltip-button"),
            "The genotype used to generate the initial population."
        );

        VisualElement lockMorphologiesContainer = initialisationMenuContainer.Q<VisualElement>("lock-morphologies");
        Toggle lockMorphologies = lockMorphologiesContainer.Q<Toggle>();
        lockMorphologies.value = ParameterManager.Instance.Reproduction.LockMorphologies;
        new Tooltip(
            tooltipElement,
            lockMorphologiesContainer.Q<Button>("tooltip-button"),
            "Check this to prevent creature morphologies from changing during evolution."
        );

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
                lockMorphologiesContainer.style.display = DisplayStyle.Flex;
            }
        };
        removeSeedButton.clicked += () =>
        {
            seedGenotypeButton.text = "<none>";
            seedGenotypeButton.style.backgroundColor = StyleKeyword.Null;
            removeSeedButton.style.display = DisplayStyle.None;
            seedGenotype = null;
        };
        removeSeedButton.style.display = DisplayStyle.None;
        lockMorphologiesContainer.style.display = DisplayStyle.None;

        initialisationMenuContainer.Q<Button>("run").clicked += () =>
        {
            if (!simulator.Running)
            {
                ShowInitialisationMenu(false);
                ShowRuntimeMenu(true);
                ToggleRuntimeMenuTab(RuntimeMenuTab.Status);
                numberOfIterations = maxIterations.value;
                ParameterManager.Instance.Reproduction.LockMorphologies = lockMorphologies.value;
                simulator.StartSimulation(
                    (TrialType)trialType.value,
                    maxIterations.value,
                    populationSize.value,
                    survivalPercentage.value / 100f,
                    mutationRate.value,
                    seedGenotype
                );
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
        controlsTabToggle.clicked += () => ToggleRuntimeMenuTab(RuntimeMenuTab.Controls);
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

        void recordBestGenotype()
        {
            if (simulator.bestIndividual != null)
                GenotypeMemory.RecordRecentGenotype(
                    simulator.bestIndividual.genotype,
                    "Iteration " + simulator.currentIteration + "/" + simulator.MaxIterations + ".",
                    focusGrid.GetBestIndividualCamera().CaptureImage()
                );
        }

        reset.clicked += () => { recordBestGenotype(); SceneManager.LoadScene(SceneManager.GetActiveScene().name); };
        exit.clicked += () => { recordBestGenotype(); SceneManager.LoadScene("MainMenu"); };
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
        Label seedGenotype = parametersTab.Q<Label>("seed-genotype");
        Label lockMorphologies = parametersTab.Q<Label>("lock-morphologies");

        simulator.OnSimulationStart += () =>
        {
            trialType.text = Utilities.PascalToSentenceCase(simulator.Trial.ToString());
            maxIterations.text = simulator.MaxIterations == -1 ? "∞" : simulator.MaxIterations.ToString();
            populationSize.text = simulator.PopulationSize.ToString();
            survivalPercentage.text = Mathf.FloorToInt(simulator.SurvivalPercentage * 100f).ToString() + "%";
            mutationRate.text = ParameterManager.Instance.Mutation.MutationRate == 0 ? "Mutation disabled" : "~" + ParameterManager.Instance.Mutation.MutationRate.ToString() + " mutation" + (ParameterManager.Instance.Mutation.MutationRate == 1f ? "" : "s") + "/child";
            seedGenotype.text = simulator.SeedGenotype != null ? simulator.SeedGenotype.Id : "<none>";
            lockMorphologies.parent.style.display = simulator.SeedGenotype != null ? DisplayStyle.Flex : DisplayStyle.None;
            lockMorphologies.text = ParameterManager.Instance.Reproduction.LockMorphologies ? "Yes" : "No";
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

        VisualElement speedControlContainer = controlsTab.Q<VisualElement>("speed-control");
        Button pauseButton = speedControlContainer.Q<Button>("pause-simulation");
        Button playButton = speedControlContainer.Q<Button>("play-simulation");
        Button fastForwardButton = speedControlContainer.Q<Button>("fastforward-simulation");
        pauseButton.style.backgroundColor = StyleKeyword.Null;
        playButton.style.backgroundColor = buttonActiveColor;
        fastForwardButton.style.backgroundColor = StyleKeyword.Null;
        pauseButton.clicked += () =>
        {
            WorldManager.Instance.timeScale = 0f;
            pauseButton.style.backgroundColor = buttonActiveColor;
            playButton.style.backgroundColor = StyleKeyword.Null;
            fastForwardButton.style.backgroundColor = StyleKeyword.Null;
        };
        playButton.clicked += () =>
        {
            WorldManager.Instance.timeScale = 1f;
            pauseButton.style.backgroundColor = StyleKeyword.Null;
            playButton.style.backgroundColor = buttonActiveColor;
            fastForwardButton.style.backgroundColor = StyleKeyword.Null;
        };
        fastForwardButton.clicked += () =>
        {
            WorldManager.Instance.timeScale = 5f;
            pauseButton.style.backgroundColor = StyleKeyword.Null;
            playButton.style.backgroundColor = StyleKeyword.Null;
            fastForwardButton.style.backgroundColor = buttonActiveColor;
        };
        throttledTime = speedControlContainer.Q<ProgressBar>("throttled-time");
        new Tooltip(
            tooltipElement,
            speedControlContainer.Q<Button>("tooltip-button"),
            "Changes the simulation speed. The maximum speed is dependent on system performance."
        );

        VisualElement pauseIteratingContainer = controlsTab.Q<VisualElement>("pause-iterating");
        pauseIteratingContainer.Q<Toggle>().RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.pauseIterating = e.newValue);
        new Tooltip(
            tooltipElement,
            pauseIteratingContainer.Q<Button>("tooltip-button"),
            "Click this to prevent the simulator from proceeding to the next iteration until you are ready."
        );
    }

    private void InitialiseSettingsTab()
    {
        settingsTab = runtimeMenuContainer.Q<VisualElement>("settings");

        VisualElement colourByFitnessContainer = settingsTab.Q<VisualElement>("colour-by-fitness");
        Toggle colourByFitness = colourByFitnessContainer.Q<Toggle>();
        colourByFitness.value = simulator.colourByRelativeFitness;
        colourByFitness.RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.colourByRelativeFitness = e.newValue);
        new Tooltip(
            tooltipElement,
            colourByFitnessContainer.Q<Button>("tooltip-button"),
            "Visualises relative creature fitness by colour. Red = poor fitness; Green = good fitness; Blue = best fitness."
        );

        VisualElement focusBestCreaturesContainer = settingsTab.Q<VisualElement>("focus-best");
        SliderInt focusBestCreatures = focusBestCreaturesContainer.Q<SliderInt>();
        focusBestCreatures.RegisterValueChangedCallback((ChangeEvent<int> e) => focusGrid.numVisibleFrames = e.newValue);
        new Tooltip(
            tooltipElement,
            focusBestCreaturesContainer.Q<Button>("tooltip-button"),
            "Shows the best creatures from the previous iteration in their own separate boxes."
        );
        void updateFocusGrid() => focusGrid.SetFrameTargets(simulator.GetTopIndividuals(FocusGrid.maxFrames));
        simulator.OnIterationStart += updateFocusGrid;
        simulator.OnSimulationEnd += updateFocusGrid;

        VisualElement orbitCameraContainer = settingsTab.Q<VisualElement>("orbit-camera");
        Toggle orbitCamera = orbitCameraContainer.Q<Toggle>();
        orbitCamera.value = playerController.orbitTarget != null;
        orbitCamera.RegisterValueChangedCallback((ChangeEvent<bool> e) => playerController.orbitTarget = e.newValue ? simulator.GetSimulationOrigin() : null);
        new Tooltip(
            tooltipElement,
            orbitCameraContainer.Q<Button>("tooltip-button"),
            "Orbits the camera around the simulator origin."
        );
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

        SelectionManager.Instance.OnSelectionChange += (previouslySelected, selected) =>
        {
            selectedPhenotypeMenu.SetTarget(selected.Count > 0 ? selected[0].gameObject.GetComponent<Phenotype>() : null);
        };

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
        if (tab == RuntimeMenuTab.Controls) EnableRuntimeTab(controlsTab, controlsTabToggle); else DisableRuntimeTab(controlsTab, controlsTabToggle);
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
        iterationProgress.value = numberOfIterations == 0 ? 1f : (float)simulator.currentIteration / (float)numberOfIterations;
        iterationProgress.title = simulationComplete
        ? "Simulation Complete (" + numberOfIterations + " iteration" + (numberOfIterations != 1 ? "s" : "") + ")"
        : "Iteration " + simulator.currentIteration + "/" + (numberOfIterations == 0 ? "∞" : numberOfIterations);

        bestFitness.text = simulator.bestFitnesses.Count == 0 ? "?" : simulator.bestFitnesses[simulator.bestFitnesses.Count - 1].ToString("0.000");
        averageFitness.text = simulator.averageFitnesses.Count == 0 ? "?" : simulator.averageFitnesses[simulator.averageFitnesses.Count - 1].ToString("0.000");

        bestFitnessGraph.SetPoints(simulator.bestFitnesses);
        averageFitnessGraph.SetPoints(simulator.averageFitnesses);
    }
}
