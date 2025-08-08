using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI.Screen
{
    public class ScreenBlockingPanel : MonoBehaviour
    {
        private void Awake()
        {
            VisualElement panelElement = GetComponent<UIDocument>().rootVisualElement;
            BlockingPanel blockingPanel = gameObject.AddComponent<BlockingPanel>();
            blockingPanel.SetPanel(panelElement);
        }
    }
}
