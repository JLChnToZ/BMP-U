using System;
using BMS;
using UniRx.Async;
using SharpFileSystem;

namespace BananaBeats {
    public abstract class BMSResource: IDisposable {
        protected readonly IFileSystem fileSystem;
        protected readonly FileSystemPath filePath;
        protected bool wasPlaying;
        protected BMSEvent currentEvent;
        private bool loaded;
        private UniTask loadTask;

        public BMSResourceData ResourceData { get; }

        public event EventHandler OnEnd;

        protected BMSResource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath filePath) {
            ResourceData = resourceData;
            this.fileSystem = fileSystem;
            this.filePath = filePath;
        }

        protected abstract UniTask LoadImpl();

        public UniTask Load() {
            if(!loaded) {
                loaded = true;
                loadTask = LoadImpl();
            }
            return loadTask;
        }

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
            loaded = false;
            wasPlaying = false;
            currentEvent = default;
        }

        protected void InvokeEnd(EventArgs args = null) =>
            OnEnd?.Invoke(this, args ?? EventArgs.Empty);
    }
}
