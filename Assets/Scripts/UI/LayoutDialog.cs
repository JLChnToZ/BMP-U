using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Async;
using BananaBeats.Inputs;
using BananaBeats.Layouts;
using BananaBeats.PlayerData;
using BananaBeats.Utils;
using BMS;

namespace BananaBeats.UI {
    public class LayoutDialog: MonoBehaviour {
        public ChannelToggle channelTogglePrefab;
        public LayoutPresetEntry layoutPresetPrefab;
        public RebindKeyControl rebindPrefab;

        public Dropdown layoutSelect;
        public RectTransform channelToggleContainer;
        public RectTransform rebindContainer;
        public ReorderableList layoutEditor;
        public Button removeLayoutButton;
        public Button finishButton;

        public Toggle toggleApplyBindingToAll;

        private readonly List<BMSKeyLayout> layoutSelectValues = new List<BMSKeyLayout>();
        private readonly Dictionary<BMSKeyLayout, ChannelToggle> channelToggles = new Dictionary<BMSKeyLayout, ChannelToggle>();
        private readonly HashSet<RebindKeyControl> rebindDialogs = new HashSet<RebindKeyControl>();
        private int[] layoutArrangement;

        private bool isUpdatingToggles = false;
        private bool isUpdatingArrangements = false;
        private int userDefinedCount = 0;

        protected void Awake() {
            PrepareChannelToggles();
            PrepareBinders();
            PrepareLayoutSelect();
            removeLayoutButton.onClick.AddListener(RemoveLayoutClicked);
            toggleApplyBindingToAll.onValueChanged.AddListener(ApplyBindingToAllChanged);
            layoutEditor.OnElementDropped.AddListener(LayoutElementDropped);
            finishButton.onClick.AddListener(FinishClicked);
        }

        private void PrepareLayoutSelect() {
            layoutSelect.ClearOptions();
            layoutSelectValues.Clear();
            var items = layoutSelect.options;
            layoutSelect.onValueChanged.AddListener(OnLayoutSelectChanged);
            items.Add(new Dropdown.OptionData("SP 5Key+Turnable"));
            layoutSelectValues.Add(BMSKeyLayout.Single5Key);
            items.Add(new Dropdown.OptionData("SP 7Key+Turnable"));
            layoutSelectValues.Add(BMSKeyLayout.Single7Key);
            items.Add(new Dropdown.OptionData("PMS 9Key"));
            layoutSelectValues.Add(BMSKeyLayout.Single9Key);
            items.Add(new Dropdown.OptionData("PMS 9Key (Alt)"));
            layoutSelectValues.Add(BMSKeyLayout.Single9KeyAlt);
            items.Add(new Dropdown.OptionData("DP 10Key+Turnable×2"));
            layoutSelectValues.Add(BMSKeyLayout.Duel10Key);
            items.Add(new Dropdown.OptionData("DP 14Key+Turnable×2"));
            layoutSelectValues.Add(BMSKeyLayout.Duel14Key);
            foreach(var layout in NoteLayoutManager.layoutPresets.Keys)
                switch(layout) {
                    case BMSKeyLayout.Single5Key:
                    case BMSKeyLayout.Single7Key:
                    case BMSKeyLayout.Single9Key:
                    case BMSKeyLayout.Single9KeyAlt:
                    case BMSKeyLayout.Duel10Key:
                    case BMSKeyLayout.Duel14Key:
                        continue;
                    default: {
                        items.Add(new Dropdown.OptionData($"User Defined {++userDefinedCount}"));
                        layoutSelectValues.Add(layout);
                        break;
                    }
                }
            OnLayoutSelectChanged(layoutSelect.value);
        }

        private void PrepareChannelToggles() {
            var instances = new List<IObservable<(BMSKeyLayout, BMSKeyLayout)>>(20);
            for(int i = 11; i < 30; i++)
                if(i % 10 != 0) {
                    var channelToggle = Instantiate(channelTogglePrefab, channelToggleContainer)
                        .Init(i);
                    channelToggles[NoteLayoutManager.ChannelToLayout(i)] = channelToggle;
                    instances.Add(
                        channelToggle.ToggleChanged
                        .Scan((BMSKeyLayout.None, BMSKeyLayout.None), ((BMSKeyLayout, BMSKeyLayout) o, BMSKeyLayout v) => (o.Item2, v))
                    );
                }
            instances.Merge()
                .TakeUntilDestroy(this)
                .Scan(BMSKeyLayout.None, (BMSKeyLayout o, (BMSKeyLayout, BMSKeyLayout) v) => (o & ~v.Item1) | v.Item2)
                .Subscribe(ChannelToggleChanged);
        }

