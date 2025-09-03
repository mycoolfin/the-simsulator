using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using ECS.Rendering;

    public class CreatureVisualiser : MonoBehaviour
    {
        [SerializeField] private PhenotypeCompanionObject phenotypeCompanionObject;
        [SerializeField] private CapsuleDock dock;
        [SerializeField] private GameObject hologram;
        [SerializeField] private EmitterController emitter;

        private ICreatureCapsule currentCapsule;

        private void Start()
        {
            dock.Socket.selectEntered.AddListener((_) => SetCapsule(dock.DockedCapsule));
            dock.Socket.selectExited.AddListener((_) => SetCapsule(dock.DockedCapsule));

            hologram.SetActive(currentCapsule != null);
            emitter.SetEmissiveColor(Color.white);
            emitter.SetEmissiveIntensity(0f);
        }

        private void SetCapsule(ICreatureCapsule capsule)
        {
            if (capsule == null)
            {
                currentCapsule?.SetCompanionObjectOverride(null);
                currentCapsule = null;
            }
            else
            {
                currentCapsule = capsule;
                currentCapsule.SetCompanionObjectOverride(phenotypeCompanionObject);
            }

            hologram.SetActive(currentCapsule != null);
            emitter.SetEmissiveIntensity(currentCapsule != null ? 10f : 0f);
        }
    }
}
