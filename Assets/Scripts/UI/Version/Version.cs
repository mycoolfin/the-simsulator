using UnityEngine;
using UnityEngine.UIElements;

public class Version : MonoBehaviour
{
    private void Start()
    {
        UIDocument doc = GetComponent<UIDocument>();
        doc.rootVisualElement.Q<Label>("version").text = "v" + Application.version;
    }
}
