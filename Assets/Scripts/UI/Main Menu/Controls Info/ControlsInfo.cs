using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ControlsInfo : MonoBehaviour
{
    public UIDocument mainMenu;

    private void Awake()
    {
        UIDocument doc = GetComponent<UIDocument>();
        Button backButton = doc.rootVisualElement.Q<Button>("back");
        doc.rootVisualElement.style.display = DisplayStyle.None;
        backButton.clicked += () =>
        {
            mainMenu.rootVisualElement.style.display = DisplayStyle.Flex;
            doc.rootVisualElement.style.display = DisplayStyle.None;
        };
    }
}
