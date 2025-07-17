using Unity.Mathematics;
using UnityEngine;

public class WorldContainer : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    [SerializeField] private GameObject water;
    [SerializeField] private EmitterController innerEmitter;
    [SerializeField] private EmitterController outerEmitter;
    [SerializeField] private float dynamicScaleFactor = 1f;
    public float DynamicScaleFactor => dynamicScaleFactor;
    private float previousDynamicScaleFactor;
    private bool dynamicScaleFactorHasChanged = false;
    public bool HasChanged
    {
        get => dynamicScaleFactorHasChanged || transform.hasChanged;
        set
        {
            dynamicScaleFactorHasChanged = value;
            transform.hasChanged = value;
        }
    }

    public void Start()
    {
        previousDynamicScaleFactor = dynamicScaleFactor;
    }

    public void Update()
    {
        if (previousDynamicScaleFactor != dynamicScaleFactor)
        {
            dynamicScaleFactorHasChanged = true;
            previousDynamicScaleFactor = dynamicScaleFactor;
        }
    }

    public float4x4 GetTransformMatrix()
    {
        return float4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.Scale(transform.localScale, Vector3.one * DynamicScaleFactor)
        );
    }

    public void SetGroundEnabled(bool enabled)
    {
        if (ground != null)
            ground.SetActive(enabled);
    }

    public void SetWaterEnabled(bool enabled)
    {
        if (water != null)
            water.SetActive(enabled);
    }

    public void SetEmitterIntensities(float intensity)
    {
        if (innerEmitter != null)
            innerEmitter.SetEmissiveIntensity(intensity);
        if (outerEmitter != null)
            outerEmitter.SetEmissiveIntensity(intensity);
    }
}
