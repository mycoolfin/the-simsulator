using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager _instance;
    public static ResourceManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public GameObject limbPrefab;
    public Material deadLimbMaterial;
}