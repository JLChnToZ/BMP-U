using System;
using BMS;
using ManagedBass;
using BananaBeats.Utils;
using UniRx.Async;
using SharpFileSystem;

namespace BananaBeats {
    public class AudioResource: BMSResource {
        int handle;
        long sliceEnd;

        public float Pitch { get; set; } = 1;

        public static void InitEngine() {
            if(!Bass.Init())
                UnityEngine.Debug.LogWarning($"BASS init error: {Bass.LastError}");
        }

        public AudioResource(BMSResourceData resourceData, IFileSystem fileSystem, FileSystemPath path) :
            base(resourceData, fileSystem, path) {
        }

        public override async UniTask Load() {
            if(handle != 0) return;
            await UniTask.SwitchToTaskPool();
            if(filePath.IsReal(fileSystem))
                handle = Bass.CreateStream(filePath.ToString());
            else {
                var fileData = await fileSystem.ReadAllBytesAsync(filePath);
                handle = Bass.CreateStream(fileData, 0, fileData.Length, BassFlags.Default);
            }
            if(handle == 0)
                throw new BassException(Bass.LastError);
        }

        public override void Play(BMSEvent bmsEvent) {
            if(handle == 0) return;
            base.Play(bmsEvent);
            long sliceStart = Bass.ChannelSeconds2Bytes(handle, bmsEvent.sliceStart.ToAccurateSecond());
            sliceEnd = bmsEvent.sliceEnd >= TimeSpan.MaxValue ? long.MaxValue :
                Bass.ChannelSeconds2Bytes(handle, bmsEvent.sliceEnd.ToAccurateSecond());
            if(!Bass.ChannelSetPosition(handle, sliceStart))
                throw new BassException(Bass.LastError);
            switch(Bass.ChannelIsActive(handle)) {
                case PlaybackState.Stopped:
                case PlaybackState.Paused:
                    if(!Bass.ChannelPlay(handle))
                        throw new BassException(Bass.LastError);
                    if(!Bass.ChannelSetAttribute(handle, ChannelAttribute.Frequency, Pitch == 1 ? 0 : (Pitch * Bass.ChannelGetInfo(handle).Frequency)))
                        throw new BassException(Bass.LastError);
                    break;
            }
            wasPlaying = true;
        }

        public override void Pause() {
            if(handle == 0) return;
            base.Pause();
            if(!Bass.ChannelPause(handle))
                throw new BassException(Bass.LastError);
        }

        public override void Resume() {
            if(handle == 0) return;
            base.Resume();
            switch(Bass.ChannelIsActive(handle)) {
                case PlaybackState.Stopped:
                case PlaybackState.Paused:
                    if(!Bass.ChannelPlay(handle))
                        throw new BassException(Bass.LastError);
                    break;
            }
        }

        public override void Reset() {
            if(handle == 0) return;
            base.Reset();
            if(!Bass.ChannelStop(handle))
                throw new BassException(Bass.LastError);
        }

        public override void Update(TimeSpan diff) {
            if(handle != 0 && Bass.ChannelIsActive(handle) == PlaybackState.Playing) {
                if(Bass.ChannelGetPosition(handle) >= sliceEnd && !Bass.ChannelStop(handle))
                    throw new BassException(Bass.LastError);
            } else if(wasPlaying) {
                wasPlaying = false;
                InvokeEnd();
            }
        }

        public override void Dispose() {
            if(handle != 0) {
                if(!Bass.StreamFree(handle))
                    throw new BassException(Bass.LastError);
                handle = 0;
            }
            base.Dispose();
        }
    }
}
