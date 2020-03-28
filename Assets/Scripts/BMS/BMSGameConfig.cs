using System;
using BMS;

namespace BananaBeats {
    [Serializable]
    public struct BMSGameConfig {
        public bool loadImages;
        public float backgroundDim;
        public bool loadSounds;
        public float detune;
        public float offset;
        public float speed;
        public bool bpmAffectSpeed;
        public bool autoPlay;
        public BMSKeyLayout playableChannels;

        public static BMSGameConfig Default { get; } = new BMSGameConfig {
            loadImages = true,
            backgroundDim = 0.8F,
            loadSounds = true,
            detune = 0.5F,
            offset = 0,
            speed = 1,
            bpmAffectSpeed = true,
            autoPlay = false,
            playableChannels = (BMSKeyLayout)0x3FFFF,
        };
    }
}
