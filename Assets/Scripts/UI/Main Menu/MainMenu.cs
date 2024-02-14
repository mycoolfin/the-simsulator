using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    public UIDocument controlsInfo;
    public GameObject evolutionModeLight;
    public GameObject zooModeLight;
    public GameObject creatureEditorLight;
    public GameObject controlsLight;
    public GameObject aboutLight;
    public GameObject quitLight;

    private void Awake()
    {
        UIDocument doc = GetComponent<UIDocument>();
        Button evolutionMode = doc.rootVisualElement.Q<Button>("evolution-mode");
        Button zooMode = doc.rootVisualElement.Q<Button>("zoo-mode");
        Button creatureEditor = doc.rootVisualElement.Q<Button>("creature-editor");
        Button controls = doc.rootVisualElement.Q<Button>("controls");
        Button about = doc.rootVisualElement.Q<Button>("about");
        Button quit = doc.rootVisualElement.Q<Button>("quit");
        evolutionMode.RegisterCallback<MouseOverEvent>((e) => ToggleGlowButton(GlowButton.EvolutionMode));
        zooMode.RegisterCallback<MouseOverEvent>((e) => ToggleGlowButton(GlowButton.ZooMode));
        creatureEditor.RegisterCallback<MouseOverEvent>((e) => ToggleGlowButton(GlowButton.CreatureEditor));
        controls.RegisterCallback<MouseOverEvent>((e) => ToggleGlowButton(GlowButton.Controls));
        about.RegisterCallback<MouseOverEvent>((e) => ToggleGlowButton(GlowButton.About));
        quit.RegisterCallback<MouseOverEvent>((e) => ToggleGlowButton(GlowButton.Quit));
        evolutionMode.RegisterCallback<MouseOutEvent>((e) => ToggleGlowButton(GlowButton.None));
        zooMode.RegisterCallback<MouseOutEvent>((e) => ToggleGlowButton(GlowButton.None));
        creatureEditor.RegisterCallback<MouseOutEvent>((e) => ToggleGlowButton(GlowButton.None));
        controls.RegisterCallback<MouseOutEvent>((e) => ToggleGlowButton(GlowButton.None));
        about.RegisterCallback<MouseOutEvent>((e) => ToggleGlowButton(GlowButton.None));
        quit.RegisterCallback<MouseOutEvent>((e) => ToggleGlowButton(GlowButton.None));
        evolutionMode.clicked += () => SceneManager.LoadScene("EvolutionMode");
        zooMode.clicked += () => SceneManager.LoadScene("ZooMode");
        creatureEditor.clicked += () => SceneManager.LoadScene("CreatureEditor");
        controls.clicked += () =>
        {
            controlsInfo.rootVisualElement.style.display = DisplayStyle.Flex;
            doc.rootVisualElement.style.display = DisplayStyle.None;
        };
        about.clicked += () => { };
        quit.clicked += () => Application.Quit();
    }

    private enum GlowButton
    {
        None,
        EvolutionMode,
        ZooMode,
        CreatureEditor,
        Controls,
        About,
        Quit
    }

    private void ToggleGlowButton(GlowButton glowButton)
    {
        evolutionModeLight.SetActive(glowButton == GlowButton.EvolutionMode);
        zooModeLight.SetActive(glowButton == GlowButton.ZooMode);
        creatureEditorLight.SetActive(glowButton == GlowButton.CreatureEditor);
        controlsLight.SetActive(glowButton == GlowButton.Controls);
        aboutLight.SetActive(glowButton == GlowButton.About);
        quitLight.SetActive(glowButton == GlowButton.Quit);
    }
}
