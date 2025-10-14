using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.TwoD
{
    public class ScreenSpaceVersion : MonoBehaviour
    {
        private void Start()
        {
            UIDocument doc = GetComponent<UIDocument>();
            doc.rootVisualElement.Q<Label>("version").text = "v" + Application.version;
        }
    }
}
