using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace BMS {
    public partial class BMSManager: MonoBehaviour {
        readonly Dictionary<int, ResourceObject> bmpObjects = new Dictionary<int, ResourceObject>();
        readonly Dictionary<int, BGAObject> bgaObjects = new Dictionary<int, BGAObject>();
        readonly Dictionary<int, ResourceObject> wavObjects = new Dictionary<int, ResourceObject>();

        void ClearDataObjects(bool clear, bool direct, bool clearMetaObjects) {
            if(!direct && reloadResourceCoroutine != null)
                StopCoroutine(reloadResourceCoroutine);
            foreach(var kv in bmpObjects) {
                if(clearMetaObjects || kv.Key > 0) {
                    kv.Value.Dispose();
                    if(!clear) kv.Value.value = null;
                }
            }
            foreach(var kv in wavObjects) {
                if(clearMetaObjects || kv.Key > 0) {
                    kv.Value.Dispose();
                    if(!clear) kv.Value.value = null;
                }
            }
            if(clear) {
                Debug.LogFormat("Cleared {0} objects.", bmpObjects.Count + wavObjects.Count);
                bmpObjects.Clear();
                wavObjects.Clear();
                bgaObjects.Clear();
            }
        }

        Coroutine reloadResourceCoroutine;
        void ReloadResources() {
            if(reloadResourceCoroutine != null) StopCoroutine(reloadResourceCoroutine);
            reloadResourceCoroutine = SmartCoroutineLoadBalancer.StartCoroutine(this, ReloadResourcesCoroutine(resourcePath), Time.maximumDeltaTime);
        }

        public bool IsLoadingResources {
            get { return reloadResourceCoroutine != null; }
        }

        int totalResources = 1;
        int loadedResources = 1;
        public float LoadResourceProgress {
            get { return (float)loadedResources / totalResources; }
        }

        IEnumerator ReloadResourcesCoroutine(string path) {
            var resLoader = new ResourceLoader(path);
            totalResources = wavObjects.Count;
            if(bgaEnabled) totalResources += bmpObjects.Count;
            loadedResources = 0;
            foreach(var wav in wavObjects.Values)
                yield return resLoader.LoadResource(wav, () => loadedResources++);
            if(bgaEnabled)
                foreach(var bmp in bmpObjects.Values)
                    yield return resLoader.LoadResource(bmp, () => loadedResources++);
            reloadResourceCoroutine = null;
            yield break;
        }

        ResourceObject GetDataObject(ResourceType type, int index, string resPath) {
            Dictionary<int, ResourceObject> dataObjectDict;
            ResourceObject result;
            bool reCreate = false;
            switch(type) {
                case ResourceType.wav: dataObjectDict = wavObjects; reCreate = true; break;
                case ResourceType.bmp: dataObjectDict = bmpObjects; break;
                default: return null;
            }
            if(dataObjectDict.TryGetValue(index, out result)) {
                if(result.path != resPath)
                    reCreate = true;
            } else
                reCreate = true;
            if(reCreate)
                dataObjectDict[index] = result = new ResourceObject(index, type, resPath);
            return result;
        }

        public Texture GetBMP(int id) {
            ResourceObject res;
            if(!bmpObjects.TryGetValue(id, out res))
                return null;
#if !UNITY_ANDROID
            if(res.texture is MovieTexture) {
                var movTexture = res.texture as MovieTexture;
                movTexture.Play();
                playingMovieTextures.Add(movTexture);
            } else if(res.value is MovieTextureHolder) {
#else
            if(res.value is MovieTextureHolder) {
#endif
                var movTH = res.value as MovieTextureHolder;
                movTH.Play();
                playingMovieTextureHolders.Add(movTH);
                if(movTH.Output == null)
                    return placeHolderTexture;
            }
            return res.texture;
        }

        public bool IsMovieBmp(int id) {
            ResourceObject res;
            return bmpObjects.TryGetValue(id, out res) && res != null && (res.value is MovieTextureHolder) && res.texture != null;
        }

        public int GetWAV(int id) {
            ResourceObject res;
            if(!wavObjects.TryGetValue(id, out res))
                return -1;
            return res.soundEffect;
        }

        public BGAObject GetBGA(int id) {
            BGAObject bga;
            if(!bgaObjects.TryGetValue(id, out bga))
                bga.index = -99;
            return bga;
        }
    }
}
