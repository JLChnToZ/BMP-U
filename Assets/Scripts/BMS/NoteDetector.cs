using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BMS {
    public class NoteDetector : MonoBehaviour {
        enum NoteType {
            Normal,
            LongStart,
            LongEnd
        }
        struct KeyFrame {
            public TimeSpan timePosition;
            public int channelId;
            public int dataId;
            public NoteType type;
            public int longNoteId;
            public TimeSpan lnEndPosition;
            public TimeSpan sliceStart;
            public TimeSpan sliceEnd;
        }
        struct QueuedLongNoteState {
            public bool isNoteDown;
            public int longNoteId;
        }
        struct LongNoteState {
            public bool isDown;
            public bool isMissed;
            public int longNoteId;
            public int longNoteDataId;
        }
        [SerializeField]
        BMSManager bmsManager;
        [SerializeField]
        float startTimeOffsetSeconds;
        [SerializeField]
        float endTimeOffsetSeconds;
        
        readonly Dictionary<int, Queue<KeyFrame>> queuedFrames = new Dictionary<int, Queue<KeyFrame>>();
        readonly Dictionary<int, QueuedLongNoteState> queuedLongNoteState = new Dictionary<int, QueuedLongNoteState>();
        readonly Dictionary<int, LongNoteState> longNoteStates = new Dictionary<int, LongNoteState>();
        readonly Dictionary<int, bool> longNoteAutoFlag = new Dictionary<int, bool>();
        Chart.EventDispatcher preQueueMapper, postQueueMapper;
        IList<BMSEvent> bmsEvents;
        TimeSpan startTimeOffset, endTimeOffset;

        public TimeSpan EndTimeOffset {
            get { return endTimeOffset; }
        }

        public event Action<TimeSpan, int, int, int> OnNoteClicked;
        public event Action<int> OnLongNoteMissed;
        public bool autoMode;

        void Awake() {
            startTimeOffset = TimeSpan.FromSeconds(startTimeOffsetSeconds);
            endTimeOffset = TimeSpan.FromSeconds(-endTimeOffsetSeconds);
            bmsManager.OnBMSLoaded += OnBMSLoaded;
            bmsManager.OnGameEnded += OnEnded;
            bmsManager.OnNoteEvent += NoteEvent;
        }

        void OnDestroy() {
            if(bmsManager != null) {
                bmsManager.OnBMSLoaded -= OnBMSLoaded;
                bmsManager.OnGameEnded -= OnEnded;
                bmsManager.OnNoteEvent -= NoteEvent;
            }
        }

        void Update() {
            if(bmsManager.IsStarted && !bmsManager.IsPaused) {
                TimeSpan timePosition = bmsManager.TimePosition;
                preQueueMapper.Seek(timePosition + startTimeOffset);
                postQueueMapper.Seek(timePosition + endTimeOffset);
                if(Input.GetAxis("Cancel") > 0)
                    bmsManager.IsPaused = true;
            }
        }

        void OnApplicationFocus(bool state) {
            if(!autoMode && bmsManager != null && bmsManager.IsStarted)
                bmsManager.IsPaused = true;
        }

        void OnApplicationPause(bool state) {
            if(bmsManager != null && bmsManager.IsStarted)
                bmsManager.IsPaused = true;
        }

        void OnBMSLoaded() {
            bmsEvents = bmsManager.LoadedChart.Events;
            if(preQueueMapper != null)
                preQueueMapper.BMSEvent -= OnHasKeyFrame;
            preQueueMapper = bmsManager.LoadedChart.GetEventDispatcher();
            preQueueMapper.BMSEvent += OnHasKeyFrame;
            preQueueMapper.Seek(TimeSpan.MinValue, false);
            if(postQueueMapper != null)
                postQueueMapper.BMSEvent -= OnPostMap;
            postQueueMapper = bmsManager.LoadedChart.GetEventDispatcher();
            postQueueMapper.BMSEvent += OnPostMap;
            postQueueMapper.Seek(TimeSpan.MinValue, false);
            foreach(var channel in bmsManager.GetAllChannelIds()) {
                if(!bmsManager.GetAllAdoptedChannels().Contains(channel)) continue;
                GetChannelQueue(channel).Clear();
            }
        }

        void OnEnded() {
            foreach(var queue in queuedFrames.Values)
                queue.Clear();
            longNoteStates.Clear();
            queuedLongNoteState.Clear();
            longNoteAutoFlag.Clear();
            preQueueMapper.Seek(TimeSpan.MinValue, false);
            postQueueMapper.Seek(TimeSpan.MinValue, false);
        }

        void NoteEvent(BMSEvent bmsEvent) {
            if(autoMode) {
                if(!bmsManager.GetAllAdoptedChannels().Contains(bmsEvent.data1))
                    return;
                HandleNoteEvent(bmsEvent.data1, true, bmsEvent.type == BMSEventType.LongNoteStart || bmsEvent.type == BMSEventType.Note);
            }
        }

        void OnHasKeyFrame(BMSEvent bmsEvent) {
            if(!bmsEvent.IsNote || !bmsManager.GetAllAdoptedChannels().Contains(bmsEvent.data1))
                return;
            bool isLongNote = bmsEvent.type == BMSEventType.LongNoteEnd || bmsEvent.type == BMSEventType.LongNoteStart, lnDown = false;
            TimeSpan lnEndpos = TimeSpan.Zero;
            QueuedLongNoteState queuedLNState = new QueuedLongNoteState();
            if(isLongNote) {
                queuedLongNoteState.TryGetValue(bmsEvent.data1, out queuedLNState);
                lnDown = queuedLNState.isNoteDown;
                queuedLNState.isNoteDown = !lnDown;
                if(!lnDown) {
                    queuedLNState.longNoteId++;
                    lnEndpos = bmsEvent.time2;
                }
                queuedLongNoteState[bmsEvent.data1] = queuedLNState;
            }
            GetChannelQueue(bmsEvent.data1).Enqueue(new KeyFrame {
                timePosition = bmsEvent.time,
                channelId = bmsEvent.data1,
                dataId = (int)bmsEvent.data2,
                type = isLongNote ? (lnDown ? NoteType.LongEnd : NoteType.LongStart) : NoteType.Normal,
                longNoteId = queuedLNState.longNoteId,
                lnEndPosition = lnEndpos,
                sliceStart = bmsEvent.sliceStart,
                sliceEnd = bmsEvent.sliceEnd
            });
        }

        void OnPostMap(BMSEvent bmsEvent) {
            if(!bmsEvent.IsNote || !bmsManager.GetAllAdoptedChannels().Contains(bmsEvent.data1))
                return;
            HandleNoteEvent(bmsEvent.data1, false, false);
        }

        Queue<KeyFrame> GetChannelQueue(int channelId) {
            Queue<KeyFrame> result;
            if(!queuedFrames.TryGetValue(channelId, out result))
                queuedFrames[channelId] = result = new Queue<KeyFrame>();
            return result;
        }

        LongNoteState GetLongNoteState(int channelId) {
            LongNoteState result;
            if(!longNoteStates.TryGetValue(channelId, out result))
                longNoteStates[channelId] = result = new LongNoteState();
            return result;
        }

        public void OnClick(int channel, bool isDown) {
            if(autoMode || !bmsManager.GetAllAdoptedChannels().Contains(channel)) return;
            if(channel >= 50) channel -= 40;
            HandleNoteEvent(channel, true, isDown);
        }

        void HandleNoteEvent(int channel, bool isClicking, bool isDown) {
            var ch = GetChannelQueue(channel);
            var lns = GetLongNoteState(channel);
            TimeSpan offsetPosition = bmsManager.RealTimePosition + endTimeOffset;
            KeyFrame keyFrame;
            int flag = -1;
            while(ch.Count > 0) {
                keyFrame = ch.Peek();
                if(keyFrame.timePosition > offsetPosition) break;
                ch.Dequeue();
                if(keyFrame.type != NoteType.LongEnd || !lns.isMissed)
                    flag = bmsManager.NoteClicked(keyFrame.timePosition, channel, keyFrame.dataId, true, keyFrame.sliceStart, keyFrame.sliceEnd);
                else
                    flag = -1;
                if(OnNoteClicked != null)
                    OnNoteClicked.Invoke(keyFrame.timePosition, channel, keyFrame.dataId, flag);
            }
            if(isClicking) {
                if(ch.Count > 0) {
                    keyFrame = ch.Peek();
                    bool handle = true, skip = false, hasSound = true;
                    TimeSpan? endNote = null;
                    switch(keyFrame.type) {
                        case NoteType.Normal:
                            if(!isDown) handle = false;
                            lns.longNoteId = -1;
                            break;
                        case NoteType.LongStart:
                            if(!isDown) handle = false;
                            lns.longNoteId = keyFrame.longNoteId;
                            lns.longNoteDataId = keyFrame.dataId;
                            endNote = keyFrame.lnEndPosition;
                            break;
                        case NoteType.LongEnd:
                            if(isDown) handle = false;
                            if(lns.longNoteId != keyFrame.longNoteId) skip = true;
                            lns.longNoteId = -1;
                            hasSound = lns.longNoteDataId != keyFrame.dataId;
                            break;
                    }
                    lns.isDown = keyFrame.type == NoteType.Normal ? false : isDown;
                    longNoteStates[channel] = lns;
                    if(handle || skip) ch.Dequeue();
                    if(handle) {
                        flag = bmsManager.NoteClicked(keyFrame.timePosition, channel, keyFrame.dataId, false, keyFrame.sliceStart, keyFrame.sliceEnd, hasSound, endNote);
                        if(OnNoteClicked != null)
                            OnNoteClicked.Invoke(keyFrame.timePosition, channel, keyFrame.dataId, flag);
                        lns.isMissed = flag < 0;
                        if(flag < 0) lns.longNoteId = -1;
                        longNoteStates[channel] = lns;
                    }
                } else if(lns.isDown) {
                    lns.isDown = false;
                    lns.isMissed = true;
                    bmsManager.NoteClicked(TimeSpan.Zero, channel, 0, true, TimeSpan.Zero, TimeSpan.MaxValue, false, null);
                    if(OnLongNoteMissed != null)
                        OnLongNoteMissed.Invoke(channel);
                    longNoteStates[channel] = lns;
                }
            }
        }
    }
}
