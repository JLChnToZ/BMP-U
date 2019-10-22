using UnityEngine;
using UniRx.Async;
using BMS;
using BananaBeats.Visualization;
using BananaBeats.Configs;
using BananaBeats.Inputs;

using UnityEngine.UI;

namespace BananaBeats {
    public class LoaderTestManager: MonoBehaviour {

        private BMSLoader loader;

        public NoteAppearanceSetting appearanceSetting;
        public BGADisplayManager bgaPrefab;
        private BGADisplayManager instaniatedBGA;

        public Button loadButton, pauseButton, loadPanelLoadButton, loadPanelCancelButton;
        public RectTransform loadPanel;
        public InputField bmsInput;

        public Text scoreText;
        public Text comboText;

        public bool auto;

#if UNITY_EDITOR
        protected void Awake() {
            UnityEditor.EditorApplication.pauseStateChanged += OnPause;
        }
#endif

        protected void Start() {
            AudioResource.InitEngine();

            InputSystem.GetActionMap();

            BMSPlayableManager.ScoreConfig = new ScoreConfig {
                comboBonusRatio = 0.4F,
                maxScore = 10000000,
                timingConfigs = new[] {
                    new TimingConfig { rankType = 0, score = 1F, secondsDiff = 0.07F, },
                    new TimingConfig { rankType = 1, score = 0.8F, secondsDiff = 0.2F, },
                    new TimingConfig { rankType = 2, score = 0.5F, secondsDiff = 0.4F, },
                    new TimingConfig { rankType = -1, score = 0F, secondsDiff = 1F, },
                },
            };

            instaniatedBGA = Instantiate(bgaPrefab);

            appearanceSetting?.Init();

            loadButton.onClick.AddListener(() => {
                loadPanel.gameObject.SetActive(true);
            });

            pauseButton.onClick.AddListener(() => {
                var player = BMSPlayableManager.Instance;
                if(player == null) return;
                switch(player.PlaybackState) {
                    case PlaybackState.Paused: player.Play(); break;
                    case PlaybackState.Playing: player.Pause(); break;
                }
            });

            loadPanelLoadButton.onClick.AddListener(() => {
                TestLoadBMS(bmsInput.text);
                loadPanel.gameObject.SetActive(false);
            });

            loadPanelCancelButton.onClick.AddListener(() => {
                loadPanel.gameObject.SetActive(false);
            });
        }

        private UniTaskVoid TestLoadBMS(string path) {
            loader?.Dispose();
            loader = new BMSLoader(path);
            return ReloadBMS();
        }

        private async UniTaskVoid ReloadBMS() {
            Debug.Log("Parse file");
            await UniTask.SwitchToTaskPool();
            loader.Chart.Parse(ParseType.Header | ParseType.Content | ParseType.ContentSummary | ParseType.Resources);
            await UniTask.SwitchToMainThread();
            Debug.Log("Debug Info:");
            Debug.Log($"Title: {loader.Chart.Title}");
            Debug.Log($"Sub Title: {loader.Chart.SubTitle}");
            Debug.Log($"Artist: {loader.Chart.Artist}");
            Debug.Log($"Sub Artist: {loader.Chart.SubArtist}");
            Debug.Log($"Genre: {loader.Chart.Genre}");
            Debug.Log($"Layout: {loader.Chart.Layout}");
            Debug.Log("Load audio");
            await loader.LoadAudio();
            Debug.Log("Load images");
            await loader.LoadImages();
            Debug.Log("Init player");
            var player = BMSPlayableManager.Load(loader);
            if(auto)
                player.PlayableLayout = BMSKeyLayout.None;
            Debug.Log("Load BGA layers");
            instaniatedBGA.Load(player);
            Debug.Log("Start play BMS (sound only)");
            player.Play();
            player.PlaybackStateChanged += PlaybackStateChanged;
        }

        private void PlaybackStateChanged(object sender, System.EventArgs e) {
            var player = BMSPlayableManager.Instance;
            if(player.PlaybackState == PlaybackState.Stopped) {
                player.PlaybackStateChanged -= PlaybackStateChanged;
                ReloadBMS().Forget();
            }
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

        protected void Update() {
            var player = BMSPlayableManager.Instance;
            if(player == null) return;
            if(scoreText != null)
                scoreText.text = player.Score.ToString();
            if(comboText != null)
                comboText.text = player.Combos.ToString();
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged -= OnPause;
#endif
        }
    }
}