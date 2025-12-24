using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using TheSimsulator.Core.Genotype;
    using TheSimsulator.Core.Phenotype;
    using Core.UI.TwoD;
    using Player.FPS;

    public abstract class CreatureEditor<TGenotype, TPhenotype> : MonoBehaviour
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        [SerializeField] protected FPSPlayerController playerController;
        [SerializeField] private CapsuleDock dock;
        [SerializeField] private CapsuleDock workingCopyDock;
        [SerializeField] private WorldUIScreen worldUIScreen;
        [SerializeField] protected CreatureVisualiser creatureVisualiser;
        [SerializeField] private HingedPanel consolePanel;
        [SerializeField] protected Collider genotypeEditorCollider;
        [SerializeField] protected UIDocument genotypeEditorDocument;
        [SerializeField] private PushButton startEditingButton;
        [SerializeField] private PushButton commitChangesButton;
        [SerializeField] private PushButton discardChangesButton;
        [SerializeField] private PushButton pauseTimeButton;
        [SerializeField] private PushButton resumeTimeButton;
        [SerializeField] private PushButton exportCreatureModelButton;
        [SerializeField] private PushButton rotateLeftButton;
        [SerializeField] private PushButton rotateRightButton;
        [SerializeField] private PushButton zoomInButton;
        [SerializeField] private PushButton zoomOutButton;

        private bool CanStartEditing => dock.DockedCapsule != null;
        private bool IsEditing => workingCopyDock.DockedCapsule != null;

        protected GenotypeEditor<TGenotype> genotypeEditor;

        protected ICreatureCapsule WorkingCopyCapsule => workingCopyDock.DockedCapsule;

        protected virtual void Awake()
        {
            worldUIScreen.Camera = playerController.PlayerCamera;
        }

        private void Start()
        {
            startEditingButton.SetActive(true);

            startEditingButton.OnButtonPressed += (_) => StartEditing();
            commitChangesButton.OnButtonPressed += (_) => CommitChanges();
            discardChangesButton.OnButtonPressed += (_) => DiscardChanges();
            pauseTimeButton.OnButtonPressed += (_) => PauseTime();
            resumeTimeButton.OnButtonPressed += (_) => ResumeTime();
            exportCreatureModelButton.OnButtonPressed += (_) => ExportCreatureModel();
            rotateLeftButton.OnButtonPressed += (_) => creatureVisualiser.RotateLeft();
            rotateRightButton.OnButtonPressed += (_) => creatureVisualiser.RotateRight();
            zoomInButton.OnButtonPressed += (_) => creatureVisualiser.ZoomIn();
            zoomOutButton.OnButtonPressed += (_) => creatureVisualiser.ZoomOut();
        }

        private void Update()
        {
            startEditingButton.SetDisabled(!CanStartEditing);

            commitChangesButton.SetDisabled(!IsEditing);
            discardChangesButton.SetDisabled(!IsEditing);
            pauseTimeButton.SetDisabled(!IsEditing);
            resumeTimeButton.SetDisabled(!IsEditing);
            exportCreatureModelButton.SetDisabled(!IsEditing);
            rotateLeftButton.SetDisabled(!IsEditing);
            rotateRightButton.SetDisabled(!IsEditing);
            zoomInButton.SetDisabled(!IsEditing);
            zoomOutButton.SetDisabled(!IsEditing);
        }

        private void StartEditing()
        {
            worldUIScreen.Activate();
            consolePanel.SetOpen(true);

            if (playerController != null)
            {
                playerController.SetUIMode(true, worldUIScreen.CameraTarget);
                playerController.OnUIEscapePressed += StopEditing;
            }

            if (dock.DockedCapsule != null && dock.DockedCapsule.Creature != null)
            {
                // Create working copy.
                workingCopyDock.DestroyCapsule();
                workingCopyDock.CreateAndDockEmptyCapsule(silent: true);

                void onCreatureLoaded(ICreature creature)
                {
                    workingCopyDock.DockedCapsule.OnCreatureLoaded -= onCreatureLoaded;

                    // Load genotype into editor after creature is loaded.
                    if (dock.DockedCapsule.Creature is Creature<TGenotype, TPhenotype> typedCreature)
                        genotypeEditor.LoadGenotype(typedCreature.Genotype);
                    else
                        Debug.LogError("Creature in docked capsule is not of the expected type.");
                }

                workingCopyDock.DockedCapsule.OnCreatureLoaded += onCreatureLoaded;
                workingCopyDock.DockedCapsule.InitialiseFromCreature(dock.DockedCapsule.Creature, dock.DockedCapsule.Environment);
            }
        }

        private void StopEditing()
        {
            worldUIScreen.Deactivate();
            consolePanel.SetOpen(false);

            if (playerController != null)
            {
                playerController.SetUIMode(false);
                playerController.OnUIEscapePressed -= StopEditing;
            }

            // Destroy working copy.
            workingCopyDock.DestroyCapsule();

            ResumeTime();
            creatureVisualiser.ResetRotation();
            creatureVisualiser.ResetZoom();
        }

        protected void DisplayEdits(TGenotype updatedGenotype)
        {
            if (IsEditing && workingCopyDock.DockedCapsule != null)
            {
                Creature<TGenotype, TPhenotype> creature = new()
                {
                    Genotype = updatedGenotype
                    // No need to set phenotype.
                };
                workingCopyDock.DockedCapsule.InitialiseFromCreature(creature, workingCopyDock.DockedCapsule.Environment);
            }
        }

        private void CommitChanges()
        {
            if (dock.DockedCapsule != null && workingCopyDock.DockedCapsule != null)
                dock.DockedCapsule.InitialiseFromCreature(workingCopyDock.DockedCapsule.Creature, workingCopyDock.DockedCapsule.Environment);

            StopEditing();
        }

        private void DiscardChanges()
        {
            StopEditing();
        }

        private void PauseTime()
        {
            dock.DockedCapsule?.PauseCapsuleWorldTime();
        }

        private void ResumeTime()
        {
            dock.DockedCapsule?.ResumeCapsuleWorldTime();
        }

        private void ExportCreatureModel()
        {
            ExportCreatureModel(workingCopyDock.DockedCapsule);
        }

        protected abstract void ExportCreatureModel(ICreatureCapsule dockedCapsule);

        protected virtual void OnDestroy()
        {
        }
    }
}
