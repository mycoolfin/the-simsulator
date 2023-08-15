using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    public UIDocument controlsInfo;

    private void Awake()
    {
        UIDocument doc = GetComponent<UIDocument>();
        doc.rootVisualElement.Q<Button>("evolution-mode").clicked += () => SceneManager.LoadScene("EvolutionMode");
        doc.rootVisualElement.Q<Button>("zoo-mode").clicked += () => SceneManager.LoadScene("ZooMode");
        doc.rootVisualElement.Q<Button>("ancestry-explorer").clicked += () => SceneManager.LoadScene("AncestryExplorer");
        doc.rootVisualElement.Q<Button>("controls").clicked += () =>
        {
            controlsInfo.rootVisualElement.style.display = DisplayStyle.Flex;
            doc.rootVisualElement.style.display = DisplayStyle.None;
        };
        doc.rootVisualElement.Q<Button>("quit").clicked += () => Application.Quit();
    }
}
