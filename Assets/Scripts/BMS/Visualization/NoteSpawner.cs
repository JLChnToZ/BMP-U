using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BMS.Visualization {
    public enum ColoringMode {
        None,
        Timing,
        Channel,
    }

    public class NoteSpawner: MonoBehaviour {
        public GameObject notePrefab;
        public Queue<NoteHandler> noteHandlers = new Queue<NoteHandler>();

        HashSet<NoteHandler> spawnedNoteHandlers = new HashSet<NoteHandler>();

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

        protected virtual void Start() {
            hasColors = matchColors.Length > 0;
            _handledChannels.UnionWith(handledChannels);
            foreach(var channel in _handledChannels) {
                bmsManager.AdoptChannel(channel);
                bmsManager.AdoptChannel(channel + 40);
            }
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
            spawnedNoteHandlers.Clear();
            noteHandlers.Clear();
            matchingTimeNoteHandlers.Clear();
        }

        protected virtual void PreNoteEvent(TimeSpan timePos, int channelId, int dataId) {
            bool isLongNote = bmsManager.LongNoteType > 0 && _handledChannels.Contains(channelId - 40);
            if(isLongNote) channelId -= 40;
            if(!_handledChannels.Contains(channelId)) return;
            int index = Array.IndexOf(channels, channelId);
            if(index < 0) return;
            float delta = (float)index / (channels.Length - 1);
            bool createNew = true;
            NoteHandler noteHandler = null;
            if(isLongNote) {
                int idx = Array.IndexOf(channels, channelId);
                noteHandler = longNoteHandlers[idx];
                createNew = noteHandler == null;
                if(createNew)
                    longNoteHandlers[idx] = noteHandler = GetFreeNoteHandler();
                else
                    longNoteHandlers[idx] = null;
            } else
                noteHandler = GetFreeNoteHandler();

            if(createNew)
                noteHandler.Register(this, bmsManager, timePos, channelId, dataId, delta, isLongNote);
            else
                noteHandler.RegisterLongNoteEnd(timePos, dataId);

            if(isLongNote ? createNew : true) {
                switch(coloringMode) {
                    case ColoringMode.Timing:
                        noteHandler.SetColor(defaultColor);
                        if(hasColors) {
                            if(currentMatchingTime != timePos)
                                matchingTimeNoteHandlers.Clear();
                            currentMatchingTime = timePos;
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
                            int colorId = Array.IndexOf(handledChannels, channelId);
                            noteHandler.SetColor(matchColors[colorId > 0 ? colorId % matchColors.Length : 0]);
                        }
                        break;
                    default:
                        noteHandler.SetColor(defaultColor);
                        break;
                }
            }
#if UNITY_EDITOR || DEBUG
            noteHandler.gameObject.name = string.Format("NOTE #{0:0000}:{1:0000} @{2}", channelId, dataId, timePos);
#endif
        }
        
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
        }
    }
}
