using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WorldVisualiser : MonoBehaviour
{
    [SerializeField] private NewAssets.EvolutionSimulator evolutionSimulator;
    [SerializeField] private Transform hologramTransform;
    [SerializeField] private GameObject ground;
    [SerializeField] private GameObject water;
    [SerializeField] private EmitterController innerEmitter;
    [SerializeField] private EmitterController outerEmitter;
    [SerializeField] private float dynamicScaleFactor = 1f;
    public float DynamicScaleFactor => dynamicScaleFactor;
    private float previousDynamicScaleFactor;
    private bool dynamicScaleFactorHasChanged = false;
    public bool HasChanged
    {
        get => dynamicScaleFactorHasChanged || transform.hasChanged || hologramTransform.hasChanged;
        set
        {
            dynamicScaleFactorHasChanged = value;
            transform.hasChanged = value;
            hologramTransform.hasChanged = value;
        }
    }
    private World ecsWorld;

    public void Start()
    {
        previousDynamicScaleFactor = dynamicScaleFactor;

        evolutionSimulator.OnEcsWorldCreated += (ecsWorld) =>
        {
            this.ecsWorld = ecsWorld;
            InitialiseVisualiser(evolutionSimulator.TrialType);
            SystemSettingsAPI.SetWorldVisualOffset(ecsWorld, GetTransformMatrix());
        };
    }

    public void Update()
    {
        if (previousDynamicScaleFactor != dynamicScaleFactor)
        {
            dynamicScaleFactorHasChanged = true;
            previousDynamicScaleFactor = dynamicScaleFactor;
        }

        if (evolutionSimulator.IsRunning)
        {
            AnimateEmitters(evolutionSimulator.SimulationRate);

            if (HasChanged)
            {
                SystemSettingsAPI.SetWorldVisualOffset(ecsWorld, GetTransformMatrix());
                HasChanged = false;
            }
        }
    }

    private void InitialiseVisualiser(NewAssets.TrialType trialType)
    {
        switch (trialType)
        {
            case NewAssets.TrialType.GroundDistance:
                SetGroundEnabled(true);
                SetWaterEnabled(false);
                break;
            case NewAssets.TrialType.WaterDistance:
                SetGroundEnabled(false);
                SetWaterEnabled(true);
                break;
        }
    }

    private void AnimateEmitters(SimulationRateMode simulationRate)
    {
        float frequency = simulationRate switch
        {
            SimulationRateMode.Paused => 0f,
            SimulationRateMode.RealTime => 5f,
            SimulationRateMode.FullSpeed => 20f,
            SimulationRateMode.MaximumOverdrive => 40f,
            _ => 1f,
        };
        float fluxFactor = 0.8f + 0.2f * Mathf.Sin(frequency * Time.time);
        float maxIntensity = 10f;
        SetEmitterIntensities(maxIntensity * fluxFactor);
    }

    private float4x4 GetTransformMatrix()
    {
        return float4x4.TRS(
            hologramTransform.position,
            hologramTransform.rotation,
            Vector3.Scale(hologramTransform.lossyScale, 0.001f * DynamicScaleFactor * Vector3.one)
        );
    }

    private void SetGroundEnabled(bool enabled)
    {
        if (ground != null)
            ground.SetActive(enabled);
    }

    private void SetWaterEnabled(bool enabled)
    {
        if (water != null)
            water.SetActive(enabled);
    }

    private void SetEmitterIntensities(float intensity)
    {
        if (innerEmitter != null)
            innerEmitter.SetEmissiveIntensity(intensity);
        if (outerEmitter != null)
            outerEmitter.SetEmissiveIntensity(intensity);
    }
}
