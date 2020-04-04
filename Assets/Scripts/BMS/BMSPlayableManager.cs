using System;
using System.Collections.Generic;
using BananaBeats.Utils;
using BananaBeats.Visualization;
using BananaBeats.Layouts;
using BananaBeats.Inputs;
using BMS;
using UniRx.Async;

namespace BananaBeats {
    public class BMSPlayableManager: BMSPlayer {
        private struct NoteData {
            public BMSEvent bmsEvent;
            public int Channel => (bmsEvent.data1 - 10) % 20;
            public int id;
            public NoteType noteType;
            public bool isMissed;
        }

        public static BMSPlayableManager Instance { get; private set; }

        public static BMSPlayableManager Load(BMSLoader loader) {
            if(Instance != null) {
                if(Instance.BMSLoader == loader) {
                    Instance.Reload();
                    return Instance;
                }
                Instance.Dispose();
            }
            return Instance = new BMSPlayableManager(loader);
        }

        private static ScoreConfig scoreConfig;
        public static ScoreConfig ScoreConfig {
            get { return scoreConfig; }
            set {
                scoreConfig = value;
                Instance?.InitScoreCalculator();
            }
        }

        private static EventHandler<ScoreEventArgs> onScoreEvent;
        public static event EventHandler<ScoreEventArgs> OnScore {
            add {
                onScoreEvent += value;
                if(Instance?.scoreCalculator != null)
                    Instance.scoreCalculator.OnScore += value;
            }
            remove {
                onScoreEvent -= value;
                if(Instance?.scoreCalculator != null)
                    Instance.scoreCalculator.OnScore -= value;
            }
        }

        public static event EventHandler GlobalPlayStateChanged;

        public static event BMSEventDelegate GlobalBMSEvent;

        public static event BMSEventDelegate GlobalPreBMSEvent;

        public static event Action<int> OnHitNote;

        public BMSKeyLayout PlayableLayout { get; set; }

        public TimeSpan PreOffset { get; set; } = TimeSpan.FromSeconds(3);

        public TimeSpan TimingOffset { get; set; }

        public float NoteSpeed { get; set; } = 1;

        public bool EnableNoteSpeedAdjustment { get; set; } = true;

        public float DetunePerSeconds { get; set; } = 1;

        public bool AutoTriggerLongNoteEnd { get; set; } = true;

        public int DetuneRank { get; set; } = 2;

        public int Score => scoreCalculator != null ?
            scoreCalculator.Score : 0;

        public int Combos => scoreCalculator != null ?
            scoreCalculator.Combos : 0;

        private readonly Dictionary<int, int> longNoteSound = new Dictionary<int, int>();
        private readonly Dictionary<int, int> longNoteIds = new Dictionary<int, int>();
        private readonly Dictionary<int, Queue<NoteData>> noteQueues = new Dictionary<int, Queue<NoteData>>();
        private readonly HashSet<int> missedLongNotes = new HashSet<int>();
        private readonly Queue<NoteData> deferHitNoteBuffer = new Queue<NoteData>();
        private IDisposable deferHitNote;
        private ScoreCalculator scoreCalculator;

        public BMSTimingHelper PreTimingHelper { get; }

        protected BMSPlayableManager(BMSLoader bmsLoader) : base(bmsLoader) {
            PreTimingHelper = new BMSTimingHelper(timingHelper.Chart);
            PreTimingHelper.EventDispatcher.BMSEvent += OnPreBMSEvent;
            PlayableLayout = timingHelper.Chart.Layout;
            BMSEvent += BroadcastStaticBMSEvent;
            PlaybackStateChanged += BroadcastPlayStateChanged;
        }

        public void ApplyConfig(BMSGameConfig config) {
            PlayableLayout = config.autoPlay ?
                BMSKeyLayout.None :
                (config.playableChannels & Chart.Layout);
            EnableNoteSpeedAdjustment = config.bpmAffectSpeed;
            DetunePerSeconds = config.detune;
            Volume = config.volume;
            NoteSpeed = config.speed;
            TimingOffset = TimeSpanAccurate.FromSecond(config.offset);
        }

        public void Reload() {
            if(Disposed) return;
            Reset();
            if(scoreCalculator == null)
                InitScoreCalculator();
            else
                scoreCalculator.Reload();
        }

        private void InitScoreCalculator() {
            if(Disposed || scoreConfig.timingConfigs == null) {
                scoreCalculator = null;
                return;
            }
            scoreCalculator = new ScoreCalculator(scoreConfig, Chart.MaxCombos);
            if(onScoreEvent != null)
                scoreCalculator.OnScore += onScoreEvent;
        }

