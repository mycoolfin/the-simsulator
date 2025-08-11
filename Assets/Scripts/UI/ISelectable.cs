using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI
{
    public interface ISelectable
    {
        void Select();
        Vector3 WorldPosition { get; }
        Quaternion WorldRotation { get; }
        Bounds Bounds { get; }
    }
}
