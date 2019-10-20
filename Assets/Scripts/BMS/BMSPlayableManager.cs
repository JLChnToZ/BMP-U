using System;
using System.Collections.Generic;
using BananaBeats.Utils;
using BananaBeats.Visualization;
using BananaBeats.Layouts;
using BMS;
using UniRx.Async;

namespace BananaBeats {

    public class BMSPlayableManager: BMSPlayer {
        private class DeferHitNote: IPlayerLoopItem, IDisposable {
            private readonly Queue<NoteData> deferHitNoteBuffer = new Queue<NoteData>();
            private bool disposed = false;

            public DeferHitNote() {
                PlayerLoopHelper.AddAction(PlayerLoopTiming.PreLateUpdate, this);
            }

            public void Enqueue(NoteData noteData) {
                deferHitNoteBuffer.Enqueue(noteData);
            }

            public bool MoveNext() {
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
                return !disposed;
            }

            public void Dispose() {
                disposed = true;
            }
        }


        private struct NoteData {
            public BMSEvent bmsEvent;
            public int Channel => (bmsEvent.data1 - 10) % 20;
            public int id;
            public NoteType noteType;
            public bool isMissed;
        }

        public BMSKeyLayout PlayableLayout { get; set; }

        public TimeSpan PreOffset { get; set; } = TimeSpan.FromSeconds(3);

        public bool EnableNoteSpeedAdjustment { get; set; } = true;

        public float DetunePerSeconds { get; set; } = 1;

        public bool AutoTriggerLongNoteEnd { get; set; } = true;

        public int DetuneRank { get; set; } = 2;

        public ScoreConfig ScoreConfig {
            get { return scoreConfig; }
            set {
                scoreConfig = value;
                InitScoreCalculator();
            }
        }

        public int Score => scoreCalculator != null ?
            scoreCalculator.Score : 0;

        public int Combos => scoreCalculator != null ?
            scoreCalculator.Combos : 0;

        private readonly Dictionary<int, int> longNoteSound = new Dictionary<int, int>();
        private readonly Dictionary<int, int> longNoteIds = new Dictionary<int, int>();
        private readonly Dictionary<int, Queue<NoteData>> noteQueues = new Dictionary<int, Queue<NoteData>>();
        private readonly HashSet<int> missedLongNotes = new HashSet<int>();
        private DeferHitNote deferHitNote;
        private ScoreCalculator scoreCalculator;
        private ScoreConfig scoreConfig;
        private EventHandler<ScoreEventArgs> onScoreEvent;

        public event EventHandler<ScoreEventArgs> OnScore {
            add {
                onScoreEvent += value;
                if(scoreCalculator != null)
                    scoreCalculator.OnScore += value;
            }
            remove {
                onScoreEvent -= value;
                if(scoreCalculator != null)
                    scoreCalculator.OnScore -= value;
            }
        }

        public BMSTimingHelper PreTimingHelper { get; }

        public event BMSEventDelegate PreBMSEvent;

        public event Action<int> OnHitNote;

        public BMSPlayableManager(BMSLoader bmsLoader) : base(bmsLoader) {
            PreTimingHelper = new BMSTimingHelper(timingHelper.Chart);
            PreTimingHelper.EventDispatcher.BMSEvent += OnPreBMSEvent;
            PlayableLayout = timingHelper.Chart.Layout;
        }

        private void InitScoreCalculator() {
            if(scoreConfig.timingConfigs == null) {
                scoreCalculator = null;
                return;
            }
            scoreCalculator = new ScoreCalculator(scoreConfig, Chart.MaxCombos);
            if(onScoreEvent != null)
                scoreCalculator.OnScore += onScoreEvent;
        }

        public override void Play() {
            NoteLayoutManager.SetLayout(BMSLoader.Chart.Layout);
            if(deferHitNote == null)
                deferHitNote = new DeferHitNote();
            base.Play();
        }

