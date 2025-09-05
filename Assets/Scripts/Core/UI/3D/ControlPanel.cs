using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using System.Linq;
    using Evolution;
    using TwoD;

    public class ControlPanel : MonoBehaviour
    {
        [SerializeField] private GameObject simulatorContainer;
        [SerializeField] private SimulatorSettingsUnit settingsUnit;
        [SerializeField] private WorldVisualiser worldVisualiser;
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float defaultZoom = 10f;
        private float desiredZoom;
        [Header("Panels")]
        [SerializeField] private HingedPanel leftPanel;
        [SerializeField] private HingedPanel rightPanel;
        [SerializeField] private HingedPanel topPanel;
        [Header("Buttons")]
        [SerializeField] private PushButton startStopButton;
        [SerializeField] private PushButton pauseButton;
        [SerializeField] private PushButton playButton;
        [SerializeField] private PushButton fastForwardButton;
        [SerializeField] private PushButton zoomInButton;
        [SerializeField] private PushButton zoomOutButton;
        [SerializeField] private PushButton colorByFitnessButton;
        [SerializeField] private PushButton filterByFitnessButton;
        [Header("Meters")]
        [SerializeField] private BarMeter generationProgressMeter;
        [SerializeField] private BarMeter genotypeCreationProgressMeter;
        [SerializeField] private BarMeter phenotypeCreationProgressMeter;
        [SerializeField] private BarMeter settlingProgressMeter;
        [SerializeField] private BarMeter assessmentProgressMeter;
        [SerializeField] private BarMeter zoomMeter;
        [Header("Graphs")]
        [SerializeField] private UIDocument fitnessGraphDocument;
        private LineGraph fitnessGraph;

        private IEvolutionSimulator simulator;

        private void Start()
        {
            simulator = simulatorContainer.GetComponent<IEvolutionSimulator>();
            if (simulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on simulatorContainer.");
                return;
            }

            settingsUnit.IsSimulatorRunning = () => simulator.IsRunning;

            InitialiseButtons();
            InitialiseGraphs();
        }

        private void Update()
        {
            UpdateZoomLevel();
            UpdatePanels();
            UpdateButtons();
            UpdateMeters();
        }

        private void UpdatePanels()
        {
            leftPanel.SetOpen(simulator.IsRunning);
            rightPanel.SetOpen(simulator.IsRunning);
            topPanel.SetOpen(simulator.IsRunning);
        }

        private void InitialiseButtons()
        {
            startStopButton.OnButtonPressed += (isActive) =>
            {
                if (!simulator.IsRunning)
                {
                    EvolutionParameters parameters = settingsUnit.GetEvolutionParameters();
                    simulator.SetEvolutionParameters(parameters);
                    simulator.StartEvolution();
                    worldVisualiser.DynamicScaleFactor = 0f;
                    desiredZoom = defaultZoom;
                }
                else simulator.StopEvolution();
            };

            pauseButton.OnButtonPressed += (isActive) =>
            {
                simulator.PauseSimulation();
            };

            playButton.OnButtonPressed += (isActive) =>
            {
                simulator.RealTimeSimulation();
            };

            fastForwardButton.OnButtonPressed += (isActive) =>
            {
                simulator.FullSpeedSimulation();
            };

            zoomInButton.OnButtonPressed += (isActive) =>
            {
                Zoom(0.1f);
            };

            zoomOutButton.OnButtonPressed += (isActive) =>
            {
                Zoom(-0.1f);
            };

            colorByFitnessButton.OnButtonPressed += (isActive) =>
            {
                worldVisualiser.ColorByFitness = !isActive;
            };

            filterByFitnessButton.OnButtonPressed += (isActive) =>
            {
                worldVisualiser.FilterBySurvivors = !isActive;
            };
        }

        private void InitialiseGraphs()
        {
            fitnessGraph = new LineGraph(fitnessGraphDocument.rootVisualElement);
            fitnessGraph.AddSeries("Best Fitness", Color.cyan);
            fitnessGraph.AddSeries("Average Fitness", Color.orange);

            simulator.OnGenerationComplete += (stats) =>
            {
                fitnessGraph.SetSeriesPoints("Best Fitness", stats.Select(s => s.BestFitness).ToList());
                fitnessGraph.SetSeriesPoints("Average Fitness", stats.Select(s => s.AverageFitness).ToList());
            };
            simulator.OnEvolutionStop += () =>
            {
                fitnessGraph.ClearAllSeriesPoints();
            };
        }

        private void UpdateButtons()
        {
            pauseButton.SetDisabled(!simulator.IsRunning);
            playButton.SetDisabled(!simulator.IsRunning);
            fastForwardButton.SetDisabled(!simulator.IsRunning);
            zoomInButton.SetDisabled(!simulator.IsRunning);
            zoomOutButton.SetDisabled(!simulator.IsRunning);
            colorByFitnessButton.SetDisabled(!simulator.IsRunning);
            filterByFitnessButton.SetDisabled(!simulator.IsRunning);

            startStopButton.SetActive(simulator.IsRunning);
            pauseButton.SetActive(simulator.IsRunning && simulator.IsSimulationPaused);
            playButton.SetActive(simulator.IsRunning && simulator.IsSimulationRealTime);
            fastForwardButton.SetActive(simulator.IsRunning && simulator.IsSimulationFullSpeed);
            zoomInButton.SetActive(simulator.IsRunning);
            zoomOutButton.SetActive(simulator.IsRunning);
            colorByFitnessButton.SetActive(simulator.IsRunning && worldVisualiser.ColorByFitness);
            filterByFitnessButton.SetActive(simulator.IsRunning && worldVisualiser.FilterBySurvivors);
        }

        private void UpdateMeters()
        {
            if (!simulator.IsRunning)
            {
                generationProgressMeter.SetProgress(0f);
                genotypeCreationProgressMeter.SetProgress(0f);
                phenotypeCreationProgressMeter.SetProgress(0f);
                settlingProgressMeter.SetProgress(0f);
                assessmentProgressMeter.SetProgress(0f);
                zoomMeter.SetProgress(0f);

                fitnessGraph.ClearAllSeriesPoints();
            }
            else
            {
                float lerpSpeed = Time.deltaTime * 10f;

                float generationProgress = simulator.MaxGenerations <= 0 ? 0f : (float)simulator.CurrentGeneration / simulator.MaxGenerations;
                generationProgressMeter.SetProgress(Mathf.Lerp(generationProgressMeter.CurrentProgress, generationProgress, lerpSpeed));

                float gLerp = Mathf.Lerp(genotypeCreationProgressMeter.CurrentProgress, simulator.GenotypeCreationProgress, lerpSpeed);
                genotypeCreationProgressMeter.SetProgress(simulator.PhenotypeCreationProgress > 0f ? 1f : gLerp);

                float pLerp = Mathf.Lerp(phenotypeCreationProgressMeter.CurrentProgress, simulator.PhenotypeCreationProgress, lerpSpeed);
                phenotypeCreationProgressMeter.SetProgress(simulator.GenotypeCreationProgress < 1f ? 0f : simulator.SettleProgress > 0f ? 1f : pLerp);

                float sLerp = Mathf.Lerp(settlingProgressMeter.CurrentProgress, simulator.SettleProgress, lerpSpeed);
                settlingProgressMeter.SetProgress(simulator.PhenotypeCreationProgress < 1f ? 0f : simulator.AssessmentProgress > 0f ? 1f : sLerp);

                float aLerp = Mathf.Lerp(assessmentProgressMeter.CurrentProgress, simulator.AssessmentProgress, lerpSpeed);
                assessmentProgressMeter.SetProgress(simulator.SettleProgress < 1f ? 0f : aLerp);

                float zoomLevel = (worldVisualiser.DynamicScaleFactor - minZoom) / (maxZoom - minZoom);
                zoomMeter.SetProgress(zoomLevel);
            }
        }

        private void UpdateZoomLevel()
        {
            worldVisualiser.DynamicScaleFactor = Mathf.Lerp(worldVisualiser.DynamicScaleFactor, desiredZoom, Time.deltaTime * 2f);
        }

        private void Zoom(float amount)
        {
            float step = (maxZoom - minZoom) * amount;
            desiredZoom = Mathf.Clamp(worldVisualiser.DynamicScaleFactor + step, minZoom, maxZoom);
        }
    }
}
