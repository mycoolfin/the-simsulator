using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Management;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.Player
{
    public class XRModeSwitcher : MonoBehaviour
    {
        [Header("Rigs")]
        [SerializeField] private GameObject desktopRig;
        [SerializeField] private CharacterController xrRigCharacterController;
        [SerializeField] private GameObject xrRigCameraOffset;
        [SerializeField] private TeleportationArea teleportationArea;

        [Header("Optional")]
        [SerializeField] private bool startXRIfHmdPresentAtLaunch = true;
        [SerializeField] private bool fallbackToDesktopIfHmdLost = true;

        private void Awake()
        {
            // Default to desktop.
            SetDesktopActive(true);
            SetXRActive(false);

            // Listen for device connect/disconnect to hot-swap.
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }

        private void Start()
        {
            if (startXRIfHmdPresentAtLaunch && IsHmdPresent())
                _ = StartXRAndSwap();
        }

        private void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }

        private async System.Threading.Tasks.Task StartXRAndSwap()
        {
            XRManagerSettings mgr = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (mgr == null) return;

            // Already running?
            if (mgr.isInitializationComplete == true)
            {
                SetDesktopActive(false);
                SetXRActive(true);
                return;
            }

            // Initialize + start XR loader.
            await System.Threading.Tasks.Task.Yield();
            mgr.InitializeLoaderSync();
            if (mgr.activeLoader == null)
                return; // No loader, stay desktop.

            mgr.StartSubsystems();

            // Swap rigs.
            SetDesktopActive(false);
            SetXRActive(true);
        }

        private void StopXRAndSwapToDesktop()
        {
            XRManagerSettings mgr = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (mgr == null) return;

            mgr.StopSubsystems();
            mgr.DeinitializeLoader();

            SetXRActive(false);
            SetDesktopActive(true);
        }

        private bool IsHmdPresent()
        {
            var list = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, list);
            bool present = list.Count > 0;

            return present;
        }

        private void OnDeviceConnected(InputDevice device)
        {
            if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
                _ = StartXRAndSwap();
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            if (!fallbackToDesktopIfHmdLost) return;

            var list = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, list);
            if (list.Count == 0)
                StopXRAndSwapToDesktop();
        }

        private void SetDesktopActive(bool active)
        {
            if (desktopRig) desktopRig.SetActive(active);
            ToggleAudioListener(desktopRig, active);
        }

        private void SetXRActive(bool active)
        {
            if (xrRigCharacterController) xrRigCharacterController.enabled = active;
            if (xrRigCameraOffset) xrRigCameraOffset.SetActive(active);
            ToggleAudioListener(xrRigCameraOffset, active);
            if (teleportationArea) teleportationArea.enabled = active;
        }

        private void ToggleAudioListener(GameObject root, bool active)
        {
            if (!root) return;
            var listener = root.GetComponentInChildren<AudioListener>(true);
            if (listener) listener.enabled = active;
        }
    }
}