        private void PrepareBinders() {
            foreach(var channel in InputManager.Inputs) {
                var component = Instantiate(rebindPrefab, rebindContainer);
                component.SetInputActionForBinding(channel);
                rebindDialogs.Add(component);
            }
        }

        private void OnLayoutSelectChanged(int index) {
            var layout = layoutSelectValues[index];
            UpdateToggles(layout);
            if(!NoteLayoutManager.layoutPresets.TryGetValue(layout, out layoutArrangement)) {
                layoutArrangement = NoteLayoutManager.GetDefaultPreset(layout);
                NoteLayoutManager.layoutPresets[layout] = layoutArrangement;
            }
            UpdateLayout();
        }

        private void ChannelToggleChanged(BMSKeyLayout value) {
            if(isUpdatingToggles) return;
            InputManager.bindings.GetOrConstruct(value);
            InputManager.SwitchBindingLayout(value);
            var temp = new List<int>();
            foreach(var i in layoutArrangement)
                if(value.HasChannel(i))
                    temp.Add(i);
            for(int i = 11; i < 30; i++)
                if(value.HasChannel(i) && Array.IndexOf(layoutArrangement, i) < 0)
                    temp.Add(i);
            if(temp.Count == layoutArrangement.Length)
                temp.CopyTo(layoutArrangement);
            else
                NoteLayoutManager.layoutPresets[value] = layoutArrangement = temp.ToArray();
            int index = layoutSelectValues.IndexOf(value);
            if(index < 0) {
                index = layoutSelectValues.Count;
                layoutSelect.options.Add(new Dropdown.OptionData($"User Defined {++userDefinedCount}"));
                layoutSelectValues.Add(value);
            }
            layoutSelect.SetValueWithoutNotify(index);
            UpdateLayout();
        }

        private void UpdateToggles(BMSKeyLayout layout) {
            isUpdatingToggles = true;
            foreach(var channelToggle in channelToggles)
                channelToggle.Value.toggle.isOn =
                    (layout & channelToggle.Key) == channelToggle.Key;
            isUpdatingToggles = false;
        }

        private void ApplyBindingToAllChanged(bool enabled) {
            foreach(var dlg in rebindDialogs)
                dlg.applyToAllLayouts = enabled;
        }

        private void UpdateLayout() {
            isUpdatingArrangements = true;
            var container = layoutEditor.Content;
            for(int i = 0; i < layoutArrangement.Length; i++) {
                if(i >= container.childCount)
                    Instantiate(layoutPresetPrefab, container).Init(layoutArrangement[i]);
                else
                    container.GetChild(i).GetComponent<LayoutPresetEntry>().Init(layoutArrangement[i]);
            }
            while(container.childCount > layoutArrangement.Length) {
                var child = container.GetChild(layoutArrangement.Length);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            layoutEditor.Refresh();
            isUpdatingArrangements = false;
        }

        private void RemoveLayoutClicked() {
            var layout = layoutSelectValues[layoutSelect.value];
            switch(layout) {
                case BMSKeyLayout.Single5Key:
                case BMSKeyLayout.Single7Key:
                case BMSKeyLayout.Single9Key:
                case BMSKeyLayout.Single9KeyAlt:
                case BMSKeyLayout.Duel10Key:
                case BMSKeyLayout.Duel14Key:
                    return;
                default: {
                    layoutSelect.options.RemoveAt(layoutSelect.value);
                    if(layoutSelect.value > layoutSelect.options.Count)
                        layoutSelect.SetValueWithoutNotify(0);
                    OnLayoutSelectChanged(layoutSelect.value);
                    break;
                }
            }
        }

        private void LayoutElementDropped(ReorderableList.ReorderableListEventStruct entry) {
            if(isUpdatingArrangements) return;
            int temp = layoutArrangement[entry.ToIndex];
            layoutArrangement[entry.ToIndex] = layoutArrangement[entry.FromIndex];
            layoutArrangement[entry.FromIndex] = temp;
        }

        private void FinishClicked() {
            Save().Forget();
            gameObject.SetActive(false);
        }

        private async UniTaskVoid Save() {
            await UniTask.SwitchToTaskPool();
            using(var playerData = new PlayerDataManager()) {
                InputManager.Save(playerData);
                NoteLayoutManager.Save(playerData);
            }
            await UniTask.SwitchToMainThread();
        }
    }
}
