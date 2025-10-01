using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    public sealed class EditableText : MonoBehaviour, ISelectable
    {
        [Header("References")]
        [SerializeField] private TMP_Text label;

        [Header("Behavior")]
        [SerializeField, Min(1)] private int maxLength = 32;
        [SerializeField] private bool clickOutsideCancels = true;

        [Header("Caret")]
        [SerializeField] private string caretChar = "|";
        [SerializeField, Min(0.05f)] private float caretBlinkPeriod = 0.6f;

        [Header("Backspace Repeat (Desktop)")]
        [SerializeField, Min(0f)] private float backspaceInitialDelay = 0.35f;
        [SerializeField, Min(0f)] private float backspaceRepeatRate = 0.05f;

        [Header("XR / Mobile")]
        [SerializeField] private bool useSystemKeyboardOnXR = true;
        [SerializeField] private string keyboardPrompt = "Enter text";

        public event Action<string> OnCommitted;
        public event Action OnCanceled;

        public bool CanEdit = true;

        // --- State ---
        private bool editing;
        private int editStartedFrame;
        private string originalText = string.Empty;
        private readonly StringBuilder buffer = new(64);

        private float caretTimer;
        private bool caretVisible;

        // Desktop backspace repeat
        private float backspaceTimer;
        private bool backspaceRepeating;

        // XR system keyboard (Quest)
#if UNITY_ANDROID && !UNITY_EDITOR
        private TouchScreenKeyboard xrKeyboard;
        private string xrKeyboardPrevText;
#endif

        // Public info expected by ISelectable
        public Vector3 WorldPosition => transform.position;
        public Quaternion WorldRotation => transform.rotation;
        public Bounds Bounds => label.bounds;

        public static bool AnyEditing => editingInstances.Count > 0;
        private static readonly List<EditableText> editingInstances = new();

        // ===== Unity lifecycle =====

        private void Awake()
        {
            if (label == null)
            {
                Debug.LogError($"{nameof(EditableText)} is missing label reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            // Desktop text input event (characters). XR uses TouchScreenKeyboard text polling.
            if (Keyboard.current != null)
                Keyboard.current.onTextInput += OnDesktopTextChar;
        }

        private void OnDisable()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= OnDesktopTextChar;
        }

        private void Update()
        {
            if (!editing) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            // XR keyboard path (Quest)
            if (useSystemKeyboardOnXR && xrKeyboard != null)
            {
                UpdateFromXRKeyboard();
                BlinkCaret();
                return;
            }
#endif
            // Desktop / fallback path
            UpdateFromDesktopKeyboard();
            BlinkCaret();
        }

        // ===== Public API =====

        public void SetText(string text)
        {
            if (editing) Cancel(); // end current session without commit
            buffer.Clear().Append(text ?? string.Empty);
            UpdateVisual(force: true);
        }

        public void Select() // ISelectable
        {
            if (editing || !CanEdit) return;
            BeginEdit();
        }

        // ===== Editing lifecycle =====

        private void BeginEdit()
        {
            editing = true;
            editingInstances.Add(this);
            editStartedFrame = Time.frameCount;

            originalText = label.text ?? string.Empty;
            buffer.Clear().Append(originalText);

            ResetCaret();
            ResetBackspace();

#if UNITY_ANDROID && !UNITY_EDITOR
            OpenXRKeyboard();
#endif
            // Ensure single-line for edit display
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UpdateVisual();
        }

        private void Commit()
        {
            var text = buffer.ToString();
            label.text = text;
            OnCommitted?.Invoke(text);
            EndEditCommonCleanup();
        }

        private void Cancel()
        {
            label.text = originalText;
            OnCanceled?.Invoke();
            EndEditCommonCleanup();
        }

        private void EndEditCommonCleanup()
        {
            editing = false;
            editingInstances.Remove(this);
            caretVisible = false;
            label.ForceMeshUpdate();

#if UNITY_ANDROID && !UNITY_EDITOR
            xrKeyboard = null; // user closes overlay; we just drop our reference
#endif
        }

        // ===== Input paths =====

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OpenXRKeyboard()
        {
            if (!useSystemKeyboardOnXR) return;

            // TouchScreenKeyboard opens the Meta System Keyboard overlay on Quest (when the OpenXR feature is enabled).
            xrKeyboard = TouchScreenKeyboard.Open(
                buffer.ToString(),
                TouchScreenKeyboardType.Default,
                autocorrection: false,
                multiline: false,
                secure: false,
                alert: false,
                textPlaceholder: keyboardPrompt
            );
            xrKeyboardPrevText = xrKeyboard != null ? xrKeyboard.text : buffer.ToString();
        }

        private void UpdateFromXRKeyboard()
        {
            // Mirror text from the overlay
            if (xrKeyboard != null && xrKeyboard.text != xrKeyboardPrevText)
            {
                SetBufferClamped(xrKeyboard.text);
                xrKeyboardPrevText = xrKeyboard.text;
                UpdateVisual();
            }

            // Completion / cancel
            if (xrKeyboard == null) return;

            switch (xrKeyboard.status)
            {
                case TouchScreenKeyboard.Status.Done:
                    Commit();
                    break;
                case TouchScreenKeyboard.Status.Canceled:
                case TouchScreenKeyboard.Status.LostFocus:
                    Cancel();
                    break;
            }
        }
#endif

        private void UpdateFromDesktopKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Enter = commit
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Commit();
                return;
            }

            // Escape = cancel
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cancel();
                return;
            }

            HandleBackspace(keyboard);
            if (clickOutsideCancels) HandleClickOutside();
        }

        // Character input (desktop only). XR uses polling from TouchScreenKeyboard.
        private void OnDesktopTextChar(char c)
        {
            if (!editing) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (useSystemKeyboardOnXR && xrKeyboard != null)
                return; // avoid double-input if XR overlay up
#endif
            // Ignore control chars and the caret glyph itself
            if (!char.IsControl(c) && c.ToString() != caretChar && buffer.Length < maxLength)
            {
                buffer.Append(c);
                UpdateVisual();
            }
        }

        // ===== Helpers =====

        private void HandleBackspace(Keyboard keyboard)
        {
            bool pressed = keyboard.backspaceKey.isPressed;
            bool justPressed = keyboard.backspaceKey.wasPressedThisFrame;

            if (justPressed)
            {
                TryDeleteOne();
                backspaceTimer = 0f;
                backspaceRepeating = false;
                return;
            }

            if (!pressed)
            {
                backspaceRepeating = false;
                backspaceTimer = 0f;
                return;
            }

            // Held
            backspaceTimer += Time.unscaledDeltaTime;
            if (!backspaceRepeating && backspaceTimer >= backspaceInitialDelay)
            {
                backspaceRepeating = true;
                backspaceTimer = 0f;
            }
            else if (backspaceRepeating && backspaceTimer >= backspaceRepeatRate)
            {
                TryDeleteOne();
                backspaceTimer = 0f;
            }
        }

        private void HandleClickOutside()
        {
            if (Mouse.current?.leftButton.wasPressedThisFrame != true) return;

            // Ignore the starting click that triggered Select()
            if (Time.frameCount == editStartedFrame) return;

            // For UGUI: proper rect hit test. For 3D TMP, you can add a collider + your own ray test.
            if (label is TextMeshProUGUI ui && ui.TryGetComponent(out RectTransform rect))
            {
                var mousePos = Mouse.current.position.ReadValue();
                var cam = ui.canvas ? ui.canvas.worldCamera : null;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, cam))
                    Cancel();
            }
            // If you're using TextMeshPro (3D) without a rect, consider adding a collider and doing your own Physics ray check here.
        }

        private void BlinkCaret()
        {
            caretTimer += Time.unscaledDeltaTime;
            if (caretTimer >= caretBlinkPeriod)
            {
                caretTimer = 0f;
                caretVisible = !caretVisible;
                UpdateVisual();
            }
        }

        private void UpdateVisual(bool force = false)
        {
            if (!editing && !force) return;
            label.text = caretVisible ? buffer + caretChar : buffer.ToString();
        }

        private void ResetCaret()
        {
            caretTimer = 0f;
            caretVisible = true;
        }

        private void ResetBackspace()
        {
            backspaceTimer = 0f;
            backspaceRepeating = false;
        }

        private void TryDeleteOne()
        {
            if (buffer.Length <= 0) return;
            buffer.Length -= 1;
            UpdateVisual();
        }

        private void SetBufferClamped(string text)
        {
            buffer.Clear();
            if (string.IsNullOrEmpty(text)) return;

            if (text.Length > maxLength) buffer.Append(text, 0, maxLength);
            else buffer.Append(text);
        }
    }
}
