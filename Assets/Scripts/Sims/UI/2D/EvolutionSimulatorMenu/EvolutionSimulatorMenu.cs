using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Entities;
using SFB;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.TwoD
{
    using Core.Utilities;
    using Core.Evolution;
    using Core.UI.Player;
    using Core.UI.TwoD;

    public class EvolutionSimulatorMenu : MonoBehaviour
    {
        public GameObject simulatorContainer;
        public PlayerController playerController;
        public SelectedAssessableCreatureMenu selectedCreatureMenu;
        public FocusGrid focusGrid;

        private IEvolutionSimulator simulator;

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
        private string seedGenotypePath;

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
        private ProgressBar generationProgress;
        private ProgressBar assessmentProgress;
        private int numberOfGenerations;
        private Label bestFitness;
        private Label averageFitness;
        private LineGraph bestFitnessGraph;
        private LineGraph averageFitnessGraph;

        private Color buttonActiveColor = Color.white;
        private Color buttonErrorColor = Color.red;

        private List<IAssessableCreature> previousBestIndividuals = new();

        private World ecsWorld;

        private void Start()
        {
            simulator = simulatorContainer.GetComponent<IEvolutionSimulator>();
            if (simulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on simulatorContainer.");
                return;
            }

            doc = GetComponent<UIDocument>();

            InitialiseTooltip();
            InitialiseInitialisationMenu();
            InitialiseRuntimeMenu();
            InitialiseExitMenu();
            InitialiseSelectedIndividualMenu();

            ShowInitialisationMenu(true);
            ShowRuntimeMenu(false);
            ShowExitMenu(false);

            simulator.OnEcsWorldCreated += (ecsWorld) =>
            {
                this.ecsWorld = ecsWorld;
                simulator.GetPresentationSettingsAPI().SetColorByFitness(ecsWorld, false);
                simulator.GetPresentationSettingsAPI().SetFilterBySurvivors(ecsWorld, false, simulator.MaxSurvivors);
            };
        }

        private void Update()
        {
            if (simulator.IsRunning)
            {
                var simulationRateInfo = simulator.GetSimulationSettingsAPI().GetSimulationRateInfo(ecsWorld);
                throttledTime.value = simulationRateInfo.ActualTPS / (1 / simulationRateInfo.FrameBudget);

                if (simulator.AssessmentProgress > 0f) // In Assessment phase.
                {
                    assessmentProgress.title = simulator.AssessmentProgress < 1f ? "Assessing fitness..." : "Fitness assessment complete.";
                    assessmentProgress.value = simulator.AssessmentProgress;
                }
                else if (simulator.SettleProgress > 0f) // In Settle phase.
                {
                    assessmentProgress.title = simulator.SettleProgress < 1f ? "Settling creatures..." : "Settling complete.";
                    assessmentProgress.value = simulator.SettleProgress;
                }
                else // In Initialisation phase.
                {
                    assessmentProgress.title = "Initialising simulation...";
                    assessmentProgress.value = 0f;
                }
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
            SliderInt maxGenerations = maxIterationsContainer.Q<SliderInt>();
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
            new Tooltip(
                tooltipElement,
                lockMorphologiesContainer.Q<Button>("tooltip-button"),
                "Check this to prevent creature morphologies from changing during evolution."
            );

            void RemoveSeedGenotype()
            {
                seedGenotypeButton.text = "<none>";
                seedGenotypeButton.style.backgroundColor = StyleKeyword.Null;
                removeSeedButton.style.display = DisplayStyle.None;
                lockMorphologiesContainer.style.display = DisplayStyle.None;
                seedGenotypePath = null;
            }

            seedGenotypeButton.clicked += () =>
            {
                string seedGenotypePath = StandaloneFileBrowser.OpenFilePanel(
                    "Load Genotype",
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "genotype",
                    false
                ).FirstOrDefault();

                if (string.IsNullOrEmpty(seedGenotypePath) || !File.Exists(seedGenotypePath))
                {
                    RemoveSeedGenotype();
                }
                else
                {
                    this.seedGenotypePath = seedGenotypePath;
                    seedGenotypeButton.text = Path.GetFileNameWithoutExtension(seedGenotypePath);
                    seedGenotypeButton.style.backgroundColor = buttonActiveColor;
                    removeSeedButton.style.display = DisplayStyle.Flex;
                    lockMorphologiesContainer.style.display = DisplayStyle.Flex;
                }
            };
            removeSeedButton.clicked += RemoveSeedGenotype;

            RemoveSeedGenotype(); // Initialise with no seed genotype.

            initialisationMenuContainer.Q<Button>("run").clicked += () =>
            {
                if (!simulator.IsRunning)
                {
                    ShowInitialisationMenu(false);
                    ShowRuntimeMenu(true);
                    ToggleRuntimeMenuTab(RuntimeMenuTab.Status);
                    numberOfGenerations = maxGenerations.value;
                    EvolutionParameters parameters = new()
                    {
                        PopulationSize = populationSize.value,
                        MaxGenerations = maxGenerations.value,
                        SurvivalRate = survivalPercentage.value / 100f,
                        MutationRate = mutationRate.value,
                        TrialType = (TrialType)trialType.value,
                        SeedGenotypePath = seedGenotypePath,
                        LockMorphologies = lockMorphologies.value,
                    };
                    simulator.SetEvolutionParameters(parameters);
                    simulator.StartEvolution();
                }
            };

            initialisationMenuContainer.Q<Button>("exit").clicked += () => SceneManager.LoadScene("MainMenu");
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

            reset.clicked += () => { SceneManager.LoadScene(SceneManager.GetActiveScene().name); };
            exit.clicked += () => { SceneManager.LoadScene("MainMenu"); };
            cancel.clicked += () => ShowExitMenu(false);
        }

        private void InitialiseParametersTab()
        {
            parametersTab = runtimeMenuContainer.Q<VisualElement>("parameters");
            Label trialType = parametersTab.Q<Label>("trial-type");
            Label maxGenerations = parametersTab.Q<Label>("max-iterations");
            Label populationSize = parametersTab.Q<Label>("population-size");
            Label survivalRate = parametersTab.Q<Label>("survival-percentage");
            Label mutationRate = parametersTab.Q<Label>("mutation-rate");
            Label seedGenotype = parametersTab.Q<Label>("seed-genotype");
            Label lockMorphologies = parametersTab.Q<Label>("lock-morphologies");

            simulator.OnEvolutionStart += () =>
            {
                trialType.text = StringUtils.PascalToSentenceCase(simulator.TrialType.ToString());
                maxGenerations.text = simulator.MaxGenerations == -1 ? "∞" : simulator.MaxGenerations.ToString();
                populationSize.text = simulator.PopulationSize.ToString();
                survivalRate.text = Mathf.FloorToInt(simulator.SurvivalRate * 100f).ToString() + "%";
                mutationRate.text = simulator.MutationRate == 0 ? "Mutation disabled" : "~" + simulator.MutationRate.ToString() + " mutation" + (simulator.MutationRate == 1f ? "" : "s") + "/child";
                seedGenotype.text = simulator.SeedGenotypeName ?? "<none>";
                lockMorphologies.parent.style.display = simulator.SeedGenotypeName != null ? DisplayStyle.Flex : DisplayStyle.None;
                lockMorphologies.text = simulator.LockMorphologies ? "Yes" : "No";
            };
        }

        private void InitialiseStatusTab()
        {
            statusTab = runtimeMenuContainer.Q<VisualElement>("status");
            generationProgress = statusTab.Q<ProgressBar>("iteration-progress");
            assessmentProgress = statusTab.Q<ProgressBar>("assessment-progress");
            bestFitness = statusTab.Q<Label>("best-fitness");
            averageFitness = statusTab.Q<Label>("average-fitness");
            VisualElement bestFitnessGraphContainer = statusTab.Q<VisualElement>("best-fitness-graph");
            VisualElement averageFitnessGraphContainer = statusTab.Q<VisualElement>("average-fitness-graph");
            bestFitnessGraph = new LineGraph(bestFitnessGraphContainer, Color.cyan);
            averageFitnessGraph = new LineGraph(averageFitnessGraphContainer, Color.yellow);

            simulator.OnGenerationStart += (generation) => UpdateStatus(false);
            simulator.OnGenerationComplete += (generation) => generationProgress.title += " (Loading...)";
            simulator.OnEvolutionComplete += () => UpdateStatus(true);
        }

        private void InitialiseControlsTab()
        {
            controlsTab = runtimeMenuContainer.Q<VisualElement>("controls");

            VisualElement speedControlContainer = controlsTab.Q<VisualElement>("speed-control");
            Button pauseButton = speedControlContainer.Q<Button>("pause-simulation");
            Button playButton = speedControlContainer.Q<Button>("play-simulation");
            Button fastForwardButton = speedControlContainer.Q<Button>("fastforward-simulation");
            simulator.RealTimeSimulation();
            pauseButton.style.backgroundColor = StyleKeyword.Null;
            playButton.style.backgroundColor = buttonActiveColor;
            fastForwardButton.style.backgroundColor = StyleKeyword.Null;
            pauseButton.clicked += () =>
            {
                simulator.PauseSimulation();
                pauseButton.style.backgroundColor = buttonActiveColor;
                playButton.style.backgroundColor = StyleKeyword.Null;
                fastForwardButton.style.backgroundColor = StyleKeyword.Null;
            };
            playButton.clicked += () =>
            {
                simulator.RealTimeSimulation();
                pauseButton.style.backgroundColor = StyleKeyword.Null;
                playButton.style.backgroundColor = buttonActiveColor;
                fastForwardButton.style.backgroundColor = StyleKeyword.Null;
            };
            fastForwardButton.clicked += () =>
            {
                simulator.FullSpeedSimulation();
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

            VisualElement pauseLoopContainer = controlsTab.Q<VisualElement>("pause-iterating");
            pauseLoopContainer.Q<Toggle>().RegisterValueChangedCallback(e => simulator.SetEvolutionLoopPaused(e.newValue));
            new Tooltip(
                tooltipElement,
                pauseLoopContainer.Q<Button>("tooltip-button"),
                "Click this to prevent the simulator from proceeding to the next iteration until you are ready."
            );
        }

        private void InitialiseSettingsTab()
        {
            settingsTab = runtimeMenuContainer.Q<VisualElement>("settings");

            VisualElement colourByFitnessContainer = settingsTab.Q<VisualElement>("colour-by-fitness");
            Toggle colourByFitnessToggle = colourByFitnessContainer.Q<Toggle>();
            colourByFitnessToggle.value = false;
            colourByFitnessToggle.RegisterValueChangedCallback(e => SetColourByFitness(e.newValue));
            new Tooltip(
                tooltipElement,
                colourByFitnessContainer.Q<Button>("tooltip-button"),
                "Visualises relative creature fitness by colour. Red = poor fitness; Green = good fitness; Blue = best fitness."
            );

            VisualElement filterBySurvivorsContainer = settingsTab.Q<VisualElement>("filter-by-survivors");
            Toggle filterBySurvivorsToggle = filterBySurvivorsContainer.Q<Toggle>();
            filterBySurvivorsToggle.value = false;
            filterBySurvivorsToggle.RegisterValueChangedCallback(e => SetFilterBySurvivors(e.newValue));
            new Tooltip(
                tooltipElement,
                filterBySurvivorsContainer.Q<Button>("tooltip-button"),
                "Shows only the creatures that will survive to the next generation."
            );

            VisualElement focusBestCreaturesContainer = settingsTab.Q<VisualElement>("focus-best");
            SliderInt focusBestCreatures = focusBestCreaturesContainer.Q<SliderInt>();
            focusBestCreatures.RegisterValueChangedCallback(e => focusGrid.numVisibleFrames = e.newValue);
            new Tooltip(
                tooltipElement,
                focusBestCreaturesContainer.Q<Button>("tooltip-button"),
                "Shows the best creatures from the previous iteration in their own separate boxes."
            );
            simulator.OnGenerationComplete += (stats) =>
            {
                // Record the best individuals.
                previousBestIndividuals = simulator.Population?
                    .OrderByDescending(i => i.Fitness)
                    .Take(FocusGrid.maxFrames)
                    .Cast<IAssessableCreature>()
                    .ToList() ?? new();
            };
            void updateFocusGrid() => focusGrid.SetFrameTargets(previousBestIndividuals);
            simulator.OnGenerationStart += (generation) => updateFocusGrid();

            VisualElement orbitCameraContainer = settingsTab.Q<VisualElement>("orbit-camera");
            Toggle orbitCamera = orbitCameraContainer.Q<Toggle>();
            orbitCamera.value = playerController.orbitTarget != null;
            orbitCamera.RegisterValueChangedCallback(e => playerController.orbitTarget = e.newValue ? transform : null);
            new Tooltip(
                tooltipElement,
                orbitCameraContainer.Q<Button>("tooltip-button"),
                "Orbits the camera around the simulator origin."
            );
        }

        private void InitialiseSelectedIndividualMenu()
        {
            selectedCreatureMenu.EnableSaveButton(() => selectedCreatureMenu.SelectedCreature.SaveGenotypeToFile((_, _) => { }));
            selectedCreatureMenu.EnableProtectButton(() =>
            {
                IAssessableCreature individual = selectedCreatureMenu.SelectedCreature;
                individual?.Protect(!individual.IsProtected);
            });
            selectedCreatureMenu.EnableCullButton(() => selectedCreatureMenu.SelectedCreature?.Cull());

            // TODO
            // SelectionManager.Instance.OnSelectionChange += (previouslySelected, selected) =>
            // {
            //     selectedIndividualMenu.SetTarget(selected.Count > 0 ? selected[0].gameObject.GetComponent<Individual>() : null);
            // };

            simulator.OnGenerationStart += (generation) => selectedCreatureMenu.SetTarget(null);
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
            generationProgress.value = numberOfGenerations == 0 ? 1f : simulator.CurrentGeneration / (float)numberOfGenerations;
            generationProgress.title = simulationComplete
            ? "Simulation Complete (" + numberOfGenerations + " generation" + (numberOfGenerations != 1 ? "s" : "") + ")"
            : "Generation " + simulator.CurrentGeneration + "/" + (numberOfGenerations == 0 ? "∞" : numberOfGenerations);

            bestFitness.text = simulator.Statistics.Count == 0 ? "?" : simulator.Statistics[simulator.Statistics.Count - 1].BestFitness.ToString("0.000");
            averageFitness.text = simulator.Statistics.Count == 0 ? "?" : simulator.Statistics[simulator.Statistics.Count - 1].AverageFitness.ToString("0.000");

            bestFitnessGraph.SetPoints(simulator.Statistics.Select(s => s.BestFitness).ToList());
            averageFitnessGraph.SetPoints(simulator.Statistics.Select(s => s.AverageFitness).ToList());
        }

        private void SetColourByFitness(bool enabled)
        {
            simulator.GetPresentationSettingsAPI().SetColorByFitness(ecsWorld, enabled);
        }

        private void SetFilterBySurvivors(bool enabled)
        {
            simulator.GetPresentationSettingsAPI().SetFilterBySurvivors(ecsWorld, enabled, simulator.MaxSurvivors);
        }
    }
}
