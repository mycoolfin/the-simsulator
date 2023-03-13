using UnityEngine;

public class WorldManager : MonoBehaviour
{
    private static WorldManager _instance;
    public static WorldManager Instance { get { return _instance; } }

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

    [Header("World Objects")]
    public GameObject ground;
    [Header("Parameters")]
    [Range(0f, 5f)]
    public float timeScale;
    [Range(15, 60)]
    public int minimumFps = 30;
    public float throttledTimeScale;
    public bool simulateFluid;
    public float fluidDensity;
    public bool gravity;
    private Vector3 defaultGravity = new Vector3(0f, -9.81f, 0f);
    public bool useComputeShaderForNNs = false;

    private void Start()
    {
        throttledTimeScale = timeScale;
    }

    private void Update()
    {
        ThrottleTimeScaling();
    }

    private void FixedUpdate()
    {
        Physics.gravity = gravity ? defaultGravity : Vector3.zero;
    }

    private void ThrottleTimeScaling()
    {
        if (1 / Time.deltaTime < minimumFps)
            throttledTimeScale = Mathf.Max(0.1f, throttledTimeScale - Time.deltaTime);
        else
            throttledTimeScale = Mathf.Min(timeScale, throttledTimeScale + Time.deltaTime);
        Time.timeScale = throttledTimeScale;
    }

    public void EnableGround(bool enabled)
    {
        ground.SetActive(enabled);
    }
}