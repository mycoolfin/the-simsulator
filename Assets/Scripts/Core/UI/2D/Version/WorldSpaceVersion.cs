using TMPro;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.TwoD
{
    [RequireComponent(typeof(TextMeshPro))]
    public class WorldSpaceVersion : MonoBehaviour
    {
        private void Start()
        {
            TextMeshPro textMeshPro = GetComponent<TextMeshPro>();
            textMeshPro.text = "v" + Application.version;
        }
    }
}
