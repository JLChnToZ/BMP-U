using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BMS {
    internal class TimingHelper {
        readonly HashSet<KeyframesHandle> handles = new HashSet<KeyframesHandle>();

        int endedCount;
        public bool IsEnded {
            get { return endedCount >= handles.Count; }
        }

        readonly TimeSpan offset;
        public TimingHelper() : this(TimeSpan.Zero) { }

        public TimingHelper(TimeSpan offset) {
            this.offset = offset;
        }

        public event Action<TimeSpan, int, int> OnIndexChange;
        void InvokeIndexChange(TimeSpan timePosition, int id, int value) {
            if(OnIndexChange != null)
                OnIndexChange.Invoke(timePosition, id, value);
        }

        public void AddTimelineHandle(TimeLine timeline, int timeLineId) {
            if(!timeline.Normalized) return;
            var handle = new KeyframesHandle(this, timeline.KeyFrames, timeLineId);
            handles.Add(handle);
        }

        public void Reset() {
            foreach(var handle in handles)
                handle.Reset();
            endedCount = 0;
        }

        public void Update(TimeSpan timePosition) {
            timePosition += offset;
            foreach(var handle in handles)
                handle.Update(timePosition);
        }

        public void ClearHandles() {
            handles.Clear();
        }

        class KeyframesHandle {
            readonly IList<KeyFrame> keyFrameList;
            readonly TimingHelper parent;
            readonly int id;
            int index;
            bool isEnded;

            public KeyframesHandle(TimingHelper parent, IList<KeyFrame> keyFrameList, int id) {
                this.parent = parent;
                this.keyFrameList = keyFrameList;
                this.id = id;
                Reset();
            }

            public void Reset() {
                index = -1;
                isEnded = false;
            }

            public void Update(TimeSpan timePosition) {
                int maxIndex = keyFrameList.Count - 1;
                while(index < maxIndex) {
                    var keyFrame = keyFrameList[index + 1];
                    if(timePosition < keyFrame.TimePosition) break;
                    parent.InvokeIndexChange(keyFrame.TimePosition, id, keyFrame.Value);
                    index++;
                }
                if(index >= maxIndex && !isEnded) {
                    parent.endedCount++;
                    isEnded = true;
                }
            }
        }
    }

    internal class TimeSpanHandle<T> {
        readonly IList<KeyValuePair<TimeSpan, T>> keyFrameList;
        readonly int id;
        int index;
        bool isEnded;

        public bool IsEnded { get { return isEnded; } }
        public event Action<TimeSpan, T> OnNotified;

        public TimeSpanHandle(IEnumerable<KeyValuePair<TimeSpan, T>> keyFrameCollection) {
            List<KeyValuePair<TimeSpan, T>> keyFrameList = null;
            this.keyFrameList = keyFrameCollection as IList<KeyValuePair<TimeSpan, T>> ?? (keyFrameList = new List<KeyValuePair<TimeSpan, T>>(keyFrameCollection));
            if(keyFrameList != null)
                keyFrameList.Sort(KeyValuePairComparer<TimeSpan, T>.Default);
            Reset();
        }

        public void Reset() {
            index = -1;
            isEnded = false;
        }

        public void Update(TimeSpan timePosition) {
            int maxIndex = keyFrameList.Count - 1;
            while(index < maxIndex) {
                var keyFrame = keyFrameList[index + 1];
                if(timePosition < keyFrame.Key) break;
                if(OnNotified != null) OnNotified.Invoke(keyFrame.Key, keyFrame.Value);
                index++;
            }
            if(index >= maxIndex && !isEnded)
                isEnded = true;
        }
    }
}
