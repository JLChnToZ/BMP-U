using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BMS {
    public class NoteDetector:MonoBehaviour {
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
        TimingHelper preQueueMapper, postQueueMapper;
        TimeSpan endTimeOffset;

        public TimeSpan EndTimeOffset {
            get { return endTimeOffset; }
        }

        public event Action<TimeSpan, int, int, int> OnNoteClicked;
        public event Action<int> OnLongNoteMissed;
        public bool autoMode;

        void Awake() {
            endTimeOffset = TimeSpan.FromSeconds(-endTimeOffsetSeconds);
            preQueueMapper = new TimingHelper(TimeSpan.FromSeconds(startTimeOffsetSeconds));
            postQueueMapper = new TimingHelper(endTimeOffset);
            preQueueMapper.OnIndexChange += OnHasKeyFrame;
            postQueueMapper.OnIndexChange += OnPostMap;
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
                preQueueMapper.Update(timePosition);
                postQueueMapper.Update(timePosition);
            }
        }

        void OnBMSLoaded() {
            preQueueMapper.ClearHandles();
            postQueueMapper.ClearHandles();
            foreach(var channel in bmsManager.GetAllChannelIds()) {
                if(!bmsManager.GetAllAdoptedChannels().Contains(channel)) continue;
                if(channel >= 50 && bmsManager.LongNoteType != 1) continue;
                GetChannelQueue(channel >= 50 ? channel - 40 : channel).Clear();
                var timeLine = bmsManager.GetTimeLine(channel);
                preQueueMapper.AddTimelineHandle(timeLine, channel);
                postQueueMapper.AddTimelineHandle(timeLine, channel);
            }
        }

        void OnEnded() {
            foreach(var queue in queuedFrames.Values)
                queue.Clear();
            longNoteStates.Clear();
            queuedLongNoteState.Clear();
            longNoteAutoFlag.Clear();
            preQueueMapper.Reset();
            postQueueMapper.Reset();
        }

        void NoteEvent(TimeSpan timePosition, int channel, int dataId) {
            if(autoMode) {
                if(!bmsManager.GetAllAdoptedChannels().Contains(channel))
                    return;
                bool isUp = false;
                if(channel >= 50) {
                    longNoteAutoFlag.TryGetValue(channel, out isUp);
                    longNoteAutoFlag[channel] = !isUp;
                    channel -= 40;
                }
                HandleNoteEvent(channel, true, !isUp);
            }
        }

        void OnHasKeyFrame(TimeSpan timePosition, int channel, int dataId) {
            bool isLongNote = channel >= 50, lnDown = false;
            TimeSpan lnEndpos = TimeSpan.Zero;
            QueuedLongNoteState queuedLNState = new QueuedLongNoteState();
            if(isLongNote) {
                channel -= 40;
                queuedLongNoteState.TryGetValue(channel, out queuedLNState);
                lnDown = queuedLNState.isNoteDown;
                queuedLNState.isNoteDown = !lnDown;
                if(!lnDown) {
                    queuedLNState.longNoteId++;
                    lnEndpos = preQueueMapper.Peek(channel + 40, 2).TimePosition;
                }
                queuedLongNoteState[channel] = queuedLNState;
            }
            GetChannelQueue(channel).Enqueue(new KeyFrame {
                timePosition = timePosition,
                channelId = channel,
                dataId = dataId,
                type = isLongNote ? (lnDown ? NoteType.LongEnd : NoteType.LongStart) : NoteType.Normal,
                longNoteId = queuedLNState.longNoteId,
                lnEndPosition = lnEndpos
            });
        }

        void OnPostMap(TimeSpan timePosition, int channel, int dataId) {
            if(channel >= 50) channel -= 40;
            HandleNoteEvent(channel, false, false);
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
            TimeSpan offsetPosition = bmsManager.TimePosition + endTimeOffset;
            KeyFrame keyFrame;
            int flag = -1;
            while(ch.Count > 0) {
                keyFrame = ch.Peek();
                if(keyFrame.timePosition > offsetPosition) break;
                ch.Dequeue();
                if(keyFrame.type != NoteType.LongEnd || !lns.isMissed)
                    flag = bmsManager.NoteClicked(keyFrame.timePosition, channel, keyFrame.dataId, true);
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
                        flag = bmsManager.NoteClicked(keyFrame.timePosition, channel, keyFrame.dataId, false, hasSound, endNote);
                        if(OnNoteClicked != null)
                            OnNoteClicked.Invoke(keyFrame.timePosition, channel, keyFrame.dataId, flag);
                        lns.isMissed = flag < 0;
                        if(flag < 0) lns.longNoteId = -1;
                        longNoteStates[channel] = lns;
                    }
                } else if(lns.isDown) {
                    lns.isDown = false;
                    lns.isMissed = true;
                    bmsManager.NoteClicked(TimeSpan.Zero, channel, 0, true, false, null);
                    if(OnLongNoteMissed != null)
                        OnLongNoteMissed.Invoke(channel);
                    longNoteStates[channel] = lns;
                }
            }
        }
    }
}
