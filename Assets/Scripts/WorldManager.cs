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

    public GameObject ground;
    public bool simulateFluid;
    public float fluidDensity;
    public bool gravity;
    private Vector3 defaultGravity = new Vector3(0f, -9.81f, 0f);


    public bool test = false;

    private void FixedUpdate()
    {
        Physics.gravity = gravity ? defaultGravity : Vector3.zero;
    }

    public void EnableGround(bool enabled)
    {
        ground.SetActive(enabled);
    }
}