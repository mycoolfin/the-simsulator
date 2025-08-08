using UnityEngine;
using UnityEngine.SceneManagement;

namespace mycoolfin.TheSimsulator.UnityIntegration
{
    public class ApplicationEntryPoint : MonoBehaviour
    {
        [SerializeField] private string uiEntryScene;
        [SerializeField] private string headlessEntryScene;

        private void Awake()
        {
            bool isHeadless = Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

            if (isHeadless)
            {
                Debug.Log("Running in headless mode. Loading headless entry scene.");
                SceneManager.LoadScene(headlessEntryScene);
            }
            else
            {
                Debug.Log("Running in normal mode. Loading UI entry scene.");
                SceneManager.LoadScene(uiEntryScene);
            }
        }
    }
}
