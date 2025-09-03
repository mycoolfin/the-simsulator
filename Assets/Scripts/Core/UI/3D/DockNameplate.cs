using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    [RequireComponent(typeof(AudioSource))]
    public class DockNameplate : MonoBehaviour
    {
        [SerializeField] private AudioClip renameSuccessSound;
        [SerializeField] private AudioClip renameFailureSound;
        [SerializeField] private EditableText editableText;

        private AudioSource audioSource;
        private ICreatureCapsule currentCapsule;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            editableText.OnCommitted += RenameCreature;
            AssignCreatureCapsule(null);
        }

        public void AssignCreatureCapsule(ICreatureCapsule capsule)
        {
            AssignCreature(capsule?.Creature);

            if (capsule == null)
            {
                if (currentCapsule != null)
                    currentCapsule.OnCreatureLoaded -= AssignCreature;
            }
            else
                capsule.OnCreatureLoaded += AssignCreature;
            currentCapsule = capsule;
        }

        private void AssignCreature(ICreature creature)
        {
            if (creature == null)
            {
                editableText.SetText(string.Empty);
                editableText.CanEdit = false;
            }
            else
            {
                editableText.SetText(creature.Name);
                editableText.CanEdit = true;
            }
        }

        private void RenameCreature(string newName)
        {
            if (currentCapsule?.Creature != null && newName != currentCapsule.Creature.Name)
            {
                bool success = currentCapsule.RenameCreature(newName);
                if (success)
                {
                    currentCapsule.Creature.Name = newName;
                    audioSource.PlayOneShot(renameSuccessSound);
                }
                else
                {
                    audioSource.PlayOneShot(renameFailureSound);
                }
                editableText.SetText(currentCapsule.Creature.Name);
            }
        }
    }
}