        public override void Play() {
            if(Disposed) return;
            var layout = BMSLoader.Chart.Layout;
            NoteLayoutManager.SwitchLayout(layout);
            InputManager.SwitchBindingLayout(layout);
            if(deferHitNote == null)
                deferHitNote = GameLoop.RunAsUpdate(DeferHitNote, PlayerLoopTiming.PreLateUpdate);
            InputManager.Inputs.Enable();
            base.Play();
        }

        public override void Pause() {
            InputManager.Inputs.Disable();
            base.Pause();
        }

        protected override async UniTask Update(TimeSpan delta) {
            if(PlaybackState == PlaybackState.Playing) {
                var pos1 = timingHelper.CurrentPosition;
                var pos2 = timingHelper.StopResumePosition;
                NoteDisplayManager.ScrollSpeed = NoteSpeed;
                NoteDisplayManager.ScrollPos = (!EnableNoteSpeedAdjustment || pos1 > pos2 ? pos1 : pos2).ToAccurateSecondF();
                ReportBeatFlow();
            }
            await base.Update(delta);
            if(PlaybackState == PlaybackState.Playing) {
                PreTimingHelper.CurrentPosition = timingHelper.CurrentPosition + PreOffset;
                CheckNoteStatus();
            }
        }

        private void DeferHitNote() {
            while(deferHitNoteBuffer.Count > 0) {
                try {
                    var noteData = deferHitNoteBuffer.Dequeue();
                    switch(noteData.noteType) {
                        case NoteType.Normal:
                        case NoteType.Fake:
                            NoteDisplayManager.HitNote(noteData.id, false);
                            NoteDisplayManager.Destroy(noteData.id);
                            break;
                        case NoteType.LongStart:
                            NoteDisplayManager.HitNote(noteData.id, false);
                            if(noteData.isMissed)
                                NoteDisplayManager.Destroy(noteData.id);
                            break;
                        case NoteType.LongEnd:
                            NoteDisplayManager.HitNote(noteData.id, true);
                            NoteDisplayManager.Destroy(noteData.id);
                            break;
                    }
                    // TODO: Missed Note Handling
                } catch(Exception ex) {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        public override void Reset() {
            InputManager.Inputs.Disable();
            base.Reset();
            PreTimingHelper?.Reset();
            if(scoreCalculator == null)
                InitScoreCalculator();
            else if(scoreCalculator.MaxNotes != Chart.MaxCombos)
                scoreCalculator.MaxNotes = Chart.MaxCombos;
            else
                scoreCalculator.Reset();
            noteQueues.Clear();
            missedLongNotes.Clear();
            if(deferHitNote != null) {
                deferHitNote.Dispose();
                deferHitNote = null;
            }
            NoteDisplayManager.Clear();
        }

        protected override object OnNoteEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                InternalHitNote(bmsEvent.data1);
                return WavEvent(bmsEvent, ignoreType: true);
            }
            return null;
        }

        protected override object OnLongNoteStartEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                longNoteSound[bmsEvent.data1] = (int)bmsEvent.data2;
                InternalHitNote(bmsEvent.data1);
                return WavEvent(bmsEvent, ignoreType: true);
            }
            return null;
        }

