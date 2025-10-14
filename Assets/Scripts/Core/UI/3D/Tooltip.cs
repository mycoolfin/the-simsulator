using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] private string text;
        public string Text => text;
    }
}
