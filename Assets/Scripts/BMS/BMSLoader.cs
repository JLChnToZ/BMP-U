using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UniRx.Async;
using BananaBeats.Utils;
using BMS;
using Ude;
using SharpFileSystem;
using SharpFileSystem.SharpZipLib;

namespace BananaBeats {
    public class BMSLoader: IDisposable {
        private readonly FileSystemPath path;
        private readonly Dictionary<int, ImageResource> bmp = new Dictionary<int, ImageResource>();
        private readonly Dictionary<int, AudioResource> wav = new Dictionary<int, AudioResource>();
        private readonly bool customFileSystem;
        private bool bmpLoaded, wavLoaded;
        private UniTask bmpLoader, wavLoader;

        public IFileSystem FileSystem { get; private set; }

        public Chart Chart { get; private set; }

        public BMSLoader(string path, IFileSystem fileSystem = null) :
            this(FilsSystemHelper.RootDataPath.Combine(HelperFunctions.FixPathRoot(path)), fileSystem) {}

        public BMSLoader(FileSystemPath path, IFileSystem fileSystem = null) {
            customFileSystem = fileSystem != null;
            FileSystem = customFileSystem ? fileSystem : new SeamlessZipFileSystem(FilsSystemHelper.DefaultFileSystem);
            this.path = path.ParentPath;
            using(var stream = FileSystem.OpenRandomAccessFile(path, FileAccess.Read)) {
                var detector = new CharsetDetector();
                detector.Feed(stream);
                detector.DataEnd();
                stream.Seek(0, SeekOrigin.Begin);
                Encoding encoding;
                try {
                    if(string.IsNullOrEmpty(detector.Charset))
                        encoding = Encoding.GetEncoding(932);
                    else
                        encoding = Encoding.GetEncoding(detector.Charset);
                } catch {
                    encoding = Encoding.Default;
                }
                using(var reader = new StreamReader(stream, encoding)) {
                    var ext = path.GetExtension();
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

        public UniTask LoadImages(IProgress<float> progress = null) {
            if(bmpLoaded) return bmpLoader;
            bmpLoaded = true;
            return bmpLoader = PrepareAndLoadImagesAsync(progress);
        }

        private async UniTask PrepareAndLoadImagesAsync(IProgress<float> progress = null) {
            PrepareLoadImages();
            await LoadResourceAsync(bmp.Values, progress);
        }

        private async UniTask<ImageResource> LoadSingleImage(BMSResourceData resData) {
            var res = PrepareLoadSingleImage(resData);
            try {
                await res.Load();
            } catch(Exception ex) {
                Debug.LogError($"Error while loading {resData.resourceId} {resData.dataPath}");
                Debug.LogException(ex);
            }
            await UniTask.SwitchToMainThread();
            return res;
        }

        public void PrepareLoadImages() {
            foreach(var resData in Chart.IterateResourceData(ResourceType.bmp))
                PrepareLoadSingleImage(resData);
        }

        private ImageResource PrepareLoadSingleImage(BMSResourceData resData) {
            if(!bmp.TryGetValue((int)resData.resourceId, out var res) || res.ResourceData.dataPath != resData.dataPath) {
                if(res != null) res.Dispose();
                if(string.IsNullOrEmpty(resData.dataPath)) return null;
                var path = this.path.Combine(resData.dataPath);
                if(!FileSystem.Exists(path)) return null;
                switch(Path.GetExtension(resData.dataPath).ToLower()) {
                    case ".jpg":
                    case ".jpe":
                    case ".jpeg":
                    case ".png":
                        res = new ImageResource(resData, FileSystem, path);
                        break;
                    case ".bmp":
                    case ".gif":
                    case ".tga":
                    case ".psd":
                        res = new BMPImageSource(resData, FileSystem, path);
                        break;
                    default:
                        res = new VideoImageResource(resData, FileSystem, path);
                        break;
                }
                bmp[(int)resData.resourceId] = res;
            }
            return res;
        }

        public UniTask LoadAudio(IProgress<float> progress = null) {
            if(wavLoaded) return wavLoader;
            wavLoaded = true;
            return wavLoader = PrepareAndLoadAudioAsync(progress);
        }

        private async UniTask PrepareAndLoadAudioAsync(IProgress<float> progress = null) {
            PrepareLoadAudio();
            await LoadResourceAsync(wav.Values, progress);
            await UniTask.SwitchToMainThread();
        }

        public void PrepareLoadAudio() {
            HashSet<FileSystemPath> checkedPaths = null;
            IDictionary<FileSystemPath, FileSystemPath> cache = null;
            foreach(var resData in Chart.IterateResourceData(ResourceType.wav)) {
                if(!wav.TryGetValue((int)resData.resourceId, out var res) || res.ResourceData.dataPath != resData.dataPath) {
                    if(res != null) res.Dispose();
                    if(string.IsNullOrEmpty(resData.dataPath)) continue;
                    var path = this.path.Combine(resData.dataPath);
                    if(!FileSystem.Exists(path)) {
                        var parent = path.ParentPath;
                        if(checkedPaths == null)
                            checkedPaths = new HashSet<FileSystemPath>();
                        if(checkedPaths.Add(parent))
                            cache = HelperFunctions.GetCacheDictForMatchingNames(FileSystem, parent, cache);
                        if(cache.TryGetValue(parent.AppendFile(path.GetFileNameWithoutExtension()), out var matchedPath))
                            path = matchedPath;
                    }
                    wav[(int)resData.resourceId] = res = new AudioResource(resData, FileSystem, path);
                }
            }
        }

        private async UniTask LoadResourceAsync<T>(ICollection<T> collection, IProgress<float> progress = null) where T : BMSResource {
            int i = 0;
            float countf = collection.Count;
            foreach(var res in collection) {
                try {
                    await res.Load();
                } catch(Exception ex) {
                    var resData = res.ResourceData;
                    Debug.LogError($"Error while loading {resData.resourceId} {resData.dataPath}");
                    Debug.LogException(ex);
                } finally {
                    await UniTask.SwitchToMainThread();
                }
                progress?.Report(++i / countf);
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
            if(!customFileSystem)
                FileSystem.Dispose();
        }
    }
}
