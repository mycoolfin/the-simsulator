using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
public class HingedPanel : MonoBehaviour
{
    [SerializeField] private bool startOpen = false;
    [SerializeField] private float openAngle = 0f;
    [SerializeField] private float closeAngle = 90f;
    public bool IsOpen { get; private set; } = false;

    private HingeJoint hinge;

    private void Start()
    {
        hinge = GetComponent<HingeJoint>();
        SetOpen(startOpen);
    }

    public void SetOpen(bool open)
    {
        hinge.spring = new()
        {
            spring = hinge.spring.spring,
            damper = hinge.spring.damper,
            targetPosition = open ? openAngle : closeAngle
        };

        IsOpen = open;
    }
}
