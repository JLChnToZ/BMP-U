#pragma warning disable CS0649
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace BananaBeats.UI {
    [RequireComponent(typeof(RectTransform))]
    public class RebindDialog: MonoBehaviour {

        [NonSerialized]
        private RebindingOperation rebindingOperation;
        [NonSerialized]
        private InputAction inputAction;

        [SerializeField]
        private Text controlName;
        [SerializeField]
        private Button changeButton;
        [SerializeField]
        private Button resetButton;
        [SerializeField]
        private Text bindingText;

        private void Awake() {
            if(changeButton != null)
                changeButton.onClick.AddListener(ChangeClicked);
            if(resetButton != null)
                resetButton.onClick.AddListener(ResetClicked);
        }

        public void SetInputActionForBinding(InputAction inputAction) {
            this.inputAction = inputAction;
            UpdateDisplay(inputAction);
        }

        private void ChangeClicked() {
            if(rebindingOperation != null && rebindingOperation.started) {
                rebindingOperation.Complete();
                rebindingOperation = null;
            } else {
                rebindingOperation = inputAction?
                    .PerformInteractiveRebinding()
                    .WithTargetBinding(0)
                    .WithCancelingThrough("<keyboard>/escape")
                    .WithControlsExcluding("<mouse>/*")
                    .OnPotentialMatch(UpdateDisplay)
                    .OnComplete(UpdateDisplay)
                    .Start();
            }
            UpdateDisplay(inputAction);
        }

        private void ResetClicked() {
            if(rebindingOperation != null) {
                rebindingOperation.Cancel();
                rebindingOperation = null;
            }
            inputAction?.RemoveAllBindingOverrides();
            UpdateDisplay(inputAction);
        }

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
                    bindingText.text = GetBindingPath(bindings[0]);
                    break;
                default: {
                    int i = 0;
                    var pathDisplay = new string[count];
                    foreach(var binding in bindings)
                        pathDisplay[i++] = GetBindingPath(binding);
                    bindingText.text = string.Join(", ", pathDisplay);
                    break;
                }

            }
        }

        private static string GetBindingPath(InputBinding binding) {
            var overridePath = binding.overridePath;
            return string.IsNullOrWhiteSpace(overridePath) ? binding.path : overridePath;
        }

        private void OnFinalize(RebindingOperation rebindingOperation) {
        }

        private void OnDestroy() {
            if(rebindingOperation != null)
                rebindingOperation.Dispose();
        }
    }
}
