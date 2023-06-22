using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

public enum CameraGridItemSize
{
    Small,
    Medium,
    Large
}

public class CameraGrid : MonoBehaviour
{
    public GameObject followCameraPrefab;
    public VisualTreeAsset cameraGridItem;

    public List<FollowCamera> followCameras;

    private List<VisualElement> cameraGridItems;

    private const int maxCameras = 16;

    public bool small; // TODO: remove
    public bool medium;
    public bool large;

    private void Start()
    {
        cameraGridItems = new();
        followCameras = new();

        UIDocument doc = GetComponent<UIDocument>();
        VisualElement gridFrame = doc.rootVisualElement.Q<VisualElement>("grid-frame");

        for (int i = 0; i < maxCameras; i++)
        {

            VisualElement gridItem = cameraGridItem.Instantiate();
            cameraGridItems.Add(gridItem);
            gridFrame.Add(gridItem);

            FollowCamera followCamera = Instantiate(followCameraPrefab).GetComponent<FollowCamera>();
            followCamera.name = "Follow Camera " + (i + 1);
            followCamera.transform.parent = transform;
            followCameras.Add(followCamera);
        }

        ChangeGridLayout(CameraGridItemSize.Large);
    }

    private void Update()
    {
        if (small) // TODO: remove
        {
            small = false;
            ChangeGridLayout(CameraGridItemSize.Small);
        }
        if (medium)
        {
            medium = false;
            ChangeGridLayout(CameraGridItemSize.Medium);
        }
        if (large)
        {
            large = false;
            ChangeGridLayout(CameraGridItemSize.Large);
        }
    }

    private void ChangeGridLayout(CameraGridItemSize itemSize)
    {
        int numberOfCells;
        float sizePercentage;
        switch (itemSize)
        {
            case CameraGridItemSize.Small:
                numberOfCells = 16;
                sizePercentage = 1f / 4f;
                break;
            case CameraGridItemSize.Medium:
                numberOfCells = 9;
                sizePercentage = 1f / 3f;
                break;
            case CameraGridItemSize.Large:
                numberOfCells = 4;
                sizePercentage = 1f / 2f;
                break;
            default:
                throw new System.ArgumentException("CameraGridItemSize option '" + itemSize + "' not supported.");
        }

        for (int i = 0; i < followCameras.Count; i++)
        {
            if (i < numberOfCells)
            {
                RenderTexture rt = new RenderTexture(Mathf.FloorToInt(Screen.width * sizePercentage), Mathf.FloorToInt(Screen.width * sizePercentage), 16, RenderTextureFormat.ARGB32);
                followCameras[i].GetComponent<Camera>().targetTexture = rt;
                followCameras[i].gameObject.SetActive(true);

                cameraGridItems[i].style.display = DisplayStyle.Flex;
                cameraGridItems[i].style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
                cameraGridItems[i].style.width = Length.Percent(100f * sizePercentage);
                cameraGridItems[i].style.height = Length.Percent(100f * sizePercentage);
            }
            else
            {
                followCameras[i].gameObject.SetActive(false);
                cameraGridItems[i].style.display = DisplayStyle.None;
            }
        }
    }
}
