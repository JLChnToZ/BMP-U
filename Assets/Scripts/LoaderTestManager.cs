using UnityEngine;
using UniRx;
using UniRx.Async;
using BMS;
using BananaBeats.Visualization;
using BananaBeats.Configs;
using BananaBeats.Inputs;
using BananaBeats.UI;

using UnityEngine.UI;

namespace BananaBeats {
    public class LoaderTestManager: MonoBehaviour {

        private BMSLoader loader;

        public NoteAppearanceSetting appearanceSetting;
        public BGADisplayManager bgaPrefab;
        private BGADisplayManager instaniatedBGA;

        public Button pauseButton, loadPanelLoadButton, loadPanelCancelButton, keyBindingsButton, configButton;
        public Canvas canvasRoot;
        public LayoutDialog bindingsPanel;
        public ConfigDialog configPanel;
        public RectTransform loadPanel;
        public InputField bmsInput;
        public Toggle autoMode;

        private BMSGameConfig config = BMSGameConfig.Default;

#if UNITY_EDITOR
        protected void Awake() {
            UnityEditor.EditorApplication.pauseStateChanged += OnPause;
        }
#endif

        protected void Start() {
            BMSPlayableManager.GlobalPlayStateChanged += PlaybackStateChanged;

            instaniatedBGA = Instantiate(bgaPrefab);

            if(appearanceSetting != null) appearanceSetting.Init();

            pauseButton.onClick.AddListener(() => {
                var player = BMSPlayableManager.Instance;
                if(player != null && player.PlaybackState == PlaybackState.Playing)
                    player.Pause();
                loadPanel.gameObject.SetActive(true);
            });

            loadPanelLoadButton.onClick.AddListener(() => {
                TestLoadBMS(bmsInput.text);
                loadPanel.gameObject.SetActive(false);
            });

            loadPanelCancelButton.onClick.AddListener(() => {
                loadPanel.gameObject.SetActive(false);
                var player = BMSPlayableManager.Instance;
                if(player != null && player.PlaybackState == PlaybackState.Paused)
                    player.Play();
            });

            keyBindingsButton.onClick.AddListener(() => {
                Instantiate(bindingsPanel, canvasRoot.transform);
            });

            configButton.onClick.AddListener(() => {
                var panel = Instantiate(configPanel, canvasRoot.transform);
                panel.Config = config;
                panel.OnCompleted.AsObservable().Subscribe(_ => config = panel.Config);
            });
        }

        private UniTaskVoid TestLoadBMS(string path) {
            loader?.Dispose();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            path = path.Replace('\\', '/');
#endif
            loader = new BMSLoader(path);
            return ReloadBMS();
        }

        private async UniTaskVoid ReloadBMS() {
            await UniTask.SwitchToTaskPool();
            loader.Chart.Parse(ParseType.Header | ParseType.Content | ParseType.ContentSummary | ParseType.Resources);
            await UniTask.SwitchToMainThread();
            HUD.GameHUDManager.UpdateHUD(loader);
            await loader.LoadAudio();
            await loader.LoadImages();
            var player = BMSPlayableManager.Load(loader);
            player.ApplyConfig(config);
            instaniatedBGA.Load(player);
            instaniatedBGA.ApplyConfig(config);
            player.Play();
        }

        private void PlaybackStateChanged(object sender, System.EventArgs e) {
            var player = BMSPlayableManager.Instance;
            if(player.PlaybackState == PlaybackState.Stopped)
                ReloadBMS().Forget();
        }

#if UNITY_EDITOR
        private void OnPause(UnityEditor.PauseState pauseState) {
            var player = BMSPlayableManager.Instance;
            if(player == null || player.PlaybackState == PlaybackState.Stopped)
                return;
            switch(pauseState) {
                case UnityEditor.PauseState.Paused:
                    player.Pause();
                    break;
                case UnityEditor.PauseState.Unpaused:
                    player.Play();
                    break;
            }
        }
#endif

        private void OnApplicationPause(bool pause) {
            var player = BMSPlayableManager.Instance;
            if(player == null || player.PlaybackState == PlaybackState.Stopped)
                return;
            if(pause)
                player.Pause();
            else
                player.Play();
        }

        protected void OnDestroy() {
            if(loader != null) {
                loader.Dispose();
                loader.FileSystem?.Dispose();
                loader = null;
            }
            BMSPlayableManager.Instance?.Dispose();
            if(instaniatedBGA != null) {
                Destroy(instaniatedBGA);
            }
            BMSPlayableManager.GlobalPlayStateChanged -= PlaybackStateChanged;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged -= OnPause;
#endif
        }
    }
}