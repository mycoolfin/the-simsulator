using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using ECS.Rendering;

    public class CreatureVisualiser : MonoBehaviour
    {
        [SerializeField] private PhenotypeCompanionObject terrestrialPhenotypeCompanionObject;
        [SerializeField] private PhenotypeCompanionObject aquaticPhenotypeCompanionObject;
        [SerializeField] private CapsuleDock dock;
        [SerializeField] private GameObject hologram;
        [SerializeField] private EmitterController emitter;

        private Quaternion initialCreatureRotation = Quaternion.identity;
        private Quaternion desiredCreatureRotation = Quaternion.identity;
        private const float rotationMagnitudeDegrees = 15f;

        private Vector3 initialCreatureZoom = Vector3.one;
        private Vector3 desiredCreatureZoom = Vector3.one;
        private const float zoomFactor = 1.1f;

        private bool needsRotationUpdate = false;
        private bool needsZoomUpdate = false;
        private const float rotationThreshold = 0.001f;
        private const float scaleThreshold = 0.001f;

        private ICreatureCapsule currentCapsule;

        private void Start()
        {
            dock.Socket.selectEntered.AddListener((_) => SetCapsule(dock.DockedCapsule));
            dock.Socket.selectExited.AddListener((_) => SetCapsule(dock.DockedCapsule));

            hologram.SetActive(currentCapsule != null);
            emitter.SetEmissiveColor(Color.white);
            emitter.SetEmissiveIntensity(0f);
        }

        private void Update()
        {
            if (!needsRotationUpdate && !needsZoomUpdate)
                return;

            if (needsRotationUpdate)
            {
                // Lerp creature rotation towards desired rotation.
                Quaternion rot = Quaternion.Slerp(
                    terrestrialPhenotypeCompanionObject.transform.rotation,
                    desiredCreatureRotation,
                    Time.deltaTime * 10f
                );

                if (Quaternion.Angle(rot, desiredCreatureRotation) < rotationThreshold)
                {
                    rot = desiredCreatureRotation;
                    needsRotationUpdate = false;
                }

                terrestrialPhenotypeCompanionObject.transform.rotation = rot;
                aquaticPhenotypeCompanionObject.transform.rotation = rot;
            }

            if (needsZoomUpdate)
            {
                // Lerp creature zoom towards desired zoom.
                Vector3 scale = Vector3.Lerp(
                    terrestrialPhenotypeCompanionObject.transform.localScale,
                    desiredCreatureZoom,
                    Time.deltaTime * 10f
                );

                if (Vector3.Distance(scale, desiredCreatureZoom) < scaleThreshold)
                {
                    scale = desiredCreatureZoom;
                    needsZoomUpdate = false;
                }

                terrestrialPhenotypeCompanionObject.transform.localScale = scale;
                aquaticPhenotypeCompanionObject.transform.localScale = scale;
            }
        }

        private void SetCapsule(ICreatureCapsule capsule)
        {
            if (capsule == null)
            {
                if (currentCapsule != null)
                {
                    currentCapsule.SetCompanionObjectOverride(null);
                    currentCapsule.OnCreatureLoaded -= HandleCapsuleUpdate;
                    currentCapsule = null;
                }
            }
            else
            {
                currentCapsule = capsule;
                currentCapsule.OnCreatureLoaded += HandleCapsuleUpdate;
                HandleCapsuleUpdate(currentCapsule.Creature);
            }

            hologram.SetActive(currentCapsule != null);
            emitter.SetEmissiveIntensity(currentCapsule != null ? 10f : 0f);
        }

        private void HandleCapsuleUpdate(ICreature _)
        {
            currentCapsule.SetCompanionObjectOverride(currentCapsule.Environment == CapsuleEnvironment.Terrestrial ? terrestrialPhenotypeCompanionObject : aquaticPhenotypeCompanionObject);
        }

        public void RotateLeft()
        {
            desiredCreatureRotation = Quaternion.Euler(0, -rotationMagnitudeDegrees, 0) * desiredCreatureRotation;
            needsRotationUpdate = true;
        }

        public void RotateRight()
        {
            desiredCreatureRotation = Quaternion.Euler(0, rotationMagnitudeDegrees, 0) * desiredCreatureRotation;
            needsRotationUpdate = true;
        }

        public void ResetRotation()
        {
            terrestrialPhenotypeCompanionObject.transform.rotation = initialCreatureRotation;
            aquaticPhenotypeCompanionObject.transform.rotation = initialCreatureRotation;
            desiredCreatureRotation = initialCreatureRotation;
            needsRotationUpdate = false;
        }

        public void ZoomIn()
        {
            desiredCreatureZoom *= zoomFactor;
            needsZoomUpdate = true;
        }

        public void ZoomOut()
        {
            desiredCreatureZoom /= zoomFactor;
            needsZoomUpdate = true;
        }

        public void ResetZoom()
        {
            terrestrialPhenotypeCompanionObject.transform.localScale = initialCreatureZoom;
            aquaticPhenotypeCompanionObject.transform.localScale = initialCreatureZoom;
            desiredCreatureZoom = initialCreatureZoom;
            needsZoomUpdate = false;
        }
    }
}
