using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI
{
    using UnityIntegration.Evolution;

    public class WorldVisualiser : MonoBehaviour
    {
        [SerializeField] private GameObject simulatorContainer;
        [SerializeField] private Transform hologramTransform;
        [SerializeField] private GameObject ground;
        [SerializeField] private GameObject water;
        [SerializeField] private EmitterController innerEmitter;
        [SerializeField] private EmitterController outerEmitter;
        [SerializeField] private Color emitterColor = Color.white;

        private IEvolutionSimulator simulator;

        public float DynamicScaleFactor = 1f;
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

        public bool ColorByFitness = false;
        private bool previousColorByFitness;

        public bool FilterBySurvivors = false;
        private bool previousFilterBySurvivors;


        private World ecsWorld;

        public void Start()
        {
            simulator = simulatorContainer.GetComponent<IEvolutionSimulator>();
            if (simulator == null)
            {
                Debug.LogError("IEvolutionSimulator component not found on simulatorContainer.");
                return;
            }

            previousDynamicScaleFactor = DynamicScaleFactor;
            previousColorByFitness = ColorByFitness;

            simulator.OnEcsWorldCreated += (ecsWorld) =>
            {
                this.ecsWorld = ecsWorld;
                InitialiseVisualiser(simulator.TrialType);
                ECS.API.PresentationSettings.SetWorldVisualOffset(ecsWorld, GetTransformMatrix());
                ECS.API.PresentationSettings.SetColorByFitness(ecsWorld, ColorByFitness);
                ECS.API.PresentationSettings.SetFilterBySurvivors(ecsWorld, FilterBySurvivors, simulator.MaxSurvivors);
            };
        }

        public void Update()
        {
            if (previousDynamicScaleFactor != DynamicScaleFactor)
            {
                dynamicScaleFactorHasChanged = true;
                previousDynamicScaleFactor = DynamicScaleFactor;
            }

            if (simulator.IsRunning)
            {
                AnimateEmitters(simulator.IsSimulationRealTime, simulator.IsSimulationFullSpeed);

                if (HasChanged)
                {
                    ECS.API.PresentationSettings.SetWorldVisualOffset(ecsWorld, GetTransformMatrix());
                    HasChanged = false;
                }

                if (previousColorByFitness != ColorByFitness)
                {
                    ECS.API.PresentationSettings.SetColorByFitness(ecsWorld, ColorByFitness);
                    previousColorByFitness = ColorByFitness;
                }

                if (previousFilterBySurvivors != FilterBySurvivors)
                {
                    ECS.API.PresentationSettings.SetFilterBySurvivors(ecsWorld, FilterBySurvivors, simulator.MaxSurvivors);
                    previousFilterBySurvivors = FilterBySurvivors;
                }
            }
            else
            {
                SetEmitterIntensities(0f);
                SetGroundEnabled(false);
                SetWaterEnabled(false);
            }
        }

        private void InitialiseVisualiser(TrialType trialType)
        {
            innerEmitter.SetEmissiveColor(emitterColor);
            outerEmitter.SetEmissiveColor(emitterColor);
            SetEmitterIntensities(0f);

            switch (trialType)
            {
                case TrialType.GroundDistance:
                    SetGroundEnabled(true);
                    SetWaterEnabled(false);
                    break;
                case TrialType.WaterDistance:
                    SetGroundEnabled(false);
                    SetWaterEnabled(true);
                    break;
            }
        }

        private void AnimateEmitters(bool isRealTime, bool isFullSpeed)
        {
            float frequency = isRealTime ? 5f : isFullSpeed ? 20f : 0f;
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
}
