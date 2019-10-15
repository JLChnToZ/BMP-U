using System;
using System.Collections.Generic;
using BananaBeats.Utils;
using BMS;
using UniRx.Async;
using Utils;

namespace BananaBeats {
    [Serializable]
    public struct TimingConfig {
        public int scoreLevel;
        public TimeSpan diff;
    }

    public class BMSPlayableManager: BMSPlayer {
        public BMSKeyLayout PlayableLayout { get; set; }

        public TimeSpan PreOffset { get; set; } = TimeSpan.FromSeconds(3);

        private readonly Dictionary<int, int> longNoteSound = new Dictionary<int, int>();
        public BMSTimingHelper PreTimingHelper { get; }

        public event BMSEventDelegate PreBMSEvent;

        public event Action<int> OnHitNote;

        public BMSPlayableManager(BMSLoader bmsLoader) : base(bmsLoader) {
            PreTimingHelper = new BMSTimingHelper(timingHelper.Chart);
            PreTimingHelper.EventDispatcher.BMSEvent += OnPreBMSEvent;
            PlayableLayout = timingHelper.Chart.Layout;
        }

        protected override async UniTask Update(TimeSpan delta) {
            await base.Update(delta);
            if(IsPlaying)
                PreTimingHelper.CurrentPosition = timingHelper.CurrentPosition + PreOffset;
        }

        public override void Reset() {
            base.Reset();
            PreTimingHelper?.Reset();
        }

        protected override object OnNoteEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                OnHitNote?.Invoke(bmsEvent.data1);
                bmsEvent.type = BMSEventType.WAV;
                return base.OnWAVEvent(bmsEvent);
            }
            return base.OnNoteEvent(bmsEvent);
        }

        protected override object OnLongNoteStartEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                longNoteSound[bmsEvent.data1] = (int)bmsEvent.data2;
                OnHitNote?.Invoke(bmsEvent.data1);
                bmsEvent.type = BMSEventType.WAV;
                return base.OnWAVEvent(bmsEvent);
            }
            return base.OnLongNoteStartEvent(bmsEvent);
        }

        protected override object OnLongNoteEndEvent(BMSEvent bmsEvent) {
            if(!IsChannelPlayable(bmsEvent.data1)) {
                OnHitNote?.Invoke(bmsEvent.data1);
                if(!longNoteSound.TryGetValue(bmsEvent.data1, out int lnStartId) || lnStartId != (int)bmsEvent.data2) {
                    bmsEvent.type = BMSEventType.WAV;
                    return base.OnWAVEvent(bmsEvent);
                }
            }
            return base.OnLongNoteEndEvent(bmsEvent);
        }

        private void OnPreBMSEvent(BMSEvent bmsEvent) {
            PreBMSEvent?.Invoke(bmsEvent, null);
        }

        public void HitNote(int channel, bool isDown) {
            if(!IsChannelPlayable(channel)) return;
            // TODO: Score Calculation & Play sound
            OnHitNote?.Invoke(channel);
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
