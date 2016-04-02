using UnityEngine;

namespace BMS {
    public enum ResourceType {
        Unknown,
        bmp,
        wav
    }

    public class ResourceObject {
        readonly int index;
        internal readonly ResourceType type;
        string resPath;
        internal object value;

        public ResourceObject(int index, ResourceType type, string resPath) {
            this.index = index;
            this.type = type;
            this.resPath = resPath;
        }

        public ResourceObject(int index, object value) {
            this.index = index;
            this.value = value;
            if(value is MovieTextureHolder) type = ResourceType.bmp;
            else if(value is Texture) type = ResourceType.bmp;
            else if(value is AudioClip) type = ResourceType.wav;
            else type = ResourceType.Unknown;
        }

        public string path {
            get { return resPath; }
        }

        public Texture texture {
            get { return value != null ? ((value as Texture) ?? (value as MovieTextureHolder).Output) : null; }
        }

        public AudioClip soundEffect {
            get { return value as AudioClip; }
        }
    }

    public struct BGAObject {
        public int index;
        public Rect clipArea;
        public Vector2 offset;
    }
}
