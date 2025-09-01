using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Evolution;

    public class SimulatorSettingsUnit : MonoBehaviour
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

        private void Start()
        {
            ResetToDefaults();
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
            };

            resetToDefaultsButton.OnButtonPressed += (isActive) =>
            {
                ResetToDefaults();
            };
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

        private readonly TrialType[] TrialTypeOptions = { TrialType.GroundDistance, TrialType.WaterDistance, TrialType.GroundDistance, TrialType.WaterDistance }; // TODO: Return when light assessments are implemented.
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
