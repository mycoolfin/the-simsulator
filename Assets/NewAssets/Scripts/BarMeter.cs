using UnityEngine;

public class BarMeter : MonoBehaviour
{
    [SerializeField] private GameObject meter;
    [SerializeField] private Color defaultColor = Color.white;
    public Color DefaultColor => defaultColor;

    public float CurrentProgress { get; private set; } = 0f;

    private EmitterController emitterController;

    private void Start()
    {
        emitterController = meter.GetComponent<EmitterController>();
        emitterController.SetEmissiveColor(defaultColor);
    }

    public void SetProgress(float progress)
    {
        CurrentProgress = Mathf.Clamp01(progress);
        meter.transform.localScale = new Vector3(CurrentProgress, 1f, 1f);
        float xOffset = (CurrentProgress / 2f) - 0.5f;
        meter.transform.localPosition = new Vector3(xOffset, 0f, 0f);

        emitterController.SetEmissiveIntensity(CurrentProgress == 0f ? 0f : 1f);
    }

    public void SetColor(Color color)
    {
        emitterController.SetEmissiveColor(color);
    }
}