        protected override async UniTask Update(TimeSpan delta) {
            if(IsPlaying) {
                var pos1 = timingHelper.CurrentPosition;
                var pos2 = timingHelper.StopResumePosition;
                NoteDisplayScroll.time = (!EnableNoteSpeedAdjustment || pos1 > pos2 ? pos1 : pos2).ToAccurateSecondF();
                ReportBeatFlow();
            }
            var task = base.Update(delta);
            if(!task.IsCompleted)
                await task;
            else
                task.GetResult();
            if(IsPlaying) {
                PreTimingHelper.CurrentPosition = timingHelper.CurrentPosition + PreOffset;
                CheckNoteStatus();
            }
        }

        public override void Reset() {
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
                bmsEvent.type = BMSEventType.WAV;
                return base.OnWAVEvent(bmsEvent);
            }
            return base.OnNoteEvent(bmsEvent);
        }

        protected override object OnLongNoteStartEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                longNoteSound[bmsEvent.data1] = (int)bmsEvent.data2;
                InternalHitNote(bmsEvent.data1);
                bmsEvent.type = BMSEventType.WAV;
                return base.OnWAVEvent(bmsEvent);
            }
            return WavEvent(bmsEvent);
        }

        protected override object OnLongNoteEndEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                InternalHitNote(bmsEvent.data1);
                if(!longNoteSound.TryGetValue(bmsEvent.data1, out int lnStartId) || lnStartId != (int)bmsEvent.data2) {
                    bmsEvent.type = BMSEventType.WAV;
                    return base.OnWAVEvent(bmsEvent);
                }
            }
            return WavEvent(bmsEvent);
        }

        protected override object OnUnknownEvent(BMSEvent bmsEvent) {
            if(bmsEvent.data1 > 30 && bmsEvent.data1 < 50)
                InternalHitNote(bmsEvent.data1);
            return WavEvent(bmsEvent);
        }

        private void OnPreBMSEvent(BMSEvent bmsEvent) {
            int channel = (bmsEvent.data1 - 10) % 20;
            var bpmScale = EnableNoteSpeedAdjustment ? PreTimingHelper.BPM / 135F : 1;
            switch(bmsEvent.type) {
                case BMSEventType.Note: {
                    int id = NoteDisplayManager.Spawn(channel, bmsEvent.time, NoteType.Normal, bpmScale);
                    noteQueues.GetOrConstruct(channel, true).Enqueue(new NoteData {
                        id = id,
                        bmsEvent = bmsEvent,
                        noteType = NoteType.Normal,
                    });
                    break;
                }
                case BMSEventType.LongNoteStart: {
                    int id = NoteDisplayManager.Spawn(channel, bmsEvent.time, NoteType.LongStart, bpmScale);
                    longNoteIds[channel] = id;
                    noteQueues.GetOrConstruct(channel, true).Enqueue(new NoteData {
                        id = id,
                        bmsEvent = bmsEvent,
                        noteType = NoteType.LongStart,
                    });
                    break;
                }
                case BMSEventType.LongNoteEnd: {
                    if(longNoteIds.TryGetValue(channel, out int id)) {
                        NoteDisplayManager.SetEndNoteTime(id, bmsEvent.time, bpmScale);
                        noteQueues.GetOrConstruct(channel, true).Enqueue(new NoteData {
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
                        noteQueues.GetOrConstruct(channel, true).Enqueue(new NoteData {
                            id = id,
                            bmsEvent = bmsEvent,
                            noteType = NoteType.Fake,
                        });
                    }
                    break;
                }
            }
            PreBMSEvent?.Invoke(bmsEvent, null);
        }

        public void HitNote(int channel, bool isHolding) {
            if(!IsChannelPlayable(channel) ||
                scoreCalculator == null ||
                !noteQueues.TryGetValue(channel, out var queue) ||
                queue.Count <= 0)
                return;
            var noteData = queue.Peek();
            var timeDiff = timingHelper.CurrentPosition - noteData.bmsEvent.time;
            switch(noteData.noteType) {
                case NoteType.Normal:
                case NoteType.LongStart:
                    if(isHolding) InternalHitNote(channel, timeDiff);
                    break;
                case NoteType.LongEnd:
                    if(!isHolding) InternalHitNote(channel, timeDiff);
                    break;
            }
        }

        private void InternalHitNote(int channel, int hittedState = 0) {
            channel = (channel - 10) % 20;
            var noteData = noteQueues.GetOrConstruct(channel, true).Dequeue();
            noteData.isMissed = hittedState >= 0;
            deferHitNote.Enqueue(noteData);
            if(noteData.isMissed && noteData.noteType == NoteType.LongStart)
                missedLongNotes.Add(channel);
            OnHitNote?.Invoke(channel);
            WavEvent(
                noteData.bmsEvent,
                DetunePerSeconds != 0 || hittedState < DetuneRank ? 1 :
                (timingHelper.CurrentPosition - noteData.bmsEvent.time).ToAccurateSecondF() * DetunePerSeconds + 1
            );
        }

        private void InternalHitNote(int channel, TimeSpan timeDiff) =>
            InternalHitNote(channel, scoreCalculator.HitNote(timeDiff));

        private void CheckNoteStatus() {
            if(scoreCalculator == null) return;
            foreach(var queue in noteQueues.Values) {
                if(queue == null || queue.Count <= 0)
                    continue;
                var noteData = queue.Peek();
                var channel = noteData.Channel;
                if(noteData.noteType != NoteType.LongEnd || !missedLongNotes.Remove(channel)) {
                    var timeDiff = timingHelper.CurrentPosition - noteData.bmsEvent.time;
                    if(scoreCalculator.HitNote(timeDiff, true) >= 0) {
                        if(AutoTriggerLongNoteEnd &&
                            noteData.noteType == NoteType.LongEnd &&
                            timeDiff <= TimeSpan.Zero)
                            InternalHitNote(channel, timeDiff);
                        continue;
                    }
                }
                InternalHitNote(channel, -1);
            }
        }

        private void ReportBeatFlow() {
            var beatFlow = timingHelper.BeatFlow;
            var timeSignature = timingHelper.TimeSignature;
            NoteLaneManager.SetBeatFlowEffect((1 - beatFlow % timeSignature / timeSignature) * (1 - beatFlow % 1F));
        }

        public bool IsChannelPlayable(int channelId) {
            switch(channelId) {
                case 11: case 51: return (PlayableLayout & BMSKeyLayout.P11) == BMSKeyLayout.P11;
                case 12: case 52: return (PlayableLayout & BMSKeyLayout.P12) == BMSKeyLayout.P12;
                case 13: case 53: return (PlayableLayout & BMSKeyLayout.P13) == BMSKeyLayout.P13;
                case 14: case 54: return (PlayableLayout & BMSKeyLayout.P14) == BMSKeyLayout.P14;
                case 15: case 55: return (PlayableLayout & BMSKeyLayout.P15) == BMSKeyLayout.P15;
                case 16: case 56: return (PlayableLayout & BMSKeyLayout.P16) == BMSKeyLayout.P16;
                case 17: case 57: return (PlayableLayout & BMSKeyLayout.P17) == BMSKeyLayout.P17;
                case 18: case 58: return (PlayableLayout & BMSKeyLayout.P18) == BMSKeyLayout.P18;
                case 19: case 59: return (PlayableLayout & BMSKeyLayout.P19) == BMSKeyLayout.P19;
                case 21: case 61: return (PlayableLayout & BMSKeyLayout.P21) == BMSKeyLayout.P21;
                case 22: case 62: return (PlayableLayout & BMSKeyLayout.P22) == BMSKeyLayout.P22;
                case 23: case 63: return (PlayableLayout & BMSKeyLayout.P23) == BMSKeyLayout.P23;
                case 24: case 64: return (PlayableLayout & BMSKeyLayout.P24) == BMSKeyLayout.P24;
                case 25: case 65: return (PlayableLayout & BMSKeyLayout.P25) == BMSKeyLayout.P25;
                case 26: case 66: return (PlayableLayout & BMSKeyLayout.P26) == BMSKeyLayout.P26;
                case 27: case 67: return (PlayableLayout & BMSKeyLayout.P27) == BMSKeyLayout.P27;
                case 28: case 68: return (PlayableLayout & BMSKeyLayout.P28) == BMSKeyLayout.P28;
                case 29: case 69: return (PlayableLayout & BMSKeyLayout.P29) == BMSKeyLayout.P29;
                default: return false;
            }
        }
    }
}
