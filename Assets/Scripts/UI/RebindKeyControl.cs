#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using BananaBeats.Inputs;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace BananaBeats.UI {
    [RequireComponent(typeof(RectTransform))]
    public class RebindKeyControl: MonoBehaviour {
        private static readonly HashSet<RebindKeyControl> dialogs = new HashSet<RebindKeyControl>();

        [NonSerialized]
        public bool applyToAllLayouts;

        [NonSerialized]
        private RebindingOperation rebindingOperation;
        [NonSerialized]
        private InputAction inputAction;

        [SerializeField]
        private Text controlName;
        [SerializeField]
        private Toggle changeButton;
        [SerializeField]
        private Button resetButton;
        [SerializeField]
        private Text bindingText;

        private void Awake() {
            dialogs.Add(this);
            if(changeButton != null)
                changeButton.onValueChanged.AddListener(ChangeClicked);
            if(resetButton != null)
                resetButton.onClick.AddListener(ResetClicked);
        }

        public void SetInputActionForBinding(InputAction inputAction) {
            this.inputAction = inputAction;
            UpdateDisplay(inputAction);
        }

        private void ChangeClicked(bool enabled) {
            if(enabled) StartRebinding();
            else StopRebinding();
            UpdateDisplay();
        }

        private bool StopRebinding() {
            if(rebindingOperation != null && rebindingOperation.started) {
                rebindingOperation.Complete();
                rebindingOperation.Dispose();
                rebindingOperation = null;
                return true;
            }
            return false;
        }

        private void StartRebinding() {
            foreach(var other in dialogs)
                other.StopRebinding();
            rebindingOperation = inputAction?
                .PerformInteractiveRebinding()
                .WithTargetBinding(0)
                .WithCancelingThrough("<keyboard>/escape")
                .WithControlsExcluding("<mouse>/*")
                .OnPotentialMatch(UpdateDisplay)
                .OnComplete(OnComplete)
                .Start();
        }

        private void OnComplete(RebindingOperation rebindingOperation) {
            InputManager.ApplyBinding(rebindingOperation.action, applyToAllLayouts);
            UpdateDisplay(rebindingOperation.action);
        }

        private void ResetClicked() {
            if(rebindingOperation != null) {
                rebindingOperation.Cancel();
                rebindingOperation = null;
            }
            inputAction?.RemoveAllBindingOverrides();
            UpdateDisplay();
        }

        public void UpdateDisplay() => UpdateDisplay(inputAction);

        private void UpdateDisplay(RebindingOperation rebindingOperation) =>
            UpdateDisplay(rebindingOperation.action);

        private void UpdateDisplay(InputAction inputAction) {
            if(inputAction == null) {
                controlName.text = "";
                bindingText.text = "N/A";
                return;
            }
            var bindings = inputAction.bindings;
            int count = bindings.Count;
            controlName.text = inputAction.name;
            switch(count) {
                case 0:
                    bindingText.text = "N/A";
                    break;
                case 1:
                    bindingText.text = InputManager.GetBindingPath(bindings[0]);
                    break;
                default: {
                    int i = 0;
                    var pathDisplay = new string[count];
                    foreach(var binding in bindings)
                        pathDisplay[i++] = InputManager.GetBindingPath(binding);
                    bindingText.text = string.Join(", ", pathDisplay);
                    break;
                }
            }
            if(changeButton != null) {
                bool enabled = false;
                if(rebindingOperation != null)
                    enabled = rebindingOperation.started && !rebindingOperation.completed;
                changeButton.SetIsOnWithoutNotify(enabled);
            }
        }

        private void OnDestroy() {
            dialogs.Remove(this);
            if(rebindingOperation != null)
                rebindingOperation.Dispose();
        }
    }
}
