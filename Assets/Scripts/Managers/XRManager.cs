using UnityEngine;
using UnityEngine.XR.Management;

public class XRManager : MonoBehaviour
{
    private static XRManager _instance;
    public static XRManager Instance { get { return _instance; } }

    public bool enableXR;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        if (enableXR)
        {
            StopXR(); // This is sometimes necessary for some reason.
            StartXR();
        }
        else
        {
            StopXR();
        }
    }

    public void StartXR()
    {
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    void StopXR()
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
    }
}
