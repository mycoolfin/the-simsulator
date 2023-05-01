using System;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private static SelectionManager _instance;
    public static SelectionManager Instance { get { return _instance; } }

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

    private ISelectable selected;
    public ISelectable Selected
    {
        get => selected;
        set
        {
            selected = value;
            OnSelection();
        }
    }
    public event Action OnSelection = delegate { };
}
