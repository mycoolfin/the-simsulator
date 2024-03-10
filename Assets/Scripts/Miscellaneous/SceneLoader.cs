using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private void Awake()
    {
        if (!Application.isBatchMode)
        {
            gameObject.SetActive(false);
            return;
        }

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-scene")
            {
                SceneManager.LoadScene(args[i + 1]);
                break;
            }
        }
    }
}
