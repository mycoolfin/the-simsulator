using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public enum HingeBehaviour
    {
        RotateX90,
        RotateX180
    }

    [RequireComponent(typeof(Animator))]
    public class HingedPanel : MonoBehaviour
    {
        [SerializeField] private bool startOpen = false;
        [SerializeField] private HingeBehaviour hingeBehaviour = HingeBehaviour.RotateX90;
        [SerializeField] private bool invertBehaviour = false;
        public bool IsOpen { get; private set; } = false;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            SetOpen(startOpen);
        }

        public void SetOpen(bool open)
        {
            switch (hingeBehaviour)
            {
                case HingeBehaviour.RotateX90:
                    animator.SetBool("X90", open ^ invertBehaviour);
                    break;
                case HingeBehaviour.RotateX180:
                    animator.SetBool("X180", open ^ invertBehaviour);
                    break;
                default:
                    break;
            }
            IsOpen = open;
        }
    }
}
