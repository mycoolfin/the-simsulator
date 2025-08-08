using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI.Screen
{
    public class Version : MonoBehaviour
    {
        private void Start()
        {
            UIDocument doc = GetComponent<UIDocument>();
            doc.rootVisualElement.Q<Label>("version").text = "v" + Application.version;
        }
    }
}
