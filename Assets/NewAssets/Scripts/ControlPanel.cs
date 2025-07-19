using UnityEngine;

public class ControlPanel : MonoBehaviour
{
    [SerializeField] private NewAssets.EvolutionSimulator evolutionSimulator;
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

    private void Start()
    {
        InitialiseButtons();
    }

    private void Update()
    {
        UpdatePanels();
        UpdateButtons();
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
            if (!   evolutionSimulator.IsRunning) evolutionSimulator.StartEvolution();
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
    }

    private void UpdateButtons()
    {
        startStopButton.SetActive(evolutionSimulator.IsRunning);
        pauseButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.Paused);
        playButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.RealTime);
        ffButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.FullSpeed);
        fffButton.SetActive(evolutionSimulator.IsRunning && evolutionSimulator.SimulationRate == SimulationRateMode.MaximumOverdrive);
    }
}
