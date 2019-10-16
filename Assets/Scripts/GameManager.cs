using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Async;
using BMS;
using BananaBeats.Utils;
using BananaBeats.Visualization;

namespace BananaBeats {
    public class GameManager: MonoBehaviour {

        public string bmsPath;

        private BMSLoader loader;
        private BMSPlayableManager player;

        public GameObject[] notePrefabs;
        public Material longNoteBodyMaterial;

        public BGAConfig[] bgaConfigs;

        private readonly HashSet<BGADisplay> instaniatedBGADisplays = new HashSet<BGADisplay>();

        protected void Start() {
            TestLoadBMS().Forget();
            if(notePrefabs != null)
                for(int i = 0; i < notePrefabs.Length; i++)
                    NoteDisplayManager.ConvertPrefab(notePrefabs[i], (NoteType)i);
            NoteDisplayManager.LongNoteMaterial = longNoteBodyMaterial;
        }

        private void SetTestPositions(BMSKeyLayout layout) {
            var startPos = new Vector3[20];
            var endPos = new Vector3[20];

            var channels = new List<int>(20);
            if((layout & ~BMSKeyLayout.Single5Key) == BMSKeyLayout.None)
                channels.AddRange(new[] { 6, 1, 2, 3, 4, 5, });
            else if((layout & ~BMSKeyLayout.Single7Key) == BMSKeyLayout.None)
                channels.AddRange(new[] { 6, 1, 2, 3, 4, 5, 8, 9, });
            else if((layout & ~BMSKeyLayout.Single9Key) == BMSKeyLayout.None)
                channels.AddRange(new[] { 1, 2, 3, 4, 5, 12, 13, 14, 15, });
            else if((layout & ~BMSKeyLayout.Single9KeyAlt) == BMSKeyLayout.None)
                channels.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, });
            else if((layout & ~BMSKeyLayout.Duel10Key) == BMSKeyLayout.None)
                channels.AddRange(new[] { 6, 1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, });
            else if((layout & ~BMSKeyLayout.Duel14Key) == BMSKeyLayout.None)
                channels.AddRange(new[] { 6, 1, 2, 3, 4, 5, 8, 9, 11, 12, 13, 14, 15, 18, 19, 16, });
            else
                channels.AddRange(Enumerable.Range(0, 20));

            float mid = channels.Count / 2F;
            for(int i = 0, l = channels.Count; i < l; i++) {
                startPos[channels[i]] = new Vector3((i - mid) * 1.1F, 0, 100);
                endPos[channels[i]] = new Vector3((i - mid) * 1.1F, 0, 0);
            }
            NoteDisplayManager.RegisterPosition(startPos, endPos);
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
            if(bgaConfigs != null)
                foreach(var bgaCfg in bgaConfigs) {
                    var renderer = bgaCfg.renderer;
                    var material = renderer.sharedMaterial;
                    renderer.material = Instantiate(material);
                    instaniatedBGADisplays.Add(new BGADisplay(player, renderer, bgaCfg.channel));
                }
            Debug.Log("Start play BMS (sound only)");
            SetTestPositions(loader.Chart.Layout);
            player.Play();
        }

        protected void OnDestroy() {
            if(instaniatedBGADisplays.Count > 0) {
                foreach(var handler in instaniatedBGADisplays)
                    handler.Dispose();
                instaniatedBGADisplays.Clear();
            }
            if(loader != null) {
                loader.Dispose();
                loader.VirtualFS?.Dispose();
                loader = null;
            }
            if(player != null) {
                player.Dispose();
                player = null;
            }
        }

        [Serializable]
        public struct BGAConfig {
            public Renderer renderer;
            public int channel;
        }
    }
}