using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public PlayerController player;
    public Spawner spawner;
    public List<Vector3> surfaceSpawnPositions;
    public List<Vector3> underwaterSpawnPositions;
    public MainMenuTitle title;
    public MainMenuButton evolutionModeButton;
    public MainMenuButton zooModeButton;
    public MainMenuButton creatureEditorButton;
    public MainMenuButton controlsButton;
    public MainMenuButton aboutButton;
    public MainMenuButton quitButton;
    public GameObject controlsPanel;
    public MainMenuButton controlsBackButton;
    public GameObject aboutPanel;
    public MainMenuButton aboutBackButton;

    private Vector3 defaultCameraPosition;
    private Quaternion defaultCameraRotation;
    private Vector3 desiredCameraPosition;
    private Quaternion desiredCameraRotation;

    private MainMenuButton hoveringOver;

    private void Start()
    {
        evolutionModeButton.OnSelect += () => SceneManager.LoadScene("EvolutionMode");
        zooModeButton.OnSelect += () => SceneManager.LoadScene("ZooMode");
        creatureEditorButton.OnSelect += () => SceneManager.LoadScene("CreatureEditor");
        controlsButton.OnSelect += () =>
        {
            desiredCameraPosition = new Vector3(controlsPanel.transform.position.x, controlsPanel.transform.position.y, defaultCameraPosition.z);
            desiredCameraRotation = Quaternion.identity;
        };
        aboutButton.OnSelect += () =>
        {
            desiredCameraPosition = new Vector3(aboutPanel.transform.position.x, aboutPanel.transform.position.y, defaultCameraPosition.z);
            desiredCameraRotation = Quaternion.identity;
        };
        controlsBackButton.OnSelect += () =>
        {
            desiredCameraPosition = defaultCameraPosition;
            desiredCameraRotation = defaultCameraRotation;
        };
        aboutBackButton.OnSelect += () =>
        {
            desiredCameraPosition = defaultCameraPosition;
            desiredCameraRotation = defaultCameraRotation;
        };
        quitButton.OnSelect += () => Application.Quit();

        defaultCameraPosition = player.playerCamera.transform.position;
        defaultCameraRotation = player.playerCamera.transform.rotation;
        desiredCameraPosition = defaultCameraPosition;
        desiredCameraRotation = defaultCameraRotation;
        Debug.Log(Random.value);
        if (Random.value > 0.5f)
            Surface();
        else
            Underwater();
    }

    private void Update()
    {
        player.playerCamera.transform.position = Vector3.Lerp(player.playerCamera.transform.position, desiredCameraPosition, Time.deltaTime * 10f);
        player.playerCamera.transform.rotation = Quaternion.Lerp(player.playerCamera.transform.rotation, desiredCameraRotation, Time.deltaTime * 10f);
        HoverEffect();
    }

    private void HoverEffect()
    {
        Ray ray = player.playerCamera.ScreenPointToRay(player.CursorPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.TryGetComponent(out MainMenuButton button))
            {
                if (hoveringOver != button)
                {
                    if (hoveringOver != null) hoveringOver.hovering = false;
                    button.hovering = true;
                }
                hoveringOver = button;
            }
            else
            {
                if (hoveringOver != null) hoveringOver.hovering = false;
                hoveringOver = null;
            }
        }
    }

    private void Surface()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Surface);
        title.ToggleLit(false);
        spawner.spawnType = SpawnType.Surface;
        spawner.spawnLocations = surfaceSpawnPositions;
    }

    private void Underwater()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Underwater);
        title.ToggleLit(true);
        spawner.spawnType = SpawnType.Underwater;
        spawner.spawnLocations = underwaterSpawnPositions;
    }
}
