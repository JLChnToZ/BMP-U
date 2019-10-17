using System;
using BMS;

namespace BananaBeats {
    public class BMSTimingHelper {
        public Chart Chart { get; }

        public Chart.EventDispatcher EventDispatcher { get; }

        public TimeSpan CurrentPosition {
            get { return currentPosition; }
            set {
                currentPosition = value;
                EventDispatcher.Seek(value);
            }
        }

        public TimeSpan StartPosition { get; }

        public float BPM { get; private set; }

        public TimeSpan BPMBasePosition { get; private set; }

        public float BeatFlow =>
            (currentPosition - BPMBasePosition).ToAccurateMinuteF() * BPM + beatFlowBase;

        public float TimeSignature { get; private set; }

        public TimeSpan StopResumePosition { get; private set; }

        private float beatFlowBase;
        private TimeSpan currentPosition;

        public BMSTimingHelper(Chart chart) {
            Chart = chart;
            EventDispatcher = chart.GetEventDispatcher();
            EventDispatcher.BMSEvent += OnBMSEvent;
            var events = chart.Events;
            if(events.Count > 0) {
                StartPosition = events[0].time;
                if(StartPosition > TimeSpan.Zero)
                    StartPosition = TimeSpan.Zero;
            }
            Reset();
        }

        private void OnBMSEvent(BMSEvent bmsEvent) {
            switch(bmsEvent.type) {
                case BMSEventType.BeatReset:
                    OnBeatResetEvent(bmsEvent);
                    break;
                case BMSEventType.BPM:
                    OnBPMEvent(bmsEvent);
                    break;
                case BMSEventType.STOP:
                    OnStopEvent(bmsEvent);
                    break;
            }
        }

        protected void OnBeatResetEvent(BMSEvent bmsEvent) {
            beatFlowBase = 0;
            BPMBasePosition = currentPosition;
            TimeSignature = (float)bmsEvent.Data2F;
        }

        protected void OnBPMEvent(BMSEvent bmsEvent) {
            beatFlowBase = BeatFlow;
            BPMBasePosition = currentPosition;
            BPM = (float)bmsEvent.Data2F;
        }

        protected void OnStopEvent(BMSEvent bmsEvent) {
            TimeSpan offset = new TimeSpan(bmsEvent.data2);
            StopResumePosition = bmsEvent.time + offset;
            BPMBasePosition -= offset;
        }

        public void Reset() {
            currentPosition = StartPosition;
            StopResumePosition = StartPosition;
            BPMBasePosition = StartPosition;
            TimeSignature = 4;
            beatFlowBase = 0;
            BPM = Chart.BPM;
            EventDispatcher.Seek(TimeSpan.MinValue, false);
        }
    }
}
