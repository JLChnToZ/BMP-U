using UnityEngine;
using UniRx.Async;
using BMS;
using BananaBeats.Visualization;
using BananaBeats.Configs;

using UnityEngine.UI;

namespace BananaBeats {
    public class GameManager: MonoBehaviour {

        private BMSLoader loader;
        private BMSPlayableManager player;

        public NoteAppearanceSetting appearanceSetting;
        public BGADisplayManager bgaPrefab;
        private BGADisplayManager instaniatedBGA;

        public Button loadButton, pauseButton, loadPanelLoadButton, loadPanelCancelButton;
        public RectTransform loadPanel;
        public InputField bmsInput;

#if UNITY_EDITOR
        protected void Awake() {
            UnityEditor.EditorApplication.pauseStateChanged += OnPause;
        }
#endif

        protected void Start() {
            AudioResource.InitEngine();

            instaniatedBGA = Instantiate(bgaPrefab);

            appearanceSetting?.Init();

            loadButton.onClick.AddListener(() => {
                loadPanel.gameObject.SetActive(true);
            });

            pauseButton.onClick.AddListener(() => {
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
            if(loader != null)
                loader.Dispose();
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
            player = new BMSPlayableManager(loader) {
                PlayableLayout = BMSKeyLayout.None, // Full auto
            };
            Debug.Log("Load BGA layers");
            instaniatedBGA.Load(player);
            Debug.Log("Start play BMS (sound only)");
            player.Play();
            player.PlaybackStateChanged += PlaybackStateChanged;
        }

        private void PlaybackStateChanged(object sender, System.EventArgs e) {
            if(player.PlaybackState == PlaybackState.Stopped) {
                player.PlaybackStateChanged -= PlaybackStateChanged;
                ReloadBMS().Forget();
            }
        }

#if UNITY_EDITOR
        private void OnPause(UnityEditor.PauseState pauseState) {
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
            if(player != null) {
                player.Dispose();
                player = null;
            }
            if(instaniatedBGA != null) {
                Destroy(instaniatedBGA);
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged -= OnPause;
#endif
        }
    }
}