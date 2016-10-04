using System;
using System.Collections.Generic;

namespace BMS {
    public class KeyComparer<TKey, TValue>: Comparer<KeyValuePair<TKey, TValue>> {
        static KeyComparer<TKey, TValue> defaultInstance;
        public static new KeyComparer<TKey, TValue> Default {
            get {
                if(defaultInstance == null)
                    defaultInstance = new KeyComparer<TKey, TValue>();
                return defaultInstance;
            }
        }

        readonly IComparer<TKey> keyComparer;

        private KeyComparer() : this(Comparer<TKey>.Default) {
        }

        public KeyComparer(IComparer<TKey> keyComparer) {
            this.keyComparer = keyComparer;
        }

        public override int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) {
            return keyComparer.Compare(x.Key, y.Key);
        }
    }

    public struct MeasureBeat: IComparable<MeasureBeat>, IEquatable<MeasureBeat> {
        readonly int measure;
        readonly float beat;
        readonly bool isAligned;

        public int Measure { get { return measure; } }
        public float Beat { get { return beat; } }
        public bool IsAligned { get { return isAligned; } }

        public MeasureBeat(int measure, float beat) {
            this.measure = measure;
            this.beat = beat;
            this.isAligned = false;
        }

        public MeasureBeat(int measure, float beat, float timeSignature) {
            this.measure = measure;
            this.beat = beat * timeSignature;
            this.isAligned = true;
        }

        public MeasureBeat Align(float timeSignature) {
            return isAligned ? this : new MeasureBeat(measure, beat, timeSignature);
        }

        public TimeSpan ToTimeSpan(MeasureBeat reference, TimeSpan referenceTime, float timeSignature, float bpm) {
            reference = reference.Align(timeSignature);
            var currentPoint = Align(timeSignature);
            float referenceMeter = reference.measure * timeSignature + reference.beat;
            float currentMeter = currentPoint.measure * timeSignature + currentPoint.beat;
            return referenceTime.Add(new TimeSpan((long)Math.Round((currentMeter - referenceMeter) * 4 / bpm * TimeSpan.TicksPerMinute)));
        }

        public int CompareTo(MeasureBeat other) {
            int compare = measure.CompareTo(other.measure);
            if(compare == 0) compare = beat.CompareTo(other.beat);
            return compare;
        }

        public bool Equals(MeasureBeat other) {
            return measure.Equals(other.measure) && ((beat == 0 && other.beat == 0) || (beat.Equals(other.beat) && isAligned.Equals(other.isAligned)));
        }

        public override bool Equals(object obj) {
            return obj is MeasureBeat && Equals((MeasureBeat)obj);
        }

        public override int GetHashCode() {
            return measure.GetHashCode() * 47 + beat.GetHashCode() * 11 + 29;
        }

        public override string ToString() {
            return string.Format("{0}:{1}", measure, beat * 4);
        }

        public static bool operator >(MeasureBeat lhs, MeasureBeat rhs) {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator >=(MeasureBeat lhs, MeasureBeat rhs) {
            return lhs.CompareTo(rhs) >= 0;
        }

        public static bool operator <(MeasureBeat lhs, MeasureBeat rhs) {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator <=(MeasureBeat lhs, MeasureBeat rhs) {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator ==(MeasureBeat lhs, MeasureBeat rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MeasureBeat lhs, MeasureBeat rhs) {
            return !lhs.Equals(rhs);
        }
    }

    public struct KeyFrame: IComparable<KeyFrame> {
        readonly long timePosition;
        readonly int value;
        readonly int randomGroup;
        readonly int randomIndex;

        public TimeSpan TimePosition {
            get { return new TimeSpan(timePosition); }
        }
        
        public int Value {
            get { return value; }
        }

        public int RandomGroup {
            get { return randomGroup; }
        }

        public int RandomIndex {
            get { return randomIndex; }
        }

        public KeyFrame(TimeSpan timePosition, int value, int randomGroup = 0, int randomIndex = 0) {
            this.timePosition = timePosition.Ticks;
            this.value = value;
            this.randomGroup = randomGroup;
            this.randomIndex = randomIndex;
        }

        public int CompareTo(KeyFrame other) {
            return timePosition.CompareTo(other.timePosition);
        }
    }

    public struct TimeLineKeyFrame: IComparable<TimeLineKeyFrame> {
        readonly public short timeLineId;
        readonly public long timePosition;
        readonly public short value;
        readonly public short randomGroup;
        readonly public short randomIndex;

        public TimeLineKeyFrame(short timeLineId, TimeSpan timePosition, short value, short randomGroup = 0, short randomIndex = 0) {
            this.timeLineId = timeLineId;
            this.timePosition = timePosition.Ticks;
            this.value = value;
            this.randomGroup = randomGroup;
            this.randomIndex = randomIndex;
        }

        public int CompareTo(TimeLineKeyFrame other) {
            return timePosition.CompareTo(other.timePosition);
        }
    }
    
    public struct KeyFrameRaw: IComparable<KeyFrameRaw> {
        readonly MeasureBeat measureBeat;
        readonly int value;
        readonly int randomGroup;
        readonly int randomIndex;

        public MeasureBeat MeasureBeat {
            get { return measureBeat; }
        }

        public int Value {
            get { return value; }
        }

        public int RandomGroup {
            get { return randomGroup; }
        }

        public int RandomIndex {
            get { return randomIndex; }
        }

        public KeyFrameRaw(int measure, float beat, int value, int randomGroup = 0, int randomIndex = 0) {
            this.measureBeat = new MeasureBeat(measure, beat);
            this.value = value;
            this.randomGroup = randomGroup;
            this.randomIndex = randomIndex;
        }

        public int CompareTo(KeyFrameRaw other) {
            return measureBeat.CompareTo(other.measureBeat);
        }
    }

    public class TimingPoint: IComparable<TimingPoint>, IEquatable<TimingPoint> {
        public MeasureBeat measureBeat;
        public TimeSpan timePosition;
        public float timeSignature;
        public bool hasTimeSignature;
        public float bpm;
        public bool hasBPM;

        public bool isBegin {
            get { return measureBeat.Beat == 0; }
        }

        public int CompareTo(TimingPoint other) {
            return measureBeat.CompareTo(other.measureBeat);
        }

        public override int GetHashCode() {
            return measureBeat.GetHashCode() * 23;
        }

        public bool Equals(TimingPoint other) {
            return measureBeat.Equals(other.measureBeat);
        }

        public override bool Equals(object obj) {
            return obj is TimingPoint && measureBeat.Equals((obj as TimingPoint).measureBeat);
        }

        public TimeSpan ConvertMeasureBeat(MeasureBeat target) {
            return target.ToTimeSpan(measureBeat, timePosition, timeSignature, bpm);
        }
    }
}
