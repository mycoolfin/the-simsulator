using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private static SelectionManager _instance;
    public static SelectionManager Instance { get { return _instance; } }

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
    }

    private List<ISelectable> selected = new();

    public List<ISelectable> Selected => selected;

    public void SetSelected(List<ISelectable> newSelected)
    {
        List<ISelectable> previouslySelected = selected;
        selected = newSelected ?? new();
        OnSelectionChange(previouslySelected, selected);
    }

    public void AddToSelection(ISelectable toAdd)
    {
        if (!Selected.Contains(toAdd))
            SetSelected(Selected.Concat(new List<ISelectable>() { toAdd }).ToList());
    }

    public void RemoveFromSelection(ISelectable toRemove)
    {
        if (Selected.Contains(toRemove))
            SetSelected(Selected.Where(s => s != toRemove).ToList());
    }

    public event Action<List<ISelectable>, List<ISelectable>> OnSelectionChange = delegate { };
}
