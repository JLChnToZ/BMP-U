using System;
using System.Collections.Generic;

namespace BMS {
    public class TimeLine {
        readonly List<KeyFrame> keyframes;
        readonly List<KeyFrameRaw> rawKeyFrames;
        IList<KeyFrame> keyFrameReadonly;
        public IList<KeyFrame> KeyFrames {
            get {
                if(keyFrameReadonly == null)
                    keyFrameReadonly = keyframes.AsReadOnly();
                return keyFrameReadonly;
            }
        }

        public bool Normalized {
            get { return rawKeyFrames.Count == 0; }
        }

        public int Count {
            get { return keyframes.Count; }
        }

        public TimeLine() {
            keyframes = new List<KeyFrame>();
            rawKeyFrames = new List<KeyFrameRaw>();
        }

        internal void AddRawKeyFrame(int verse, float beat, int value, int randomGroup, int randomIndex) {
            rawKeyFrames.Add(new KeyFrameRaw(verse, beat, value, randomGroup, randomIndex));
        }

        internal IEnumerable<MeasureBeat> GetAllMeasureBeats() {
            foreach(var keyframe in rawKeyFrames)
                yield return keyframe.MeasureBeat;
        }

        internal IEnumerable<KeyFrameRaw> GetRawKeyframes() {
            foreach(var keyframe in rawKeyFrames)
                yield return keyframe;
        }

        internal void Normalize(Dictionary<MeasureBeat, TimeSpan> mapping) {
            if(Normalized) return;
            foreach(var rawKeyFrame in rawKeyFrames)
                keyframes.Add(new KeyFrame(mapping[rawKeyFrame.MeasureBeat], rawKeyFrame.Value, rawKeyFrame.RandomGroup, rawKeyFrame.RandomIndex));
            keyframes.Sort();
            rawKeyFrames.Clear();
        }

        internal void Clear() {
            keyframes.Clear();
            rawKeyFrames.Clear();
        }
    }
}
