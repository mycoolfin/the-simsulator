using System;
using UnityEngine.UIElements;

public class Tooltip
{
    private VisualElement tooltipElement;
    private Label label;
    private string text;

    public Tooltip(VisualElement tooltipElement, VisualElement triggerElement, string text)
    {
        this.tooltipElement = tooltipElement;
        label = tooltipElement.Q<Label>();
        triggerElement.RegisterCallback<MouseMoveEvent>(Show);
        triggerElement.RegisterCallback<MouseOutEvent>(Hide);
        this.text = text;
    }

    private void Show(MouseMoveEvent e)
    {
        tooltipElement.style.top = e.mousePosition.y + 10;
        tooltipElement.style.left = e.mousePosition.x;
        tooltipElement.style.display = DisplayStyle.Flex;
        label.text = text;
    }

    private void Hide(MouseOutEvent e)
    {
        tooltipElement.style.display = DisplayStyle.None;
        label.text = "";
    }
}