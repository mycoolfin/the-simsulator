using UnityEngine;
using UnityEngine.UIElements;

public class ScreenBlockingPanel : MonoBehaviour
{
    private void Awake()
    {
        VisualElement panelElement = GetComponent<UIDocument>().rootVisualElement;
        BlockingPanel blockingPanel = gameObject.AddComponent<BlockingPanel>();
        blockingPanel.SetPanel(panelElement);
    }
}
