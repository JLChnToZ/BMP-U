using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UniRx.Async;
using BMS;
using BananaBeats.Utils;
using Ude;

namespace BananaBeats {
    public class BMSLoader: IDisposable {
        private static string rootDataPath;

        private readonly string path;
        private readonly Dictionary<int, ImageResource> bmp = new Dictionary<int, ImageResource>();
        private readonly Dictionary<int, AudioResource> wav = new Dictionary<int, AudioResource>();
        private bool bmpLoaded, wavLoaded;
        private UniTask bmpLoader, wavLoader;

        public VirtualFS VirtualFS { get; private set; }

        public Chart Chart { get; private set; }

        public BMSLoader(string path, VirtualFS virtualFS = null) {
            VirtualFS = virtualFS ?? new VirtualFS();
            if(string.IsNullOrEmpty(rootDataPath))
                rootDataPath = Path.Combine(Application.dataPath, "..");
            path = Path.Combine(rootDataPath, path);
            this.path = Path.GetDirectoryName(path);
            using(var stream = VirtualFS[path].Stream) {
                var detector = new CharsetDetector();
                detector.Feed(stream);
                detector.DataEnd();
                stream.Seek(0, SeekOrigin.Begin);
                using(var reader = new StreamReader(stream, string.IsNullOrEmpty(detector.Charset) ? Encoding.Default : Encoding.GetEncoding(detector.Charset))) {
                    var ext = Path.GetExtension(path).ToLower();
                    switch(ext) {
                        case ".bms":
                        case ".bme":
                        case ".bml":
                        case ".pms":
                            Chart = new BMSChart(reader.ReadToEnd());
                            break;
                        case ".bmson":
                            Chart = new BmsonChart(reader.ReadToEnd());
                            break;
                        default:
                            throw new InvalidDataException($"Unknown format {ext}");
                    }
                }
            }
        }

        public void UnloadAll() {
            if(bmp.Count > 0) {
                foreach(var res in bmp.Values)
                    res.Dispose();
                bmp.Clear();
            }
            if(wav.Count > 0) {
                foreach(var res in wav.Values)
                    res.Dispose();
                wav.Clear();
            }
        }

        public UniTask LoadImages() {
            if(bmpLoaded) return bmpLoader;
            bmpLoaded = true;
            return bmpLoader = LoadImagesAsync();
        }

        private async UniTask LoadImagesAsync() {
            foreach(var resData in Chart.IterateResourceData(ResourceType.bmp))
                await LoadSingleImage(resData);
        }

        private async UniTask<ImageResource> LoadSingleImage(BMSResourceData resData) {
            if(!bmp.TryGetValue((int)resData.resourceId, out var res)) {
                var path = Path.Combine(this.path, resData.dataPath);
                var entry = VirtualFS[path];
                if(entry == null) return null;
                switch(Path.GetExtension(resData.dataPath).ToLower()) {
                    case ".jpg":
                    case ".jpe":
                    case ".jpeg":
                    case ".png":
                        res = new ImageResource(resData, entry);
                        break;
                    case ".bmp":
                        res = new BMPImageSource(resData, entry);
                        break;
                    default:
                        res = new VideoImageResource(resData, entry);
                        break;
                }
                bmp[(int)resData.resourceId] = res;
            }
            try {
                await res.Load();
            } catch(Exception ex) {
                Debug.LogError($"Error while loading {resData.resourceId} {resData.dataPath}");
                Debug.LogException(ex);
            }
            return res;
        }

        public UniTask LoadAudio() {
            if(wavLoaded) return wavLoader;
            wavLoaded = true;
            return wavLoader = LoadAudioAsync();
        }

        private async UniTask LoadAudioAsync() {
            foreach(var resData in Chart.IterateResourceData(ResourceType.wav)) {
                if(!wav.TryGetValue((int)resData.resourceId, out var res)) {
                    var path = Path.Combine(this.path, resData.dataPath);
                    var entry = VirtualFS[path];
                    if(entry == null) {
                        var extLan = Path.GetExtension(path).Length;
                        var glob = path.Substring(0, path.Length - extLan) + ".*";
                        Debug.LogWarning($"Resource not found, try use pattern {glob}");
                        entry = VirtualFS.Find(glob);
                        if(entry == null) {
                            Debug.LogWarning($"Resource not found: {path}");
                            continue;
                        }
                        Debug.LogWarning($"Matched file: {entry.FullPath}");
                    }
                    wav[(int)resData.resourceId] = res = new AudioResource(resData, entry);
                }
                try {
                    await res.Load();
                } catch(Exception ex) {
                    Debug.LogError($"Error while loading {resData.resourceId} {resData.dataPath}");
                    Debug.LogException(ex);
                }
            }
        }

        public UniTask<ImageResource> GetStageImage() =>
            Chart.TryGetResourceData(ResourceType.bmp, -1, out var resData) ?
                LoadSingleImage(resData) :
                UniTask.FromResult<ImageResource>(default);

        public UniTask<ImageResource> GetBannerImage() =>
            Chart.TryGetResourceData(ResourceType.bmp, -2, out var resData) ?
                LoadSingleImage(resData) :
                UniTask.FromResult<ImageResource>(default);

        public bool TryGetBMP(int id, out ImageResource result) =>
            bmp.TryGetValue(id, out result);

        public bool TryGetWAV(int id, out AudioResource result) =>
            wav.TryGetValue(id, out result);

        public bool TryGetBGA(int id, out CroppedImageResource result) {
            if(Chart.TryGetResourceData(ResourceType.bga, id, out var data)) {
                result = new CroppedImageResource(data, this);
                return true;
            }
            result = default;
            return false;
        }

        public void Dispose() {
            UnloadAll();
        }
    }
}
