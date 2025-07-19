using System;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(EmitterController))]
public class PushButton : MonoBehaviour
{
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private float inactiveEmissivity = 0.1f;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private float activeEmissivity = 1f;
    public bool IsActive { get; private set; } = false;
    public event Action<bool> OnButtonPressed;

    private Renderer buttonRenderer;
    private EmitterController emitterController;

    [SerializeField] private bool debugPressButton = false;

    private void Update()
    {
        if (debugPressButton)
        {
            debugPressButton = false;
            Push();
        }
    }

    private void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        emitterController = GetComponent<EmitterController>();
        SetActive(false);
    }

    public void Push()
    {
        OnButtonPressed?.Invoke(IsActive);
    }

    public void SetActive(bool active)
    {
        IsActive = active;

        buttonRenderer.material.color = active ? activeColor : inactiveColor;
        emitterController.SetEmissiveColor(active ? activeColor : inactiveColor);
        emitterController.SetEmissiveIntensity(active ? activeEmissivity : inactiveEmissivity);
    }
}
