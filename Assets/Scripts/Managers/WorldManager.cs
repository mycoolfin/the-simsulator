using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TimeOfDay
{
    Morning,
    Noon,
    Evening,
    Midnight
}

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
    public GameObject directionalLight;
    public GameObject world;
    public GameObject groundOrigin;
    public GameObject waterOrigin;
    public GameObject pointLight;
    public List<GameObject> trashCan; // Objects we want destroyed later, on command.
    public Queue<GameObject> theVoid; // Objects we want destroyed as soon as possible, framerate willing.

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

    private void Start()
    {
        trashCan = new List<GameObject>();
        throttledTimeScale = timeScale;
        StartCoroutine(TheVoidConsumesAll());
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
            throttledTimeScale = Mathf.Max(0.1f, throttledTimeScale - Time.unscaledDeltaTime);
        else
            throttledTimeScale = Mathf.Min(timeScale, throttledTimeScale + Time.unscaledDeltaTime);
        Time.timeScale = throttledTimeScale;
    }

    public void SendGameObjectToTrashCan(GameObject trash)
    {
        trashCan.Add(trash);
    }

    public void EmptyTrashCan()
    {
        foreach (GameObject trash in trashCan)
            Destroy(trash);
    }

    public void SendGameObjectToTheVoid(GameObject doomedSoul)
    {
        doomedSoul.SetActive(false);
        theVoid.Enqueue(doomedSoul);
    }

    public IEnumerator TheVoidConsumesAll()
    {
        theVoid = new();
        const int consumptionRate = 10; // Will destroy this many objects per frame.
        int soulsConsumed = 0;
        while (true)
        {
            if (theVoid.Count == 0 || soulsConsumed >= consumptionRate)
            {
                yield return null;
                soulsConsumed = 0;
                continue;
            }
            else
            {
                Destroy(theVoid.Dequeue());
                soulsConsumed += 1;
            }
        }
    }

    public void ChangeTimeOfDay(TimeOfDay timeOfDay)
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Morning:
                directionalLight.transform.rotation = Quaternion.Euler(5f, -30f, 0f);
                break;
            case TimeOfDay.Noon:
                directionalLight.transform.rotation = Quaternion.Euler(90f, -30f, 0f);
                break;
            case TimeOfDay.Evening:
                directionalLight.transform.rotation = Quaternion.Euler(-5f, -30f, 0f);
                break;
            case TimeOfDay.Midnight:
                directionalLight.transform.rotation = Quaternion.Euler(-90f, -30f, 0f);
                break;
        }
    }
}