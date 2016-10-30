using UnityEngine;
using UnityObject = UnityEngine.Object;

using ManagedBass;

namespace BMS {
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
            else if(value is int) type = ResourceType.wav;
            else type = ResourceType.Unknown;
        }

        public string path {
            get { return resPath; }
        }

        public Texture texture {
            get { return value != null ? ((value as Texture) ?? (value as MovieTextureHolder).Output) : null; }
        }

        public int soundEffect {
            get { return (value is int) ? (int)value : 0; }
        }

        public void Dispose() {
            if(value is UnityObject) {
                if(Application.isPlaying)
                    UnityObject.Destroy(value as UnityObject);
                else
                    UnityObject.DestroyImmediate(value as UnityObject);
                return;
            }
            if(value is int) {
                if(!Bass.StreamFree((int)value))
                    Debug.LogErrorFormat("Failed to free stream {0} from BASS lib: {1}", value, Bass.LastError);
                return;
            }
        }
    }

    public struct BGAObject {
        public int index;
        public Rect clipArea;
        public Vector2 offset;
    }
}
