using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.ThreeD
{
    using Player.FPS;

    public sealed class EditableText : MonoBehaviour, ISelectable
    {
        [SerializeField] private TextMeshPro label;

        [Header("Behaviour")]
        [SerializeField] private int maxLength = 32;
        [SerializeField] private bool clickOutsideCancels = true;

        [Header("Visuals")]
        [SerializeField] private string caretChar = "|";
        [SerializeField] private float caretBlinkPeriod = 0.6f;

        [Header("Backspace Repeat")]
        [SerializeField] private float backspaceInitialDelay = 0.35f;
        [SerializeField] private float backspaceRepeatRate = 0.05f;

        public event Action<string> OnCommitted;
        public event Action OnCanceled;

        public bool CanEdit = true;

        private bool editing;
        private int editStartedFrame;
        private string originalText;
        private readonly StringBuilder buffer = new(64);

        private float caretTimer;
        private bool caretOn;

        private float backspaceTimer;
        private bool backspaceRepeating;

        public Vector3 WorldPosition => transform.position;
        public Quaternion WorldRotation => transform.rotation;
        public Bounds Bounds => label.bounds;

        private void OnEnable()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput += OnTextChar;
        }

        private void OnDisable()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= OnTextChar;
        }

        public void SetText(string text)
        {
            if (editing)
                EndEdit(commit: false);

            buffer.Clear().Append(text);
            UpdateVisual(forceUpdate: true);
        }

        public void Select()
        {
            if (!editing && CanEdit)
                BeginEdit();
        }

        private void BeginEdit()
        {
            editing = true;
            editStartedFrame = Time.frameCount;
            FPSPlayerController.KeyboardControlOverridden = true;

            // Store original text for potential restoration.
            originalText = label.text ?? string.Empty;
            buffer.Clear().Append(originalText);

            // Reset caret and backspace state.
            caretTimer = 0f;
            caretOn = true;
            backspaceTimer = 0f;
            backspaceRepeating = false;

            // Configure text display for editing.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UpdateVisual();
        }

        private void EndEdit(bool commit)
        {
            editing = false;
            caretOn = false;
            FPSPlayerController.KeyboardControlOverridden = false;

            if (commit)
            {
                string bufferText = buffer.ToString();
                label.text = bufferText;
                OnCommitted?.Invoke(bufferText);
            }
            else
            {
                label.text = originalText;
                OnCanceled?.Invoke();
            }

            label.ForceMeshUpdate();
        }

        private void Update()
        {
            if (!editing)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // Handle Enter key - commit the text.
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                EndEdit(commit: true);
                return;
            }

            // Handle Escape key - cancel editing.
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                EndEdit(commit: false);
                return;
            }

            // Handle backspace with repeat functionality.
            HandleBackspace(keyboard);

            // Handle clicking outside to cancel (if enabled).
            if (clickOutsideCancels && Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                // Ignore the starting click.
                if (Time.frameCount != editStartedFrame)
                {
                    RectTransform rectTransform = (RectTransform)label.transform;
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    UnityEngine.Camera worldCamera = label.canvas == null ? null : label.canvas.worldCamera;

                    if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePosition, worldCamera))
                    {
                        EndEdit(commit: false);
                        return;
                    }
                }
            }

            // Handle caret blinking animation.
            caretTimer += Time.unscaledDeltaTime;
            if (caretTimer >= caretBlinkPeriod)
            {
                caretTimer = 0f;
                caretOn = !caretOn;
                UpdateVisual();
            }
        }

        private void OnTextChar(char c)
        {
            if (!editing)
                return;

            // Ignore control characters (Enter/Backspace are handled in Update).
            // Also ignore the caret character to prevent it from being typed.
            if (!char.IsControl(c) && c.ToString() != caretChar && buffer.Length < maxLength)
            {
                buffer.Append(c);
                UpdateVisual();
            }
        }

        private void UpdateVisual(bool forceUpdate = false)
        {
            if (!editing && !forceUpdate)
                return;

            label.text = caretOn ? buffer + caretChar : buffer.ToString();
        }

        private void HandleBackspace(Keyboard keyboard)
        {
            bool backspacePressed = keyboard.backspaceKey.isPressed;
            bool backspaceJustPressed = keyboard.backspaceKey.wasPressedThisFrame;

            if (backspaceJustPressed)
            {
                // First backspace press - delete immediately.
                if (buffer.Length > 0)
                {
                    buffer.Length -= 1;
                    UpdateVisual();
                }
                backspaceTimer = 0f;
                backspaceRepeating = false;
            }
            else if (backspacePressed)
            {
                // Backspace is being held down.
                backspaceTimer += Time.unscaledDeltaTime;

                if (!backspaceRepeating && backspaceTimer >= backspaceInitialDelay)
                {
                    // Start repeating after initial delay.
                    backspaceRepeating = true;
                    backspaceTimer = 0f;
                }
                else if (backspaceRepeating && backspaceTimer >= backspaceRepeatRate)
                {
                    // Repeat backspace deletion.
                    if (buffer.Length > 0)
                    {
                        buffer.Length -= 1;
                        UpdateVisual();
                    }
                    backspaceTimer = 0f;
                }
            }
            else
            {
                // Backspace key released - reset repeat state.
                backspaceRepeating = false;
                backspaceTimer = 0f;
            }
        }
    }
}
