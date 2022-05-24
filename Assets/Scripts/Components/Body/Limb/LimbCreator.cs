using UnityEngine;
using System.Collections;

public static class LimbCreator
{
    public static LimbNode CreateLimb()
    {
        return CreateBoxLimb();
    }

    public static LimbNode CreateBoxLimb()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "BoxLimb";
        box.AddComponent<Rigidbody>();
        box.AddComponent<BoxCollider>();
        LimbNode limb = box.AddComponent<LimbNode>();
        return limb;
    }
}
