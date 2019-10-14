using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Async;
using BMS;

namespace BananaBeats {
    public delegate void BMSEventDelegate(BMSEvent bmsEvent, object resource);

    public class BMSPlayer: IDisposable {

        public BMSLoader BMSLoader { get; }

        public Chart Chart => timingHelper.Chart;

        public bool IsPlaying { get; private set; }

        public TimeSpan CurrentPosition => timingHelper.CurrentPosition;

        public float BPM => timingHelper.BPM;

        public TimeSpan BPMBasePosition => timingHelper.BPMBasePosition;

        public float BeatFlow => timingHelper.BeatFlow;

        public float TimeSignature => timingHelper.TimeSignature;

        public TimeSpan StopPosition => timingHelper.StopResumePosition;

        public int PolyPhony { get; private set; }

        public bool PlayBGA { get; set; } = true;

        public bool PlaySound { get; set; } = true;

        public event BMSEventDelegate BMSEvent;

        protected readonly BMSTimingHelper timingHelper;

        private readonly HashSet<BMSResource> playingResources = new HashSet<BMSResource>();
        private readonly HashSet<BMSResource> endedResources = new HashSet<BMSResource>();
        private DateTime lastUpdate;
        private bool disposed;

        public BMSPlayer(BMSLoader bmsLoader) {
            BMSLoader = bmsLoader;
            timingHelper = new BMSTimingHelper(bmsLoader.Chart);
            timingHelper.EventDispatcher.BMSEvent += OnBMSEvent;
            Reset();
            DoUpdateLoop().Forget();
        }

        public virtual void Play() {
            IsPlaying = true;
            foreach(var resource in playingResources)
                try {
                    resource.Resume();
                } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                    Debug.LogException(ex);
#endif
                }
        }

        public virtual void Pause() {
            IsPlaying = false;
            foreach(var resource in playingResources)
                try {
                    resource.Pause();
                } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                    Debug.LogException(ex);
#endif
                }
        }

        public virtual void Reset() {
            IsPlaying = false;
            timingHelper.Reset();
            timingHelper.EventDispatcher.Seek(TimeSpan.MinValue, false);
            foreach(var resource in playingResources)
                try {
                    resource.Reset();
                } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                    Debug.LogException(ex);
#endif
                }
            playingResources.Clear();
        }

        private async UniTaskVoid DoUpdateLoop() {
            lastUpdate = DateTime.UtcNow;
            var task = UniTask.CompletedTask;
            do {
                await UniTask.Yield();
                try {
                    if(task.IsCompleted)
                        task.GetResult();
                    else
                        await task;
                } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                    Debug.LogException(ex);
#endif
                }
                var current = DateTime.UtcNow;
                var delta = current - lastUpdate;
                lastUpdate = current;
                task = Update(delta);
            } while(!disposed);
        }

        protected virtual UniTask Update(TimeSpan delta) {
            try {
                foreach(var resource in playingResources)
                    try {
                        if(!endedResources.Contains(resource))
                            resource.Update(delta);
                    } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                        Debug.LogException(ex);
#endif
                    }
                if(endedResources.Count > 0) {
                    playingResources.ExceptWith(endedResources);
                    endedResources.Clear();
                }
                if(IsPlaying)
                    timingHelper.CurrentPosition += delta;
                return UniTask.CompletedTask;
            } catch(Exception ex) {
                return UniTask.FromException(ex);
            }
        }

        private void OnBMSEvent(BMSEvent bmsEvent) {
            object resource;
            switch(bmsEvent.type) {
                case BMSEventType.BMP:
                    resource = OnBMPEvent(bmsEvent);
                    break;
                case BMSEventType.WAV:
                    resource = OnWAVEvent(bmsEvent);
                    break;
                case BMSEventType.Note:
                    resource = OnNoteEvent(bmsEvent);
                    break;
                case BMSEventType.LongNoteStart:
                    resource = OnLongNoteStartEvent(bmsEvent);
                    break;
                case BMSEventType.LongNoteEnd:
                    resource = OnLongNoteEndEvent(bmsEvent);
                    break;
                case BMSEventType.BeatReset:
                    resource = OnBeatResetEvent(bmsEvent);
                    break;
                case BMSEventType.BPM:
                    resource = OnBPMEvent(bmsEvent);
                    break;
                case BMSEventType.STOP:
                    resource = OnStopEvent(bmsEvent);
                    break;
                case BMSEventType.Unknown:
                default:
                    resource = OnUnknownEvent(bmsEvent);
                    break;
            }
            BMSEvent?.Invoke(bmsEvent, resource);
            if(timingHelper.EventDispatcher.IsEnd)
                IsPlaying = false;
        }

        protected virtual object OnBMPEvent(BMSEvent bmsEvent) {
            object resource = null;
            if(BMSLoader.TryGetBGA((int)bmsEvent.data2, out var bga)) {
                resource = bga;
                if(PlayBGA) StartHandle(bmsEvent, bga.resource);
            } else if(BMSLoader.TryGetBMP((int)bmsEvent.data2, out var bmp)) {
                resource = bmp;
                if(PlayBGA) StartHandle(bmsEvent, bmp);
            }
            return resource;
        }

        protected virtual object OnNoteEvent(BMSEvent bmsEvent) =>
            DefaultWAVEvent(bmsEvent);

        protected virtual object OnLongNoteStartEvent(BMSEvent bmsEvent) =>
            DefaultWAVEvent(bmsEvent);

        protected virtual object OnLongNoteEndEvent(BMSEvent bmsEvent) =>
            DefaultWAVEvent(bmsEvent);

        protected virtual object OnWAVEvent(BMSEvent bmsEvent) =>
            DefaultWAVEvent(bmsEvent);

        private object DefaultWAVEvent(BMSEvent bmsEvent) {
            object resource = null;
            if(BMSLoader.TryGetWAV((int)bmsEvent.data2, out var wav)) {
                resource = wav;
                if(bmsEvent.type == BMSEventType.WAV && PlaySound)
                    StartHandle(bmsEvent, wav);
            }
            return resource;
        }

        protected virtual object OnBeatResetEvent(BMSEvent bmsEvent) => null;

        protected virtual object OnBPMEvent(BMSEvent bmsEvent) => null;

        protected virtual object OnStopEvent(BMSEvent bmsEvent) => null;

        protected virtual object OnUnknownEvent(BMSEvent bmsEvent) => null;

        public bool IsHandling(BMSResource resource) =>
            resource != null && playingResources.Contains(resource) && !endedResources.Contains(resource);

        public virtual void StartHandle(BMSEvent bmsEvent, BMSResource resource) {
            if(resource == null) return;
            try {
                resource.Play(bmsEvent);
            } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                Debug.LogException(ex);
#endif
            }
            if(!playingResources.Add(resource)) return;
            if(resource is AudioResource) PolyPhony++;
            resource.OnEnd += ResourceEnded;
        }

        public virtual void Dispose() {
            disposed = true;
            Reset();
        }

        protected virtual void ResourceEnded(object sender, EventArgs e) {
            if(!(sender is BMSResource res)) return;
            res.OnEnd -= ResourceEnded;
            if(!playingResources.Contains(res)) return;
            if(!endedResources.Add(res)) return;
            if(res is AudioResource) PolyPhony--;
        }
    }
}
