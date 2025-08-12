using System;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.TwoD
{
    using Core.UI;

    public class MainMenuButton : MonoBehaviour, ISelectable
    {
        public event Action OnSelect = delegate { };

        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;
        private Color defaultColor;
        public bool hovering;

        public Vector3 WorldPosition => transform.position;
        public Quaternion WorldRotation => transform.rotation;
        public Bounds Bounds => meshCollider.bounds;

        private void Start()
        {
            meshCollider = GetComponent<MeshCollider>();
            meshRenderer = GetComponent<MeshRenderer>();
            defaultColor = meshRenderer.material.color;
        }

        private void Update()
        {
            float a = Mathf.Lerp(meshRenderer.material.color.a, hovering ? 1f : defaultColor.a, Time.deltaTime * 10f);
            meshRenderer.material.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, a);
        }

        public void Select()
        {
            OnSelect();
        }
    }
}