        protected override object OnLongNoteEndEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                InternalHitNote(bmsEvent.data1);
                if(!longNoteSound.TryGetValue(bmsEvent.data1, out int lnStartId) || lnStartId != (int)bmsEvent.data2)
                    return WavEvent(bmsEvent, ignoreType: true);
            }
            return null;
        }

        private void OnPreBMSEvent(BMSEvent bmsEvent) {
            int channel = (bmsEvent.data1 - 10) % 20;
            var bpmScale = EnableNoteSpeedAdjustment ? PreTimingHelper.BPM / 135F : 1;
            switch(bmsEvent.type) {
                case BMSEventType.Note: {
                    int id = NoteDisplayManager.Spawn(channel, bmsEvent.time, NoteType.Normal, bpmScale);
                    noteQueues.GetOrConstruct(channel).Enqueue(new NoteData {
                        id = id,
                        bmsEvent = bmsEvent,
                        noteType = NoteType.Normal,
                    });
                    break;
                }
                case BMSEventType.LongNoteStart: {
                    int id = NoteDisplayManager.Spawn(channel, bmsEvent.time, NoteType.LongStart, bpmScale);
                    longNoteIds[channel] = id;
                    noteQueues.GetOrConstruct(channel).Enqueue(new NoteData {
                        id = id,
                        bmsEvent = bmsEvent,
                        noteType = NoteType.LongStart,
                    });
                    break;
                }
                case BMSEventType.LongNoteEnd: {
                    if(longNoteIds.TryGetValue(channel, out int id)) {
                        NoteDisplayManager.SetEndNoteTime(id, bmsEvent.time, bpmScale);
                        noteQueues.GetOrConstruct(channel).Enqueue(new NoteData {
                            id = id,
                            bmsEvent = bmsEvent,
                            noteType = NoteType.LongEnd,
                        });
                    } else
                        UnityEngine.Debug.LogWarning($"Unknown long note end channel {channel} ({bmsEvent.data1})");
                    break;
                }
                case BMSEventType.Unknown: {
                    if(bmsEvent.data1 > 30 && bmsEvent.data1 < 50) {
                        int id = NoteDisplayManager.Spawn(channel, bmsEvent.time, NoteType.Fake, bpmScale);
                        noteQueues.GetOrConstruct(channel).Enqueue(new NoteData {
                            id = id,
                            bmsEvent = bmsEvent,
                            noteType = NoteType.Fake,
                        });
                    }
                    break;
                }
            }
            GlobalPreBMSEvent?.Invoke(bmsEvent, null);
        }

        public void HitNote(int channel, bool isHolding) {
            if(Disposed || !IsChannelPlayable(channel) ||
                scoreCalculator == null ||
                !noteQueues.TryGetValue(channel - 10, out var queue) ||
                queue.Count <= 0)
                return;
            var noteData = queue.Peek();
            var timeDiff = noteData.bmsEvent.time - timingHelper.CurrentPosition;
            switch(noteData.noteType) {
                case NoteType.Normal:
                case NoteType.LongStart:
                    if(isHolding) InternalHitNote(channel, timeDiff);
                    break;
                case NoteType.LongEnd:
                    if(!isHolding) InternalHitNote(channel, timeDiff, false);
                    break;
            }
        }

        private void InternalHitNote(int channel, int hittedState = 0) {
            channel = (channel - 10) % 20;
            var noteData = noteQueues.GetOrConstruct(channel).Dequeue();
            noteData.isMissed = hittedState < 0;
            deferHitNoteBuffer.Enqueue(noteData);
            if(noteData.isMissed && noteData.noteType == NoteType.LongStart)
                missedLongNotes.Add(channel);
            OnHitNote?.Invoke(channel);
            if(noteData.isMissed || (
                noteData.noteType == NoteType.LongEnd &&
                longNoteSound.TryGetValue(noteData.bmsEvent.data1, out int lnStartId) &&
                lnStartId == (int)noteData.bmsEvent.data2))
                return;
            WavEvent(
                noteData.bmsEvent,
                DetunePerSeconds != 0 || hittedState < DetuneRank ? 1 :
                (timingHelper.CurrentPosition - noteData.bmsEvent.time).ToAccurateSecondF() * DetunePerSeconds + 1,
                true
            );
        }

        private void InternalHitNote(int channel, TimeSpan timeDiff, bool safeRange = true) =>
            InternalHitNote(channel, scoreCalculator.HitNote(timeDiff + TimingOffset, safeRange: safeRange));

        private void CheckNoteStatus() {
            if(scoreCalculator == null) return;
            foreach(var queue in noteQueues.Values) {
                if(queue == null) continue;
                while(queue.Count > 0) {
                    var noteData = queue.Peek();
                    var channel = noteData.Channel;
                    if(noteData.noteType != NoteType.LongEnd || !missedLongNotes.Remove(channel)) {
                        var timeDiff = noteData.bmsEvent.time - timingHelper.CurrentPosition + TimingOffset;
                        if(scoreCalculator.HitNote(timeDiff, true) >= 0) {
                            if(AutoTriggerLongNoteEnd &&
                                noteData.noteType == NoteType.LongEnd &&
                                timeDiff <= TimeSpan.Zero)
                                InternalHitNote(channel + 10, timeDiff - TimingOffset);
                            break;
                        }
                    }
                    InternalHitNote(channel + 10, -1);
                }
            }
        }

        private static void BroadcastStaticBMSEvent(BMSEvent bmsEvent, object res) =>
            GlobalBMSEvent?.Invoke(bmsEvent, res);

        private static void BroadcastPlayStateChanged(object sender, EventArgs e) =>
            GlobalPlayStateChanged?.Invoke(sender, e);

        private void ReportBeatFlow() {
            var beatFlow = timingHelper.BeatFlow;
            var timeSignature = timingHelper.TimeSignature;
            NoteLaneManager.SetBeatFlowEffect((1 - beatFlow % timeSignature / timeSignature) * (1 - beatFlow % 1F));
        }

        public bool IsChannelPlayable(int channelId) =>
            PlayableLayout.HasChannel(channelId);

        public override void Dispose() {
            base.Dispose();
            deferHitNote?.Dispose();
            if(Instance == this)
                Instance = null;
        }
    }
}
