using System;
using UnityEngine;
using TMPro;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.XR
{
    public class XRControllerTooltips : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Camera hmdCamera;

        [Header("Gaze Settings")]
        [SerializeField, Range(0f, 90f)] private float viewAngleThreshold = 25f;
        [SerializeField] private float maxViewDistance = 2.0f;
        [SerializeField] private LayerMask occlusionMask = ~0;
        [SerializeField] private float showHideLerp = 16f;
        [SerializeField] private bool requireLineOfSight = true;

        [Header("Anchors")]
        [SerializeField] private TooltipEntry[] entries;

        [Header("Line Style")]
        [SerializeField] private float lineWidth = 0.001f;
        [SerializeField] private Material lineMaterial;

        private bool visible;
        private float alpha;

        [Serializable]
        public class TooltipEntry
        {
            public string labelText = "Trigger";
            public Transform targetButton;
            public Transform labelAnchor;
            public TextMeshPro labelTMP;
            public LineRenderer line;
        }

        private void Awake()
        {
            // Ensure each entry has a line renderer.
            foreach (TooltipEntry e in entries)
            {
                if (!e.line && e.labelAnchor)
                    e.line = e.labelAnchor.gameObject.AddComponent<LineRenderer>();

                if (e.line)
                {
                    e.line.positionCount = 2;
                    e.line.startWidth = lineWidth;
                    e.line.endWidth = lineWidth;
                    if (lineMaterial) e.line.material = lineMaterial;
                    e.line.useWorldSpace = true;
                    e.line.enabled = false;
                }

                if (e.labelTMP)
                    e.labelTMP.text = e.labelText;
            }
        }

        private void LateUpdate()
        {
            if (!hmdCamera) return;

            bool shouldShow = ShouldShow();

            // Smooth fade.
            visible = shouldShow;
            float targetAlpha = visible ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, targetAlpha, showHideLerp * Time.deltaTime);

            // Enable/disable lines based on alpha.
            bool linesOn = alpha > 0.01f;

            // Update label facing + lines.
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (!e.targetButton || !e.labelAnchor) continue;

                // Use the anchor's position and face the HMD.
                FaceCamera(e.labelAnchor, hmdCamera.transform);

                if (e.labelTMP)
                {
                    e.labelTMP.gameObject.SetActive(true);
                    Color textColor = e.labelTMP.color;
                    textColor.a = alpha;
                    e.labelTMP.color = textColor;
                }

                if (e.line)
                {
                    e.line.enabled = linesOn;
                    if (linesOn)
                    {
                        e.line.SetPosition(0, e.labelAnchor.position);
                        e.line.SetPosition(1, e.targetButton.position);
                        
                        if (e.line.material != null)
                        {
                            Color lineColor = e.line.material.color;
                            lineColor.a = alpha;
                            e.line.material.color = lineColor;
                        }
                    }
                }
            }
        }

        private bool ShouldShow()
        {
            Vector3 toController = transform.position - hmdCamera.transform.position;
            float distance = toController.magnitude;
            if (distance > maxViewDistance) return false;

            float angle = Vector3.Angle(hmdCamera.transform.forward, toController);
            if (angle > viewAngleThreshold) return false;

            if (requireLineOfSight)
            {
                // Ray from eyes to controller; if something blocks it, don’t show.
                if (Physics.Raycast(hmdCamera.transform.position, toController.normalized, out RaycastHit hit, distance, occlusionMask))
                {
                    // Allow if we actually hit the controller or one of its children.
                    if (!IsPartOf(hit.transform, transform)) return false;
                }
            }

            return true;
        }

        private static void FaceCamera(Transform t, Transform cam)
        {
            // Billboard toward camera with full 3D rotation.
            Vector3 fwd = (t.position - cam.position).normalized; // Face the camera.
            if (fwd.sqrMagnitude < 0.0001f) fwd = t.forward;
            t.rotation = Quaternion.LookRotation(fwd, cam.up);
        }

        private static bool IsPartOf(Transform child, Transform root)
        {
            var p = child;
            while (p != null)
            {
                if (p == root) return true;
                p = p.parent;
            }
            return false;
        }
    }
}
