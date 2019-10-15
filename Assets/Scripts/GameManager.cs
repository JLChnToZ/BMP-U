using System;
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

        public GameObject notePrefab;

        public BGAConfig[] bgaConfigs;

        private readonly HashSet<BGADisplay> instaniatedBGADisplays = new HashSet<BGADisplay>();

        private readonly Dictionary<int, int> longNoteIds = new Dictionary<int, int>();
        private readonly Dictionary<int, Queue<TempNoteData>> noteQueues = new Dictionary<int, Queue<TempNoteData>>();

        private struct TempNoteData {
            public int channel;
            public int id;
            public NoteType noteType;
        }

        private enum NoteType {
            Normal,
            LongNoteStart,
            LongNoteEnd,
        }

        // Start is called before the first frame update

        protected void Start() {
            TestLoadBMS().Forget();
            NoteDisplayEntity.ConvertNoteEntity(notePrefab);
            NoteDisplayEntity.ConvertLongNoteEntity(notePrefab);
            SetTestPositions();
        }

        private void SetTestPositions() {
            var startPos = new Vector3[20];
            var endPos = new Vector3[20];
            for(int i = 0; i < 20; i++) {
                startPos[i] = new Vector3(i - 10F, 0, 100);
                endPos[i] = new Vector3(i - 10F, 0, 0);
            }
            NoteDisplayEntity.RegisterPosition(startPos, endPos);
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
            player.PreBMSEvent += OnPreBMS;
            player.OnHitNote += OnHitNote;
            Debug.Log("Start play BMS (sound only)");
            player.Play();
        }

        private void OnPreBMS(BMSEvent bmsEvent, object _) {
            int channel = (bmsEvent.data1 - 10) % 20;
            switch(bmsEvent.type) {
                case BMSEventType.Note: {
                    int id = NoteDisplayEntity.CreateNote(channel, bmsEvent.time, player.PreTimingHelper.BPM / 135F, false);
                    noteQueues.GetOrConstruct(channel, true).Enqueue(new TempNoteData {
                        channel = channel,
                        id = id,
                        noteType = NoteType.Normal,
                    });
                    break;
                }
                case BMSEventType.LongNoteStart: {
                    int id = NoteDisplayEntity.CreateNote(channel, bmsEvent.time, player.PreTimingHelper.BPM / 135F, true);
                    longNoteIds[channel] = id;
                    noteQueues.GetOrConstruct(channel, true).Enqueue(new TempNoteData {
                        channel = channel,
                        id = id,
                        noteType = NoteType.LongNoteStart,
                    });
                    break;
                }
                case BMSEventType.LongNoteEnd: {
                    if(longNoteIds.TryGetValue(channel, out int id)) {
                        NoteDisplayEntity.RegisterLongNoteEnd(id, bmsEvent.time, player.PreTimingHelper.BPM / 135F);
                        noteQueues.GetOrConstruct(channel, true).Enqueue(new TempNoteData {
                            channel = channel,
                            id = id,
                            noteType = NoteType.LongNoteEnd,
                        });
                    }
                    break;
                }
            }
        }

        private void OnHitNote(int channel) {
            channel = (channel - 10) % 20;
            var noteData = noteQueues.GetOrConstruct(channel, true).Dequeue();
            switch(noteData.noteType) {
                case NoteType.Normal:
                    NoteDisplayEntity.HitNote(noteData.id);
                    NoteDisplayEntity.DestroyNote(noteData.id);
                    break;
                case NoteType.LongNoteStart:
                    NoteDisplayEntity.HitNote(noteData.id);
                    break;
                case NoteType.LongNoteEnd:
                    NoteDisplayEntity.HitNote(noteData.id, true);
                    NoteDisplayEntity.DestroyNote(noteData.id);
                    break;
            }
        }

        protected virtual void Update() {
            if(player != null && player.IsPlaying)
                NoteDisplayEntity.SetTime(player.CurrentPosition > player.StopPosition ? player.CurrentPosition : player.StopPosition);
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
                player.PreBMSEvent -= OnPreBMS;
                player.OnHitNote -= OnHitNote;
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