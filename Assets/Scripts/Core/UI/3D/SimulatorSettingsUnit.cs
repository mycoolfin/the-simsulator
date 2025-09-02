using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Evolution;
    using IO;

    public class SimulatorSettingsUnit : SaveableBehaviour
    {
        [SerializeField] private PushButton advancedSettingsToggle;
        [SerializeField] private HingedPanel advancedSettingsBlock;
        [SerializeField] private PushButton resetToDefaultsButton;
        [SerializeField] private ButtonGroup trialTypeGroup;
        [SerializeField] private ButtonGroup populationSizeGroup;
        [SerializeField] private ButtonGroup maxGenerationsGroup;
        [SerializeField] private ButtonGroup survivalRateGroup;
        [SerializeField] private ButtonGroup mutationRateGroup;
        [SerializeField] private ButtonGroup settleSecondsGroup;
        [SerializeField] private ButtonGroup assessmentSecondsGroup;

        public delegate bool IsSimulatorRunningDelegate();
        public IsSimulatorRunningDelegate IsSimulatorRunning;

        public override string SaveId => "SimulatorSettingsUnit";
        public override int SaveVersion => 1;
        [Serializable]
        private class SimulatorSettingsData
        {
            public bool AdvancedSettingsOpen;
            public int TrialTypeIndex;
            public int PopulationSizeIndex;
            public int MaxGenerationsIndex;
            public int SurvivalRateIndex;
            public int MutationRateIndex;
            public int SettleSecondsIndex;
            public int AssessmentSecondsIndex;
        }
        public override object CaptureState()
        {
            return new SimulatorSettingsData
            {
                AdvancedSettingsOpen = advancedSettingsBlock.IsOpen,
                TrialTypeIndex = trialTypeGroup.ActiveButtonIndex,
                PopulationSizeIndex = populationSizeGroup.ActiveButtonIndex,
                MaxGenerationsIndex = maxGenerationsGroup.ActiveButtonIndex,
                SurvivalRateIndex = survivalRateGroup.ActiveButtonIndex,
                MutationRateIndex = mutationRateGroup.ActiveButtonIndex,
                SettleSecondsIndex = settleSecondsGroup.ActiveButtonIndex,
                AssessmentSecondsIndex = assessmentSecondsGroup.ActiveButtonIndex
            };
        }
        public override void RestoreState(JObject payload, int version)
        {
            SimulatorSettingsData data = payload.ToObject<SimulatorSettingsData>();
            if (data == null)
            {
                ResetToDefaults();
                return;
            }

            advancedSettingsBlock.SetOpen(data.AdvancedSettingsOpen);
            trialTypeGroup.SetActiveButton(data.TrialTypeIndex);
            populationSizeGroup.SetActiveButton(data.PopulationSizeIndex);
            maxGenerationsGroup.SetActiveButton(data.MaxGenerationsIndex);
            survivalRateGroup.SetActiveButton(data.SurvivalRateIndex);
            mutationRateGroup.SetActiveButton(data.MutationRateIndex);
            settleSecondsGroup.SetActiveButton(data.SettleSecondsIndex);
            assessmentSecondsGroup.SetActiveButton(data.AssessmentSecondsIndex);
        }

        private void Awake()
        {
            ResetToDefaults();
        }

        private void Start()
        {
            InitialiseButtons();
        }

        private void Update()
        {
            UpdateButtons();
        }

        public EvolutionParameters GetEvolutionParameters()
        {
            return new EvolutionParameters
            {
                TrialType = GetTrialType(),
                PopulationSize = GetPopulationSize(),
                MaxGenerations = GetMaxGenerations(),
                SurvivalRate = GetSurvivalRate(),
                MutationRate = GetMutationRate(),
                SettleSeconds = GetSettleSeconds(),
                AssessmentSeconds = GetAssessmentSeconds()
            };
        }

        private void InitialiseButtons()
        {
            advancedSettingsToggle.OnButtonPressed += (isActive) =>
            {
                advancedSettingsBlock.SetOpen(!isActive);
                NotifyChanged();
            };

            resetToDefaultsButton.OnButtonPressed += (isActive) =>
            {
                ResetToDefaults();
                NotifyChanged();
            };

            trialTypeGroup.OnButtonPressed += (index) => NotifyChanged();
            populationSizeGroup.OnButtonPressed += (index) => NotifyChanged();
            maxGenerationsGroup.OnButtonPressed += (index) => NotifyChanged();
            survivalRateGroup.OnButtonPressed += (index) => NotifyChanged();
            mutationRateGroup.OnButtonPressed += (index) => NotifyChanged();
            settleSecondsGroup.OnButtonPressed += (index) => NotifyChanged();
            assessmentSecondsGroup.OnButtonPressed += (index) => NotifyChanged();
        }

        private void UpdateButtons()
        {
            advancedSettingsToggle.SetActive(advancedSettingsBlock.IsOpen);
            resetToDefaultsButton.SetActive(true);

            SetButtonsDisabled(IsSimulatorRunning == null || IsSimulatorRunning());
        }

        private void SetButtonsDisabled(bool disabled)
        {
            advancedSettingsToggle.SetDisabled(disabled);
            resetToDefaultsButton.SetDisabled(disabled);
            trialTypeGroup.SetGroupDisabled(disabled);
            populationSizeGroup.SetGroupDisabled(disabled);
            maxGenerationsGroup.SetGroupDisabled(disabled);
            survivalRateGroup.SetGroupDisabled(disabled);
            mutationRateGroup.SetGroupDisabled(disabled);
            settleSecondsGroup.SetGroupDisabled(disabled);
            assessmentSecondsGroup.SetGroupDisabled(disabled);
        }

        private void ResetToDefaults()
        {
            SetTrialTypeToDefaultValue();
            SetPopulationSizeToDefaultValue();
            SetMaxGenerationsToDefaultValue();
            SetSurvivalRateToDefaultValue();
            SetMutationRateToDefaultValue();
            SetSettleSecondsToDefaultValue();
            SetAssessmentSecondsToDefaultValue();
        }

        private readonly TrialType[] TrialTypeOptions = { TrialType.GroundDistance, TrialType.WaterDistance };
        private TrialType GetTrialType() => TrialTypeOptions[trialTypeGroup.ActiveButtonIndex];
        private void SetTrialTypeToDefaultValue() => trialTypeGroup.SetActiveButton(1);

        private readonly int[] PopulationSizeOptions = { 50, 100, 200, 500, 1000, 5000 };
        private int GetPopulationSize() => PopulationSizeOptions[populationSizeGroup.ActiveButtonIndex];
        private void SetPopulationSizeToDefaultValue() => populationSizeGroup.SetActiveButton(1);

        private readonly int[] MaxGenerationsOptions = { 50, 100, 200, 500, 1000, 0 };
        private int GetMaxGenerations() => MaxGenerationsOptions[maxGenerationsGroup.ActiveButtonIndex];
        private void SetMaxGenerationsToDefaultValue() => maxGenerationsGroup.SetActiveButton(1);

        private readonly float[] SurvivalRateOptions = { 0.05f, 0.010f, 0.015f, 0.020f, 0.030f, 0.050f };
        private float GetSurvivalRate() => SurvivalRateOptions[survivalRateGroup.ActiveButtonIndex];
        private void SetSurvivalRateToDefaultValue() => survivalRateGroup.SetActiveButton(3);

        private readonly float[] MutationRateOptions = { 0f, 1f, 2f, 5f, 10f, 50f };
        private float GetMutationRate() => MutationRateOptions[mutationRateGroup.ActiveButtonIndex];
        private void SetMutationRateToDefaultValue() => mutationRateGroup.SetActiveButton(1);

        private readonly float[] SettleSecondsOptions = { 1f, 2f, 5f, 10f, 20f, 30f };
        private float GetSettleSeconds() => SettleSecondsOptions[settleSecondsGroup.ActiveButtonIndex];
        private void SetSettleSecondsToDefaultValue() => settleSecondsGroup.SetActiveButton(3);

        private readonly float[] AssessmentSecondsOptions = { 1f, 2f, 5f, 10f, 20f, 30f };
        private float GetAssessmentSeconds() => AssessmentSecondsOptions[assessmentSecondsGroup.ActiveButtonIndex];
        private void SetAssessmentSecondsToDefaultValue() => assessmentSecondsGroup.SetActiveButton(3);
    }
}
