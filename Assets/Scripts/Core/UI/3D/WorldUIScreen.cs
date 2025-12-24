using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public class WorldUIScreen : MonoBehaviour
    {
        [SerializeField] private float paddingMultiplier = 1f;
        public Camera Camera;

        private Transform dynamicCameraTarget;
        private bool isActive = false;
        private int lastScreenWidth;
        private int lastScreenHeight;

        public Transform CameraTarget => dynamicCameraTarget;

        private void Start()
        {
            // Create dynamic camera target for aspect ratio adjustment.
            dynamicCameraTarget = new GameObject($"{gameObject.name}_CameraTarget").transform;
            dynamicCameraTarget.SetParent(transform);

            // Initialize screen size tracking.
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            UpdateCameraPosition();
        }

        private void OnDestroy()
        {
            if (dynamicCameraTarget != null)
                Destroy(dynamicCameraTarget.gameObject);
        }

        private void Update()
        {
            if (isActive)
            {
                // Check if screen has been resized.
                if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                {
                    lastScreenWidth = Screen.width;
                    lastScreenHeight = Screen.height;
                    UpdateCameraPosition();
                }
            }
        }

        public void Activate()
        {
            isActive = true;
            UpdateCameraPosition();
        }

        public void Deactivate()
        {
            isActive = false;
        }

        public void UpdateCameraPosition()
        {
            float screenWidth = transform.localScale.x;
            float screenHeight = transform.localScale.y;

            float aspectRatio = (float)Screen.width / Screen.height;

            float cameraFOV = Camera.fieldOfView;

            // Calculate distance needed to fit screen width and height.
            float verticalFOV = cameraFOV * Mathf.Deg2Rad;
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(verticalFOV / 2f) * aspectRatio);

            float distanceForWidth = (screenWidth * paddingMultiplier) / (2f * Mathf.Tan(horizontalFOV / 2f));
            float distanceForHeight = (screenHeight * paddingMultiplier) / (2f * Mathf.Tan(verticalFOV / 2f));

            // Use whichever is larger to ensure both dimensions fit.
            float optimalDistance = Mathf.Max(distanceForWidth, distanceForHeight);

            // Position camera at optimal distance along screen's forward direction.
            // Make camera look back at the screen center.
            dynamicCameraTarget.SetPositionAndRotation(
                transform.position + transform.forward * optimalDistance,
                Quaternion.LookRotation(-transform.forward, Vector3.up)
            );
        }
    }
}
