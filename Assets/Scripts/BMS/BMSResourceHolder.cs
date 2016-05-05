using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace BMS {
    public partial class BMSManager: MonoBehaviour {
        readonly Dictionary<int, ResourceObject> bmpObjects = new Dictionary<int, ResourceObject>();
        readonly Dictionary<int, BGAObject> bgaObjects = new Dictionary<int, BGAObject>();
        readonly Dictionary<int, ResourceObject> wavObjects = new Dictionary<int, ResourceObject>();

        void ClearDataObjects(bool clear, bool direct) {
            if(!direct && reloadResourceCoroutine != null)
                StopCoroutine(reloadResourceCoroutine);
            UnityObject obj;
            foreach(var bmp in bmpObjects.Values) {
                obj = bmp.value as UnityObject;
                if(obj != null) {
                    Destroy(obj);
                    if(!clear) bmp.value = null;
                }
            }
            foreach(var wav in wavObjects.Values) {
                obj = wav.value as UnityObject;
                if(obj != null) {
                    Destroy(obj);
                    if(!clear) wav.value = null;
                }
            }
            if(clear) {
                bmpObjects.Clear();
                wavObjects.Clear();
                bgaObjects.Clear();
            }
        }

        Coroutine reloadResourceCoroutine;
        void ReloadResources() {
            if(reloadResourceCoroutine != null) StopCoroutine(reloadResourceCoroutine);
            reloadResourceCoroutine = SmartCoroutineLoadBalancer.StartCoroutine(this, ReloadResourcesCoroutine(resourcePath));
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
            totalResources = wavObjects.Count + bmpObjects.Count;
            loadedResources = 0;
            int waitingLoaders = 0;
            System.Action onResLoaded = () => {
                loadedResources++;
                waitingLoaders--;
            };
            foreach(var wav in wavObjects.Values) {
                var route = resLoader.LoadResource(wav, onResLoaded);
                if(route != null) {
                    waitingLoaders++;
                    StartCoroutine(route);
                } else
                    loadedResources++;
                yield return null;
            }
            foreach(var bmp in bmpObjects.Values) {
                var route = resLoader.LoadResource(bmp, onResLoaded);
                if(route != null) {
                    waitingLoaders++;
                    StartCoroutine(route);
                } else
                    loadedResources++;
                yield return null;
            }
            while(waitingLoaders > 0)
                yield return SmartCoroutineLoadBalancer.ForceLoadYieldInstruction;
            reloadResourceCoroutine = null;
            yield break;
        }

        ResourceObject GetDataObject(ResourceType type, int index, string resPath) {
            Dictionary<int, ResourceObject> dataObjectDict;
            ResourceObject result;
            bool reCreate = false;
            switch(type) {
                case ResourceType.wav: dataObjectDict = wavObjects; break;
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
            if(res.texture is MovieTexture) {
                var movTexture = res.texture as MovieTexture;
                movTexture.Play();
                playingMovieTextures.Add(movTexture);
            } else if(res.value is MovieTextureHolder) {
                var movTH = res.value as MovieTextureHolder;
                movTH.StartCoroutine = StartCoroutine;
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

        public AudioClip GetWAV(int id) {
            ResourceObject res;
            if(!wavObjects.TryGetValue(id, out res))
                return null;
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
