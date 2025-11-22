using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Evolution;

    [RequireComponent(typeof(AudioSource))]
    public class WorldVisualiser : MonoBehaviour
    {
        [SerializeField] private GameObject simulatorContainer;
        [SerializeField] private Transform hologramTransform;
        [SerializeField] private GameObject ground;
        [SerializeField] private GameObject water;
        [SerializeField] private EmitterController innerEmitter;
        [SerializeField] private EmitterController outerEmitter;
        [SerializeField] private Color emitterColor = Color.white;

        private AudioSource audioSource;

        private IEvolutionSimulator simulator;

        private float soundBootUpPeriod = 1f;
        private float simulatorStartedAt = -1f;

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

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

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
                simulator.GetPresentationSettingsAPI().SetWorldVisualOffset(ecsWorld, GetTransformMatrix());
                simulator.GetPresentationSettingsAPI().SetColorByFitness(ecsWorld, ColorByFitness);
                simulator.GetPresentationSettingsAPI().SetFilterBySurvivors(ecsWorld, FilterBySurvivors, simulator.MaxSurvivors);
            };
        }

        private void Update()
        {
            if (previousDynamicScaleFactor != DynamicScaleFactor)
            {
                dynamicScaleFactorHasChanged = true;
                previousDynamicScaleFactor = DynamicScaleFactor;
            }

            if (simulator.IsRunning)
            {
                if (simulatorStartedAt == -1f)
                    simulatorStartedAt = Time.time;
                if (!audioSource.isPlaying)
                    audioSource.Play();
                AnimateSound();
                AnimateEmitters(simulator.IsSimulationRealTime, simulator.IsSimulationFullSpeed);

                if (HasChanged)
                {
                    simulator.GetPresentationSettingsAPI().SetWorldVisualOffset(ecsWorld, GetTransformMatrix());
                    HasChanged = false;
                }

                if (previousColorByFitness != ColorByFitness)
                {
                    simulator.GetPresentationSettingsAPI().SetColorByFitness(ecsWorld, ColorByFitness);
                    previousColorByFitness = ColorByFitness;
                }

                if (previousFilterBySurvivors != FilterBySurvivors)
                {
                    simulator.GetPresentationSettingsAPI().SetFilterBySurvivors(ecsWorld, FilterBySurvivors, simulator.MaxSurvivors);
                    previousFilterBySurvivors = FilterBySurvivors;
                }
            }
            else
            {
                simulatorStartedAt = -1f;
                if (audioSource.isPlaying) audioSource.Stop();
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

            bool isGround = trialType == TrialType.GroundDistance || trialType == TrialType.GroundLightFollowing;
            SetGroundEnabled(isGround);
            SetWaterEnabled(!isGround);
        }

        private void AnimateSound()
        {
            float timeSinceStarted = Time.time - simulatorStartedAt;
            float volume = Mathf.Lerp(0f, 1f, timeSinceStarted / soundBootUpPeriod);
            audioSource.volume = volume;
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
