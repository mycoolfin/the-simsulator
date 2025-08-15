using System.Collections.Generic;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public class ButtonGroup : MonoBehaviour
    {
        [SerializeField] private List<PushButton> buttons = new();
        public int ActiveButtonIndex { get; private set; } = -1;

        private void Start()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                buttons[i].OnButtonPressed += (isActive) => SetActiveButton(index);
            }
        }

        private void Update()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].IsActive && i != ActiveButtonIndex) buttons[i].SetActive(false);
                if (!buttons[i].IsActive && i == ActiveButtonIndex) buttons[i].SetActive(true);
            }
        }

        public void SetActiveButton(int index)
        {
            if (index >= 0 && index < buttons.Count)
                ActiveButtonIndex = index;
        }

        public void SetGroupDisabled(bool disabled)
        {
            foreach (PushButton button in buttons)
                button.SetDisabled(disabled);
        }
    }
}
