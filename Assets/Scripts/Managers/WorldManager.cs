using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WorldEnvironment
{
    Surface,
    Underwater
}

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
    public Light directionalLight;
    public GameObject world;
    public GameObject groundOrigin;
    public GameObject waterOrigin;
    public GameObject pointLight;
    public List<GameObject> trashCan; // Objects we want destroyed later, on command.
    public Queue<GameObject> theVoid; // Objects we want destroyed as soon as possible, framerate willing.

    [Header("Parameters")]
    public const float minTimeScale = 0f;
    public const float maxTimeScale = 5f;
    public float timeScale;
    public int minimumFps = 30;
    public float throttledTimeScale;
    public bool simulateFluid;
    public float fluidDensity;
    private Vector3 defaultGravity = new(0f, -9.81f, 0f);

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        trashCan = new List<GameObject>();
        throttledTimeScale = 1f;
        ChangeEnvironment(WorldEnvironment.Surface);
        StartCoroutine(TheVoidConsumesAll());
    }

    private void Update()
    {
        ThrottleTimeScaling();
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

    private IEnumerator TheVoidConsumesAll()
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

    public void ChangeEnvironment(WorldEnvironment environment)
    {
        Physics.gravity = environment == WorldEnvironment.Underwater ? Vector3.zero : defaultGravity;
        simulateFluid = environment == WorldEnvironment.Underwater;
        RenderSettings.fog = environment == WorldEnvironment.Underwater;
        directionalLight.intensity = environment == WorldEnvironment.Underwater ? 0.5f : 1f;
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
