using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI.Interactable
{
    using Evolution;

    public class ControlPanel : MonoBehaviour
    {
        [SerializeField] private GameObject simulatorContainer;
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
        [SerializeField] private BarMeter zoomMeter;

        private IEvolutionSimulator simulator;

        private void Start()
        {
            simulator = simulatorContainer.GetComponent<IEvolutionSimulator>();
            if (simulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on simulatorContainer.");
                return;
            }

            InitialiseButtons();
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

        private void UpdateButtons()
        {
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
                zoomMeter.SetProgress(0f);
            }
            else
            {
                float generationProgress = simulator.MaxGenerations <= 0 ? 0f : (float)simulator.CurrentGeneration / simulator.MaxGenerations;
                generationProgressMeter.SetProgress(Mathf.Lerp(generationProgressMeter.CurrentProgress, generationProgress, Time.deltaTime * 2f));
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
