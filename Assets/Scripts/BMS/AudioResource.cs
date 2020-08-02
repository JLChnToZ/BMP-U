using System;
using BMS;
using ManagedBass;
using BananaBeats.Utils;
using UniRx.Async;
using SharpFileSystem;
using BassPlaybackState = ManagedBass.PlaybackState;
using Debug = UnityEngine.Debug;

namespace BananaBeats {
    public class AudioResource: BMSResource {
        int handle;
        long sliceEnd;
        float pitch = 1;
        float volume = 1;

        public float Pitch {
            get => pitch;
            set {
                pitch = value;
                try {
                    if(handle != 0 && Bass.ChannelIsActive(handle) == BassPlaybackState.Playing)
                        UpdatePitch();
                } catch(BassException ex) {
                    Debug.LogError($"BASS: Failed to set pitch for handle {handle:X8}: {ex.ErrorCode}");
                }
            }
        }

        public float Volume {
            get => volume;
            set {
                volume = value;
                try {
                    if(handle != 0 && Bass.ChannelIsActive(handle) == BassPlaybackState.Playing)
                        UpdateVolume();
                } catch(BassException ex) {
                    Debug.LogError($"BASS: Failed to set volume for {handle:X8}: {ex.ErrorCode}");
                }
            }
        }

        public static void InitEngine() {
            if(!Bass.Init())
                Debug.LogWarning($"BASS: Failed to init: {Bass.LastError}");
        }

        public AudioResource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath path) :
            base(resourceData, fileSystem, path) {
        }

        protected override async UniTask LoadImpl() {
            if(handle != 0) return;
            await UniTask.SwitchToThreadPool();
            if(filePath.IsReal(fileSystem))
                handle = Bass.CreateStream(filePath.ToString());
            else {
                var fileData = await fileSystem.ReadAllBytesAsync(filePath);
                handle = Bass.CreateStream(fileData, 0, fileData.Length, BassFlags.Default);
            }
            if(handle == 0)
                throw new BassException();
        }

        private void UpdatePitch() {
            if(handle == 0)
                throw new InvalidOperationException("Handle uninitialized");
            if(!Bass.ChannelSetAttribute(handle, ChannelAttribute.Frequency, pitch == 1 ? 0 : (pitch * Bass.ChannelGetInfo(handle).Frequency)))
                throw new BassException();
        }

        private void UpdateVolume() { 
            if(handle == 0)
                throw new InvalidOperationException("Handle uninitialized");
            if(!Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, volume))
                throw new BassException();
        }

        public override void Play(BMSEvent bmsEvent) {
            if(handle == 0) return;
            base.Play(bmsEvent);
            long sliceStart = Bass.ChannelSeconds2Bytes(handle, bmsEvent.sliceStart.ToAccurateSecond());
            sliceEnd = bmsEvent.sliceEnd >= TimeSpan.MaxValue ? long.MaxValue :
                Bass.ChannelSeconds2Bytes(handle, bmsEvent.sliceEnd.ToAccurateSecond());
            if(!Bass.ChannelSetPosition(handle, sliceStart))
                throw new BassException();
            switch(Bass.ChannelIsActive(handle)) {
                case BassPlaybackState.Stopped:
                case BassPlaybackState.Paused:
                    UpdateVolume();
                    UpdatePitch();
                    if(!Bass.ChannelPlay(handle))
                        throw new BassException();
                    break;
            }
            wasPlaying = true;
        }

        public override void Pause() {
            if(handle == 0) return;
            base.Pause();
            if(!Bass.ChannelPause(handle))
                throw new BassException();
        }

        public override void Resume() {
            if(handle == 0) return;
            base.Resume();
            switch(Bass.ChannelIsActive(handle)) {
                case BassPlaybackState.Stopped:
                case BassPlaybackState.Paused:
                    if(!Bass.ChannelPlay(handle))
                        throw new BassException();
                    break;
            }
        }

        public override void Reset() {
            if(handle == 0) return;
            base.Reset();
            if(!Bass.ChannelStop(handle))
                throw new BassException();
        }

        public override void Update(TimeSpan diff) {
            if(handle != 0 && Bass.ChannelIsActive(handle) == BassPlaybackState.Playing) {
                if(Bass.ChannelGetPosition(handle) >= sliceEnd && !Bass.ChannelStop(handle))
                    throw new BassException();
            } else if(wasPlaying) {
                wasPlaying = false;
                InvokeEnd();
            }
        }

        public override void Dispose() {
            if(handle != 0) {
                if(!Bass.StreamFree(handle))
                    Debug.LogWarning($"BASS: Failed to free stream {handle:X8}: {Bass.LastError}");
                handle = 0;
            }
            base.Dispose();
        }
    }
}
