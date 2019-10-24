using System;
using UnityEngine;
using UnityEngine.UI;

namespace BananaBeats.UI {
    [RequireComponent(typeof(RectTransform))]
    public class BindList: MonoBehaviour {
        public RebindDialog prefab;
        public RectTransform container;
        public Button closeButton;

        private void Awake() {
            foreach(var channel in Inputs.InputSystem.Inputs) {
                var component = Instantiate(prefab, container);
                component.SetInputActionForBinding(channel);
            }
            closeButton.onClick.AddListener(CloseClick);
        }

        private void CloseClick() {
            gameObject.SetActive(false);
        }
    }
}
