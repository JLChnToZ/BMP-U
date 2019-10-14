using System;
using BMS;
using BananaBeats.Utils;
using UniRx.Async;

namespace BananaBeats {
    public abstract class BMSResource: IDisposable {
        protected readonly IVirtualFSEntry fileEntry;
        protected bool wasPlaying;
        protected BMSEvent currentEvent;

        public BMSResourceData ResourceData { get; }

        public event EventHandler OnEnd;

        protected BMSResource(BMSResourceData resourceData, IVirtualFSEntry fileEntry) {
            ResourceData = resourceData;
            this.fileEntry = fileEntry;
        }

        public abstract UniTask Load();

        public virtual void Update(TimeSpan diff) {
            if(wasPlaying) {
                wasPlaying = false;
                InvokeEnd();
            }
        }

        public virtual void Play(BMSEvent bmsEvent) {
            wasPlaying = true;
            currentEvent = bmsEvent;
        }

        public virtual void Pause() {
            wasPlaying = false;
        }

        public virtual void Resume() {
            wasPlaying = true;
        }

        public virtual void Reset() {
            wasPlaying = false;
            currentEvent = default;
        }

        public virtual void Dispose() {
            wasPlaying = false;
            currentEvent = default;
        }

        protected void InvokeEnd(EventArgs args = null) =>
            OnEnd?.Invoke(this, args ?? EventArgs.Empty);
    }
}
