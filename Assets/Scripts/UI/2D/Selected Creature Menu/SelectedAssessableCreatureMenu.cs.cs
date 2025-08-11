using System;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.UI.TwoD
{
    using Evolution;

    public class SelectedAssessableCreatureMenu : SelectedCreatureMenuBase<IAssessableCreature>
    {
        private Button protectButton;

        protected override void Awake()
        {
            base.Awake();
            protectButton = menu.Q<Button>("protect");

            protectButton.style.display = DisplayStyle.None;

            SetTarget(null);
            SetInfoText(null);
        }

        public void EnableProtectButton(Action onProtect)
        {
            protectButton.clicked += onProtect;
            protectButton.style.display = DisplayStyle.Flex;
        }
    }
}
