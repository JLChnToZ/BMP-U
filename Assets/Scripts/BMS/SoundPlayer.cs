using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;

namespace BMS {
    public class SoundPlayer: MonoBehaviour {
        bool isAdding;

        struct SlicedAudioPlayer {
            public readonly int handle;
            public long sliceStart, sliceEnd;

            public SlicedAudioPlayer(int handle, TimeSpan sliceStart, TimeSpan sliceEnd) {
                this.handle = handle;
                this.sliceStart = Bass.ChannelSeconds2Bytes(handle, sliceStart.ToAccurateSecond());
                this.sliceEnd = sliceEnd >= TimeSpan.MaxValue ? long.MaxValue :
                    Bass.ChannelSeconds2Bytes(handle, (double)sliceEnd.ToAccurateSecond());
            }
        }

        [NonSerialized]
        Dictionary<int, SlicedAudioPlayer> audioSourceIdMapping = new Dictionary<int, SlicedAudioPlayer>();
        [NonSerialized]
        HashSet<int> unusedAudioSources = new HashSet<int>();

        [Obsolete("Unused")]
        public AudioMixerGroup mixerGroup;
        [Obsolete("Unused")]
        public AudioMixerGroup playerMixerGroup;

        bool isPaused;
        float volume = 1;

        public int Polyphony {
            get { return audioSourceIdMapping.Count; }
        }

        public float Volume {
            get { return volume; }
            set {
                volume = value;
                foreach(SlicedAudioPlayer channel in audioSourceIdMapping.Values)
                    Bass.ChannelSetAttribute(channel.handle, ChannelAttribute.Volume, value);
            }
        }

        static SoundPlayer() {
            if(!Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero, IntPtr.Zero))
                Debug.LogErrorFormat("Failed to initialize BASS : {0}", Bass.LastError);
        }

        public void PauseChanged(bool isPaused) {
            if(this.isPaused == isPaused) return;
            if(isPaused) {
                RecycleInUseAudioSources();
                foreach(var audioSource in audioSourceIdMapping.Values)
                    Bass.ChannelPause(audioSource.handle);
            } else {
                foreach(var audioSource in audioSourceIdMapping.Values)
                    Bass.ChannelPlay(audioSource.handle);
            }
            this.isPaused = isPaused;
        }

        public void StopAll() {
            foreach(var audioSource in audioSourceIdMapping.Values)
                Bass.ChannelStop(audioSource.handle);
            audioSourceIdMapping.Clear();
            isPaused = false;
        }

        void OnApplicationQuit() {
            Bass.Stop();
            Bass.Free();
        }

        public void PlaySound(int handle, TimeSpan sliceStart, TimeSpan sliceEnd, int id, bool isPlayer, float pitch, string debugName) {
            if(!isPaused) {
                isAdding = true;
                audioSourceIdMapping[id] = new SlicedAudioPlayer(handle, sliceStart, sliceEnd);
                long bytePos = Bass.ChannelSeconds2Bytes(handle, sliceStart.ToAccurateSecond());
                if(bytePos > 0) {
                    Bass.ChannelSetPosition(handle, bytePos);
                    Bass.ChannelPlay(handle, false);
                } else {
                    Bass.ChannelPlay(handle, true);
                }
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, volume);
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Frequency, pitch == 1 ? 0 : (pitch * Bass.ChannelGetInfo(handle).Frequency));
                isAdding = false;
            }
        }

        void RecycleInUseAudioSources() {
            unusedAudioSources.Clear();
            foreach(var kv in audioSourceIdMapping) {
                if(isAdding) return;
                if(Bass.ChannelIsActive(kv.Value.handle) != PlaybackState.Playing) {
                    unusedAudioSources.Add(kv.Key);
                } else if(Bass.ChannelGetPosition(kv.Value.handle) >= kv.Value.sliceEnd) {
                    Bass.ChannelStop(kv.Value.handle);
                    unusedAudioSources.Add(kv.Key);
                }
            }
            foreach(int unusedChannel in unusedAudioSources) {
                if(isAdding) return;
                audioSourceIdMapping.Remove(unusedChannel);
            }
        }
        
        void Update() {
            if(!isPaused && !isAdding) {
                RecycleInUseAudioSources();
            }
        }
    }
}
