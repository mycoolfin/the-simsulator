using System;
using UnityEngine;

public class GraphConnection
{
    public GraphNode childNode;
    private int _parentFace;
    public int parentFace
    {
        get
        {
            return _parentFace;
        }
        set
        {
            if (value >= 0 && value <= 5)
                _parentFace = value;
            else
                throw new ArgumentOutOfRangeException();
        }
    }
    private Vector2 _position;
    public Vector2 position
    {
        get
        {
            return _position;
        }
        set
        {
            if (value.x >= -1 && value.x <= 1
             && value.y >= -1 && value.y <= 1)
                _position = value;
            else
                throw new ArgumentOutOfRangeException();
        }
    }
    public Vector3 orientation;
    public Vector3 scale;
    public bool reflection;
    public bool terminalOnly;
}
