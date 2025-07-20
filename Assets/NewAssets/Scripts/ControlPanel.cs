using UnityEngine;

public class ControlPanel : MonoBehaviour
{
    [SerializeField] private NewAssets.EvolutionSimulator evolutionSimulator;
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
    [SerializeField] private PushButton ffButton;
    [SerializeField] private PushButton fffButton;
    [SerializeField] private PushButton zoomInButton;
    [SerializeField] private PushButton zoomOutButton;
    [SerializeField] private PushButton colorByFitnessButton;
    [SerializeField] private PushButton filterByFitnessButton;

    [Header("Meters")]
    [SerializeField] private BarMeter generationProgressMeter;
    [SerializeField] private BarMeter zoomMeter;

    private void Start()
    {
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
        leftPanel.SetOpen(evolutionSimulator.IsRunning);
        rightPanel.SetOpen(evolutionSimulator.IsRunning);
        topPanel.SetOpen(evolutionSimulator.IsRunning);
    }

    private void InitialiseButtons()
    {
        startStopButton.OnButtonPressed += (isActive) =>
        {
            if (!evolutionSimulator.IsRunning)
            {
                evolutionSimulator.StartEvolution();
                worldVisualiser.DynamicScaleFactor = 0f;
                desiredZoom = defaultZoom;
            }
            else evolutionSimulator.StopEvolution();
        };

        pauseButton.OnButtonPressed += (isActive) =>
        {
            evolutionSimulator.SimulationRate = SimulationRateMode.Paused;
        };

        playButton.OnButtonPressed += (isActive) =>
        {
            evolutionSimulator.SimulationRate = SimulationRateMode.RealTime;
        };

        ffButton.OnButtonPressed += (isActive) =>
        {
            evolutionSimulator.SimulationRate = SimulationRateMode.FullSpeed;
        };

        fffButton.OnButtonPressed += (isActive) =>
        {
            evolutionSimulator.SimulationRate = SimulationRateMode.MaximumOverdrive;
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
        startStopButton.SetActive(evolutionSimulator.IsRunning);
        pauseButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.Paused);
        playButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.RealTime);
        ffButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.FullSpeed);
        fffButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.MaximumOverdrive);
        zoomInButton.SetActive(evolutionSimulator.IsRunning);
        zoomOutButton.SetActive(evolutionSimulator.IsRunning);
        colorByFitnessButton.SetActive(evolutionSimulator.IsRunning && worldVisualiser.ColorByFitness);
        filterByFitnessButton.SetActive(evolutionSimulator.IsRunning && worldVisualiser.FilterBySurvivors);
    }

    private void UpdateMeters()
    {
        if (!evolutionSimulator.IsRunning)
        {
            generationProgressMeter.SetProgress(0f);
            zoomMeter.SetProgress(0f);
        }
        else
        {
            float generationProgress = (float)evolutionSimulator.CurrentGeneration / evolutionSimulator.MaxGenerations;
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
