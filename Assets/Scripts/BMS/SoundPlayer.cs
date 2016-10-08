using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

namespace BMS {
    public class SoundPlayer: MonoBehaviour {
        struct InUseAudioSource: IEquatable<InUseAudioSource> {
            public readonly AudioSource audioSource;
            public readonly int id;

            public InUseAudioSource(AudioSource audioSource, int id) {
                this.id = id;
                this.audioSource = audioSource;
            }

            public InUseAudioSource(int id) {
                this.audioSource = null;
                this.id = id;
            }

            public bool Equals(InUseAudioSource other) {
                return id == other.id;
            }

            public override bool Equals(object obj) {
                return (obj is InUseAudioSource) && Equals((InUseAudioSource)obj);
            }

            public override int GetHashCode() {
                return id.GetHashCode();
            }
        }

        struct AudioSourceSliced {
            public readonly AudioSource audioSource;
            public readonly float sliceStart, sliceEnd;

            public AudioSourceSliced(AudioSource audioSource, TimeSpan sliceStart, TimeSpan sliceEnd) {
                this.audioSource = audioSource;
                this.sliceStart = (float)sliceStart.Ticks / TimeSpan.TicksPerSecond;
                this.sliceEnd = (float)sliceEnd.Ticks / TimeSpan.TicksPerSecond;
            }
        }

        [NonSerialized]
        Queue<AudioSource> freeAudioSources = new Queue<AudioSource>();

        [NonSerialized]
        Dictionary<InUseAudioSource, float> inUseAudioSources = new Dictionary<InUseAudioSource, float>();

        [NonSerialized]
        Dictionary<int, AudioSourceSliced> audioSourceIdMapping = new Dictionary<int, AudioSourceSliced>();

        [NonSerialized]
        HashSet<AudioSource> changingAudioSource = new HashSet<AudioSource>();

        public AudioMixerGroup mixerGroup;
        public AudioMixerGroup playerMixerGroup;

        bool isPaused;
        float volume = 1;

        public int Polyphony {
            get { return inUseAudioSources.Count; }
        }

        public float Volume {
            get { return volume; }
            set {
                volume = value;
                foreach(var audioSouce in inUseAudioSources.Keys)
                    audioSouce.audioSource.volume = volume;
            }
        }
        
        public void PauseChanged(bool isPaused) {
            if(this.isPaused == isPaused) return;
            if(isPaused) {
                RecycleInUseAudioSources();
                var temp = new HashSet<InUseAudioSource>(inUseAudioSources.Keys);
                foreach(var audioSource in temp) {
                    inUseAudioSources[audioSource] = audioSource.audioSource.time;
                    audioSource.audioSource.Stop();
                }
            } else {
                foreach(var audioSource in inUseAudioSources) {
                    audioSource.Key.audioSource.Play();
                    audioSource.Key.audioSource.time = audioSource.Value;
                }
            }
            this.isPaused = isPaused;
        }

        public void StopAll() {
            foreach(var audioSource in inUseAudioSources.Keys) {
                audioSource.audioSource.Stop();
                freeAudioSources.Enqueue(audioSource.audioSource);
            }
            inUseAudioSources.Clear();
            audioSourceIdMapping.Clear();
            isPaused = false;
        }

        public void PlaySound(AudioClip audio, TimeSpan sliceStart, TimeSpan sliceEnd, int id, bool isPlayer, float pitch, string debugName) {
            AudioSourceSliced audioSource = default(AudioSourceSliced);
            if(inUseAudioSources.ContainsKey(new InUseAudioSource(id))) {
                audioSource = audioSourceIdMapping[id];
                changingAudioSource.Add(audioSource.audioSource);
                audioSource.audioSource.Stop();
                // audioSource.time = 0;
            } else {
                audioSource = new AudioSourceSliced(GetFreeAudioSource(isPlayer), sliceStart, sliceEnd);
                changingAudioSource.Add(audioSource.audioSource);
                audioSource.audioSource.clip = audio;
            }
            audioSource.audioSource.volume = volume;
            inUseAudioSources[new InUseAudioSource(audioSource.audioSource, id)] = 0;
            audioSourceIdMapping[id] = audioSource;
            audioSource.audioSource.pitch = pitch;
            if(!isPaused) {
                if(!audioSource.audioSource.isPlaying)
                    audioSource.audioSource.Play();
                audioSource.audioSource.time = (float)sliceStart.Ticks / TimeSpan.TicksPerSecond;
            }
            #if UNITY_EDITOR
            audioSource.audioSource.gameObject.name = string.Format("WAV{0:000} {1}", id, debugName);
            #endif
            changingAudioSource.Remove(audioSource.audioSource);
        }

        void RecycleInUseAudioSources() {
            var unusedAudioSources = new HashSet<InUseAudioSource>();
            foreach(var kv in audioSourceIdMapping)
                if(kv.Value.audioSource.time >= kv.Value.sliceEnd)
                    kv.Value.audioSource.Stop();
            foreach(var audioSource in inUseAudioSources.Keys)
                if(!audioSource.audioSource.isPlaying && !changingAudioSource.Contains(audioSource.audioSource))
                    unusedAudioSources.Add(audioSource);
            foreach(var audioSource in unusedAudioSources) {
                inUseAudioSources.Remove(audioSource);
                audioSourceIdMapping.Remove(audioSource.id);
                freeAudioSources.Enqueue(audioSource.audioSource);
#if UNITY_EDITOR
                audioSource.audioSource.gameObject.name = "-";
#endif
            }
        }
        
        void Update() {
            if(!isPaused) {
                RecycleInUseAudioSources();
            }
        }

        AudioSource GetFreeAudioSource(bool isPlayer) {
            AudioSource result;
            while(freeAudioSources.Count > 0) {
                result = freeAudioSources.Dequeue();
                if(result != null) {
                    SetMixerGroup(result, isPlayer);
                    return result;
                }
            }
            var go = new GameObject(string.Format("WAV Player"));
            go.transform.SetParent(transform, false);
            result = go.AddComponent<AudioSource>();
            result.dopplerLevel = 0;
            result.bypassEffects = true;
            result.bypassListenerEffects = true;
            result.bypassReverbZones = true;
            result.ignoreListenerVolume = true;
            result.loop = false;
            SetMixerGroup(result, isPlayer);
            return result;
        }

        void SetMixerGroup(AudioSource audioSource, bool isPlayer) {
            if(!isPlayer && mixerGroup != null)
                audioSource.outputAudioMixerGroup = mixerGroup;
            else if(isPlayer && playerMixerGroup != null)
                audioSource.outputAudioMixerGroup = playerMixerGroup;
        }
    }
}
