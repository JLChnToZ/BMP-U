using UnityEngine;
using UniRx.Async;
using BMS;
using BananaBeats.Visualization;
using BananaBeats.Configs;

namespace BananaBeats {
    public class GameManager: MonoBehaviour {

        public string bmsPath;

        private BMSLoader loader;
        private BMSPlayableManager player;

        public NoteAppearanceSetting appearanceSetting;
        public BGADisplayManager bgaPrefab;
        private BGADisplayManager instaniatedBGA;

        protected void Start() {
            appearanceSetting?.Init();
            TestLoadBMS().Forget();
        }

        private async UniTaskVoid TestLoadBMS() {
            Debug.Log("Start load BMS test");
            Debug.Log("Init BASS sound engine");
            AudioResource.InitEngine();
            Debug.Log("Load file");
            loader = new BMSLoader(bmsPath);
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
            await UniTask.SwitchToMainThread();
            Debug.Log("Load BGA layers");
            instaniatedBGA = Instantiate(bgaPrefab);
            instaniatedBGA.Load(player);
            Debug.Log("Start play BMS (sound only)");
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
        }
    }
}