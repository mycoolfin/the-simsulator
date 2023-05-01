using System;
using UnityEngine;

public class PickAndPlaceManager : MonoBehaviour
{
    private static PickAndPlaceManager _instance;
    public static PickAndPlaceManager Instance { get { return _instance; } }

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

    public GameObject placementCursor;
    private IPlaceable held;
    public IPlaceable Held => held;
    private Action placedCallback;
    private Action cancelCallback;

    public void ShowCursor(bool show)
    {
        placementCursor.SetActive(show);
    }

    public void MoveCursorTo(Vector3 position)
    {
        placementCursor.transform.position = position;
    }

    public void PickUp(IPlaceable placeableObject, Action placedCallback, Action cancelCallback)
    {
        placeableObject.gameObject.SetActive(false);
        held = placeableObject;
        this.placedCallback = placedCallback;
        this.cancelCallback = cancelCallback;
    }

    public void Place(Vector3 position)
    {
        if (held != null)
        {
            float yOffset = -held.GetBounds().center.y + held.GetBounds().extents.y * 2f;
            held.gameObject.transform.position = position + new Vector3(0f, yOffset, 0f);
            held.gameObject.SetActive(true);
            Action cb = placedCallback;
            Reset();
            cb();
        }
    }

    public void Cancel()
    {
        if (held != null)
        {
            Action cb = cancelCallback;
            Reset();
            cb();
        }
    }

    private void Reset()
    {
        ShowCursor(false);
        placedCallback = null;
        cancelCallback = null;
        held = null;
    }
}
