using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Async;
using BananaBeats.Utils;
using BMS;

namespace BananaBeats {
    public delegate void BMSEventDelegate(BMSEvent bmsEvent, object resource);

    public enum PlaybackState: byte {
        Stopped = 0,
        Playing = 1,
        Paused = 2,
    }

    public class BMSPlayer: IDisposable {

        public BMSLoader BMSLoader { get; }

        public Chart Chart => timingHelper.Chart;

        private PlaybackState playbackState;
        public PlaybackState PlaybackState {
            get { return playbackState; }
            private set {
                if(playbackState == value)
                    return;
                playbackState = value;
                if(!Disposed)
                    PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public TimeSpan CurrentPosition => timingHelper.CurrentPosition;

        public float BPM => timingHelper.BPM;

        public TimeSpan BPMBasePosition => timingHelper.BPMBasePosition;

        public float BeatFlow => timingHelper.BeatFlow;

        public float TimeSignature => timingHelper.TimeSignature;

        public TimeSpan StopPosition => timingHelper.StopResumePosition;

        public TimeSpan Duration => timingHelper.EventDispatcher.EndTime;

        public int PolyPhony { get; private set; }

        public bool PlayBGA { get; set; } = true;

        public bool PlaySound { get; set; } = true;

        protected bool Disposed { get; private set; }

        public event BMSEventDelegate BMSEvent;

        public event EventHandler PlaybackStateChanged;

        protected readonly BMSTimingHelper timingHelper;

        private readonly HashSet<BMSResource> playingResources = new HashSet<BMSResource>();
        private readonly HashSet<BMSResource> endedResources = new HashSet<BMSResource>();
        private DateTime lastUpdate;
        private UniTask updateTask;
        private IDisposable mainUpdateLoop;

        public BMSPlayer(BMSLoader bmsLoader) {
            BMSLoader = bmsLoader;
            timingHelper = new BMSTimingHelper(bmsLoader.Chart);
            timingHelper.EventDispatcher.BMSEvent += OnBMSEvent;
            Reset();
        }

        private void RegisterUpdateLoop() {
            if(Disposed || mainUpdateLoop != null) return;
            updateTask = UniTask.CompletedTask;
            lastUpdate = DateTime.UtcNow;
            mainUpdateLoop = GameLoop.RunAsUpdate(DoUpdateLoop);
        }

        private void UnregisterUpdateLoop() {
            if(mainUpdateLoop == null) return;
            mainUpdateLoop.Dispose();
            mainUpdateLoop = null;
        }

        public virtual void Play() {
            if(Disposed) return;
            PlaybackState = PlaybackState.Playing;
            RegisterUpdateLoop();
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
            if(Disposed) return;
            PlaybackState = PlaybackState.Paused;
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
            PlaybackState = PlaybackState.Stopped;
            UnregisterUpdateLoop();
            timingHelper.Reset();
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

        private void DoUpdateLoop() {
            if(!updateTask.IsCompleted)
                return;
            try {
                updateTask.GetResult();
            } catch(Exception ex) {
#if UNITY_EDITOR || DEBUG
                Debug.LogException(ex);
#endif
            }
            try {
                var current = DateTime.UtcNow;
                var delta = current - lastUpdate;
                lastUpdate = current;
                updateTask = Update(delta);
            } catch(Exception ex) {
                updateTask = UniTask.FromException(ex);
            }
        }

        protected virtual UniTask Update(TimeSpan delta) {
            try {
                if(PlaybackState == PlaybackState.Playing) {
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
                    timingHelper.CurrentPosition += delta;
                    if(timingHelper.EventDispatcher.IsEnd && playingResources.Count == 0) {
                        PlaybackState = PlaybackState.Stopped;
                        UnregisterUpdateLoop();
                    }
                } else if(playingResources.Count == 0)
                    UnregisterUpdateLoop();
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
            WavEvent(bmsEvent);

        protected virtual object OnLongNoteStartEvent(BMSEvent bmsEvent) =>
            WavEvent(bmsEvent);

        protected virtual object OnLongNoteEndEvent(BMSEvent bmsEvent) =>
            WavEvent(bmsEvent);

        protected virtual object OnWAVEvent(BMSEvent bmsEvent) =>
            WavEvent(bmsEvent);

        protected object WavEvent(BMSEvent bmsEvent, float pitch = 1, bool ignoreType = false) {
            object resource = null;
            if(BMSLoader.TryGetWAV((int)bmsEvent.data2, out var wav)) {
                resource = wav;
                if((ignoreType || bmsEvent.type == BMSEventType.WAV) && PlaySound)
                    StartHandle(bmsEvent, wav, pitch);
            }
            return resource;
        }

        protected virtual object OnBeatResetEvent(BMSEvent bmsEvent) => null;

        protected virtual object OnBPMEvent(BMSEvent bmsEvent) => null;

        protected virtual object OnStopEvent(BMSEvent bmsEvent) => null;

        protected virtual object OnUnknownEvent(BMSEvent bmsEvent) => null;

        public bool IsHandling(BMSResource resource) =>
            resource != null && playingResources.Contains(resource) && !endedResources.Contains(resource);

        public virtual void StartHandle(BMSEvent bmsEvent, BMSResource resource, float pitch = 1) {
            if(resource == null) return;
            try {
                if(resource is AudioResource audioRes)
                    audioRes.Pitch = pitch;
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
            Disposed = true;
            UnregisterUpdateLoop();
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
