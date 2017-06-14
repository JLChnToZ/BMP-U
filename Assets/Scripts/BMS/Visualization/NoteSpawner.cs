using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BMS.Visualization {
    public enum ColoringMode {
        None,
        Timing,
        Channel,
        Beat,
    }

    public class NoteSpawner: MonoBehaviour {
        public GameObject notePrefab;
        public Queue<NoteHandler> noteHandlers = new Queue<NoteHandler>();

        HashSet<NoteHandler> spawnedNoteHandlers = new HashSet<NoteHandler>();

        private Queue<BMSEvent> holdedEvents = new Queue<BMSEvent>();

        public BMSManager bmsManager;
        [NonSerialized]
        protected bool bmsLoadedCalled;
        [NonSerialized]
        protected int[] channels = new int[0];
        NoteHandler[] longNoteHandlers = new NoteHandler[0];

        [SerializeField]
        Color defaultColor = Color.white;
        [SerializeField]
        Color[] matchColors = new Color[0];
        public ColoringMode coloringMode;
        [NonSerialized]
        bool hasColors;
        static int currentColor;
        static TimeSpan currentMatchingTime = TimeSpan.Zero;
        readonly static HashSet<NoteHandler> matchingTimeNoteHandlers = new HashSet<NoteHandler>();
        protected int[] handledChannels = new[] { 11, 12, 13, 14, 15, 18, 19, 17, 21, 22, 23, 24, 25, 28, 29, 27 };
        HashSet<int> _handledChannels = new HashSet<int>();

        public NoteDetector noteDetector;

        public IList<int> MappedChannels {
            get { return new ReadOnlyCollection<int>(channels); }
        }

        public bool RequireRecycleImmediately {
            get { return holdedEvents.Count > 0 && holdedEvents.Peek().time >= bmsManager.TimePosition + bmsManager.PreEventOffset; }
        }

        protected virtual void Start() {
            hasColors = matchColors.Length > 0;
            _handledChannels.UnionWith(handledChannels);
            foreach(var channel in _handledChannels)
                bmsManager.AdoptChannel(channel);
            bmsManager.OnPreNoteEvent += PreNoteEvent;
            bmsManager.OnGameStarted += OnGameStarted;
            bmsManager.OnBMSLoaded += BMSLoaded;
        }

        protected virtual void OnDestroy() {
            if(bmsManager != null) {
                bmsManager.OnPreNoteEvent -= PreNoteEvent;
                bmsManager.OnGameStarted -= OnGameStarted;
                bmsManager.OnBMSLoaded -= BMSLoaded;
            }
        }

        void BMSLoaded() {
            bmsLoadedCalled = true;
        }

        protected virtual void OnGameStarted() {
            currentColor = -1;
            currentMatchingTime = TimeSpan.Zero;
            foreach(var handler in spawnedNoteHandlers) {
                Destroy(handler.gameObject);
            }
            holdedEvents.Clear();
            spawnedNoteHandlers.Clear();
            noteHandlers.Clear();
            matchingTimeNoteHandlers.Clear();
            ResetBPMEvents();
        }

        protected virtual void PreNoteEvent(BMSEvent bmsEvent) {
            switch(bmsEvent.type) {
                case BMSEventType.Note:
                case BMSEventType.LongNoteStart:
                case BMSEventType.LongNoteEnd:
                    break;
                case BMSEventType.BeatReset:
                case BMSEventType.BPM:
                case BMSEventType.STOP:
                    UpdateBPMEvents(bmsEvent);
                    return;
                default: return;
            }
            if(bmsManager.NoteLimit > 0 && spawnedNoteHandlers.Count >= bmsManager.NoteLimit && noteHandlers.Count <= 0)
                holdedEvents.Enqueue(bmsEvent);
            else {
                SpawnHoldedNotes();
                HandleSpawn(bmsEvent);
            }
        }

        private void HandleSpawn(BMSEvent bmsEvent) {
            bool isLongNote = bmsManager.LongNoteType > 0 && _handledChannels.Contains(bmsEvent.data1) && (bmsEvent.type == BMSEventType.LongNoteStart || 
                bmsEvent.type == BMSEventType.LongNoteEnd);
            if(!_handledChannels.Contains(bmsEvent.data1)) return;
            int index = Array.IndexOf(channels, bmsEvent.data1);
            if(index < 0) return;
            float delta = (float)index / (channels.Length - 1);
            bool createNew = true;
            NoteHandler noteHandler = null;
            if(isLongNote) {
                int idx = Array.IndexOf(channels, bmsEvent.data1);
                noteHandler = longNoteHandlers[idx];
                createNew = noteHandler == null;
                if(createNew)
                    longNoteHandlers[idx] = noteHandler = GetFreeNoteHandler();
                else
                    longNoteHandlers[idx] = null;
            } else
                noteHandler = GetFreeNoteHandler();

            if(createNew)
                noteHandler.Register(this, bmsManager, bmsEvent.time, bmsEvent.data1, (int)bmsEvent.data2, delta, isLongNote);
            else
                noteHandler.RegisterLongNoteEnd(bmsEvent.time, (int)bmsEvent.data2);

            if(isLongNote ? createNew : true) {
                switch(coloringMode) {
                    case ColoringMode.Timing:
                        noteHandler.SetColor(defaultColor);
                        if(hasColors) {
                            if(currentMatchingTime != bmsEvent.time)
                                matchingTimeNoteHandlers.Clear();
                            currentMatchingTime = bmsEvent.time;
                            matchingTimeNoteHandlers.Add(noteHandler);
                            if(matchingTimeNoteHandlers.Count > 1) {
                                if(matchingTimeNoteHandlers.Count == 2) {
                                    currentColor++;
                                    foreach(var nh in matchingTimeNoteHandlers)
                                        nh.SetMatchColor();
                                }
                                noteHandler.SetMatchColor();
                            }
                        }
                        break;
                    case ColoringMode.Channel:
                        if(hasColors) {
                            int colorId = Array.IndexOf(handledChannels, bmsEvent.data1);
                            noteHandler.SetColor(matchColors[colorId > 0 ? colorId % matchColors.Length : 0]);
                        }
                        break;
                    case ColoringMode.Beat:
                        double currentBeat = ((bmsEvent.time - bpmBasePoint).ToAccurateMinute() * bpm + bpmBasePointBeatFlow) % timeSign;
                        noteHandler.SetColor(HelperFunctions.ColorFromHSL(Mathf.Log(HelperFunctions.FindDivision(currentBeat), 2) / 9, 1, 0.55F));
                        break;
                    default:
                        noteHandler.SetColor(defaultColor);
                        break;
                }
            }
#if UNITY_EDITOR || DEBUG
            noteHandler.gameObject.name = string.Format("NOTE #{0:0000}:{1:0000} @{2}", bmsEvent.data1, bmsEvent.data2, bmsEvent.time);
#endif
        }

        #region BPM Events
        double bpmBasePointBeatFlow, timeSign, bpm;
        TimeSpan bpmBasePoint;

        private void ResetBPMEvents() {
            bpmBasePointBeatFlow = 0;
            timeSign = 4;
            bpm = bmsManager.LoadedChart.BPM;
            bpmBasePoint = TimeSpan.Zero;
        }

        private void UpdateBPMEvents(BMSEvent bmsEvent) {
            // No need to handle this unless it is beat coloring mode
            if(coloringMode != ColoringMode.Beat) return;
            switch(bmsEvent.type) {
                case BMSEventType.BeatReset:
                    bpmBasePointBeatFlow = 0;
                    bpmBasePoint = bmsEvent.time;
                    timeSign = bmsEvent.Data2F;
                    break;
                case BMSEventType.BPM:
                    double newBpm = bmsEvent.Data2F;
                    bpmBasePointBeatFlow += (bmsEvent.time - bpmBasePoint).ToAccurateMinute() * bpm;
                    bpmBasePoint = bmsEvent.time;
                    bpm = newBpm;
                    break;
                case BMSEventType.STOP:
                    bpmBasePoint -= new TimeSpan(bmsEvent.data2);
                    break;
            }
        }
        #endregion

        public Color CurrentMatchColor {
            get { return currentColor < 0 ? defaultColor : matchColors[currentColor % matchColors.Length]; }
        }

        protected virtual void LateUpdate() {
            if(bmsLoadedCalled) {
                bmsLoadedCalled = false;
                var _channels = new List<int>(handledChannels);
                var _shouldRemoveChannels = new HashSet<int>(_handledChannels);
                foreach(var channel in bmsManager.GetAllChannelIds())
                    _shouldRemoveChannels.Remove(channel - (channel >= 50 ? 40 : 0));
                _channels.RemoveAll(_shouldRemoveChannels.Contains);
                channels = _channels.ToArray();
                longNoteHandlers = new NoteHandler[channels.Length];
            }
        }

        protected virtual NoteHandler GetFreeNoteHandler() {
            NoteHandler result = null;
            while(noteHandlers.Count > 0 && (result == null || !result.IsIdle))
                result = noteHandlers.Dequeue();
            if(result == null) {
                var cloned = Instantiate(notePrefab);
                cloned.transform.SetParent(transform, false);
                result = cloned.GetComponent<NoteHandler>();
                spawnedNoteHandlers.Add(result);
            }
            result.transform.SetAsFirstSibling();
            return result;
        }

        public virtual void RecycleNote(NoteHandler note) {
            if(note != null)
                noteHandlers.Enqueue(note);
            SpawnHoldedNotes();
        }

        private void SpawnHoldedNotes() {
            while(noteHandlers.Count > 0 && holdedEvents.Count > 0)
                HandleSpawn(holdedEvents.Dequeue());
        }
    }
}
