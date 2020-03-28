#pragma warning disable CS0649
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UniRx;
using BMS;
using System;

namespace BananaBeats.UI {
    public class ConfigDialog: MonoBehaviour {
        private const BMSKeyLayout BMSKeyLayoutAll = (BMSKeyLayout)0x3FFFF;

        [SerializeField]
        private Toggle loadImagesToggle;
        [SerializeField]
        private Slider dimBackground;
        [SerializeField]
        private Toggle loadSoundsToggle;
        [SerializeField]
        private Toggle autoPlayToggle;
        [SerializeField]
        private Toggle bpmSpeedToggle;
        [SerializeField]
        private Slider speedSlider;
        [SerializeField]
        private Slider offsetSlider;
        [SerializeField]
        private Toggle detuneToggle;
        [SerializeField]
        private Slider detuneSlider;
        [SerializeField]
        private Button finishButton;
        [SerializeField]
        private Button selectAllTogglesButton;
        [SerializeField]
        private Button deselectAllTogglesButton;

        [SerializeField]
        private ChannelToggle channelTogglePrefab;
        [SerializeField]
        private RectTransform channelToggleContainer;
        private readonly Dictionary<BMSKeyLayout, ChannelToggle> channelToggles = new Dictionary<BMSKeyLayout, ChannelToggle>();
        private bool firstRun;

        private BMSKeyLayout layout;
        private BMSKeyLayout enabledLayout = BMSKeyLayoutAll;

        public BMSGameConfig Config {
            get => new BMSGameConfig {
                loadImages = loadImagesToggle.isOn,
                backgroundDim = dimBackground.value,
                loadSounds = loadSoundsToggle.isOn,
                autoPlay = autoPlayToggle.isOn,
                bpmAffectSpeed = bpmSpeedToggle.isOn,
                speed = speedSlider.value,
                offset = offsetSlider.value,
                detune = detuneToggle.isOn ? detuneSlider.value : 0,
                playableChannels = layout,
            };
            set {
                loadImagesToggle.isOn = value.loadImages;
                dimBackground.value = value.backgroundDim;
                loadSoundsToggle.isOn = value.loadSounds;
                autoPlayToggle.isOn = value.autoPlay;
                bpmSpeedToggle.isOn = value.bpmAffectSpeed;
                speedSlider.value = value.speed;
                offsetSlider.value = value.offset;
                bool detune = value.detune > 0;
                detuneToggle.isOn = detune;
                if(detune) detuneSlider.value = value.detune;
                if(layout != value.playableChannels) {
                    layout = value.playableChannels;
                    if(!firstRun) ChannelToggle.UpdateToggles(channelToggles, layout);
                }
            }
        }

        public UnityEvent OnCompleted =>
            finishButton.onClick;

        protected void Awake() {
            firstRun = true;
            detuneSlider.onValueChanged
                .AsObservable()
                .TakeUntilDestroy(this)
                .Subscribe(OnDetuneChanged);
            detuneToggle.onValueChanged
                .AsObservable()
                .TakeUntilDestroy(this)
                .Subscribe(OnDetuneEnabled);
            selectAllTogglesButton.onClick
                .AsObservable()
                .TakeUntilDestroy(this)
                .Subscribe(OnSelectAllClicked);
            deselectAllTogglesButton.onClick
                .AsObservable()
                .TakeUntilDestroy(this)
                .Subscribe(OnDeselectAllClicked);
            OnCompleted.AsObservable()
                .TakeUntilDestroy(this)
                .Subscribe(OnCompleteClick);
        }

        private void OnDetuneChanged(float value) =>
            detuneToggle.SetIsOnWithoutNotify(value > 0);

        private void OnDetuneEnabled(bool isOn) =>
            detuneSlider.interactable = isOn;

        protected void OnEnable() {
            var layout = this.layout;
            ChannelToggle.Prepare(
                firstRun, channelToggles, channelTogglePrefab, channelToggleContainer)
                .TakeUntilDisable(this)
                .Subscribe(ChannelToggleChanged);
            ChannelToggle.UpdateToggles(channelToggles, this.layout = layout);
            firstRun = false;
        }

        private void ChannelToggleChanged(BMSKeyLayout layout) =>
            this.layout = layout;

        private void OnSelectAllClicked(Unit _) =>
            ChannelToggle.UpdateToggles(channelToggles, layout = BMSKeyLayoutAll & enabledLayout);

        private void OnDeselectAllClicked(Unit _) =>
            ChannelToggle.UpdateToggles(channelToggles, layout = BMSKeyLayout.None);

        private void OnCompleteClick(Unit _) =>
            Destroy(gameObject);

        public void ChangeChannelTogglesEnabled(BMSKeyLayout enabledLayout = BMSKeyLayoutAll) =>
            ChannelToggle.ChangeTogglesEnabled(channelToggles, this.enabledLayout = enabledLayout);
    }
}
