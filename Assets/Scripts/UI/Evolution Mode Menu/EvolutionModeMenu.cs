using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;

public class EvolutionModeMenu : MonoBehaviour
{
    public EvolutionSimulator simulator;
    public PlayerController playerController;

    private UIDocument doc;
    private VisualElement menu;
    private bool menuOpen;
    private VisualElement initialMenu;
    private VisualElement runtimeMenu;
    private VisualElement initialisationParameters;
    private Genotype seedGenotype;
    private VisualElement runtimeParameters;
    private ProgressBar throttledTime;
    private VisualElement visualisationOptions;
    private VisualElement status;
    private ProgressBar progress;
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
        InitialiseMenu();
        InitialiseInitialisationParametersPanel();
        InitialiseRuntimeParametersPanel();
        InitialiseVisualisationOptionsPanel();
        InitialiseStatusPanel();
        InitialiseSelectedPhenotypeOptionsPanel();

        ShowMenu(true);
        ShowInitialPanels(true);
        ShowRuntimePanels(false);
    }

    private void Update()
    {
        if (menuOpen && simulator.running)
            throttledTime.value = WorldManager.Instance.throttledTimeScale / WorldManager.Instance.timeScale;
    }

    private void InitialiseMenu()
    {
        menu = doc.rootVisualElement.Q<VisualElement>("menu");
        doc.rootVisualElement.Q<Button>("menu-open").clicked += () => ShowMenu(true);
        doc.rootVisualElement.Q<Button>("menu-close").clicked += () => ShowMenu(false);
        initialMenu = doc.rootVisualElement.Q<VisualElement>("initial-menu");
        runtimeMenu = doc.rootVisualElement.Q<VisualElement>("runtime-menu");
        doc.rootVisualElement.Q<Button>("exit").clicked += () => SceneManager.LoadScene("MainMenu");
    }

    private void InitialiseInitialisationParametersPanel()
    {
        initialisationParameters = doc.rootVisualElement.Q<VisualElement>("initialisation-parameters");
        DropdownField trialType = doc.rootVisualElement.Q<DropdownField>("trial-type");
        trialType.choices = Enum.GetNames(typeof(TrialType)).Select(name => Utilities.PascalToSentenceCase(name)).ToList();
        trialType.index = 0;
        SliderInt maxIterations = doc.rootVisualElement.Q<SliderInt>("max-iterations");
        SliderInt populationSize = doc.rootVisualElement.Q<SliderInt>("population-size");
        SliderInt survivalPercentage = doc.rootVisualElement.Q<SliderInt>("survival-percentage");
        Button seedGenotypeButton = doc.rootVisualElement.Q<Button>("seed-genotype");
        Button removeSeedButton = doc.rootVisualElement.Q<Button>("remove-seed");
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

        doc.rootVisualElement.Q<Button>("run").clicked += () =>
        {
            if (!simulator.running)
            {
                ShowInitialPanels(false);
                ShowRuntimePanels(true);
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

        simulator.OnSimulationStart += () => playerController.transform.position += simulator.trialOrigin.position;
    }

    private void InitialiseRuntimeParametersPanel()
    {
        runtimeParameters = doc.rootVisualElement.Q<VisualElement>("runtime-parameters");
        Button pauseButton = doc.rootVisualElement.Q<Button>("pause-simulation");
        Button playButton = doc.rootVisualElement.Q<Button>("play-simulation");
        Button fastForwardButton = doc.rootVisualElement.Q<Button>("fastforward-simulation");
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
        throttledTime = doc.rootVisualElement.Q<ProgressBar>("throttled-time");
        doc.rootVisualElement.Q<Toggle>("pause-iterating").RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.pauseIterating = e.newValue);
        doc.rootVisualElement.Q<Button>("reset").clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void InitialiseVisualisationOptionsPanel()
    {
        visualisationOptions = doc.rootVisualElement.Q<VisualElement>("visualisation-options");
        Toggle colourByFitness = doc.rootVisualElement.Q<Toggle>("colour-by-fitness");
        colourByFitness.value = simulator.colourByRelativeFitness;
        colourByFitness.RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.colourByRelativeFitness = e.newValue);
        Toggle hideLowFitness = doc.rootVisualElement.Q<Toggle>("hide-low-fitness");
        hideLowFitness.value = simulator.filterByPotentialSurvivors;
        hideLowFitness.RegisterValueChangedCallback((ChangeEvent<bool> e) => simulator.filterByPotentialSurvivors = e.newValue);
        Toggle orbitCamera = doc.rootVisualElement.Q<Toggle>("orbit-camera");
        orbitCamera.value = playerController.orbitTarget != null;
        orbitCamera.RegisterValueChangedCallback((ChangeEvent<bool> e) => playerController.orbitTarget = e.newValue ? simulator.trialOrigin : null);
    }

    private void InitialiseStatusPanel()
    {
        status = doc.rootVisualElement.Q<VisualElement>("status");
        progress = doc.rootVisualElement.Q<ProgressBar>("progress");
        bestFitness = doc.rootVisualElement.Q<Label>("best-fitness");
        averageFitness = doc.rootVisualElement.Q<Label>("average-fitness");

        simulator.OnIterationStart += () => UpdateStatus(false);
        simulator.OnIterationEnd += () => progress.title += " (Loading...)";
        simulator.OnSimulationEnd += () => UpdateStatus(true);
    }

    private void UpdateStatus(bool simulationComplete)
    {
        progress.value = (float)simulator.currentIteration / (float)numberOfIterations;
        progress.title = simulationComplete
        ? "Simulation Complete (" + numberOfIterations + " iteration" + (numberOfIterations != 1 ? "s" : "") + ")"
        : "Iteration " + simulator.currentIteration + "/" + numberOfIterations;

        bestFitness.text = simulator.bestFitnesses.Count == 0 ? "?" : simulator.bestFitnesses[simulator.bestFitnesses.Count - 1].ToString("0.000");
        averageFitness.text = simulator.averageFitnesses.Count == 0 ? "?" : simulator.averageFitnesses[simulator.averageFitnesses.Count - 1].ToString("0.000");
    }

    private void InitialiseSelectedPhenotypeOptionsPanel()
    {
        selectedPhenotypeOptions = doc.rootVisualElement.Q<VisualElement>("selected-phenotype-options");
        Button saveToFileButton = doc.rootVisualElement.Q<Button>("save-to-file");
        Button protectButton = doc.rootVisualElement.Q<Button>("protect");
        Button cullButton = doc.rootVisualElement.Q<Button>("cull");
        Label phenotypeOptionsLog = doc.rootVisualElement.Q<Label>("phenotype-options-log");
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
            phenotypeOptionsLog.style.display = DisplayStyle.Flex;
            phenotypeOptionsLog.text = saveSuccess ? "Saved genotype to " + savePath : "Error saving genotype to file.";
            phenotypeOptionsLog.style.color = saveSuccess ? Color.black : Color.red;
        };
        protectButton.clicked += () =>
        {
            Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
            bool isProtected = simulator.TogglePhenotypeProtection(selectedPhenotype);
            phenotypeOptionsLog.style.display = DisplayStyle.Flex;
            phenotypeOptionsLog.text = isProtected ? "Protected phenotype." : "Removed phenotype protection.";
            phenotypeOptionsLog.style.color = Color.black;
        };
        cullButton.clicked += () =>
        {
            Phenotype selectedPhenotype = (Phenotype)SelectionManager.Instance.Selected;
            Destroy(selectedPhenotype.gameObject);
            SelectionManager.Instance.Selected = null;
        };

        SelectionManager.Instance.OnSelection += () =>
        {
            selectedPhenotypeOptions.style.display = ((Phenotype)SelectionManager.Instance.Selected) != null ? DisplayStyle.Flex : DisplayStyle.None;
            phenotypeOptionsLog.text = "";
            phenotypeOptionsLog.style.display = DisplayStyle.None;
        };

        simulator.OnIterationStart += () => selectedPhenotypeOptions.style.display = DisplayStyle.None;
        simulator.OnSimulationEnd += () => selectedPhenotypeOptions.style.display = DisplayStyle.None;
    }

    private void ShowMenu(bool show)
    {
        menuOpen = show;
        menu.style.left = show ? 0 : menu.resolvedStyle.width;
    }

    private void ShowInitialPanels(bool show)
    {
        initialMenu.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowRuntimePanels(bool show)
    {
        runtimeMenu.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
