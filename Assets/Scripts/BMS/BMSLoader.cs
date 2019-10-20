﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UniRx.Async;
using BananaBeats.Utils;
using BMS;
using Ude;
using SharpFileSystem;
using SharpFileSystem.FileSystems;
using SharpFileSystem.SharpZipLib;

namespace BananaBeats {
    public class BMSLoader: IDisposable {
        private static bool isRootPathCreated;
        private static FileSystemPath rootDataPath;
        private static IFileSystem defaultFS;

        private readonly FileSystemPath path;
        private readonly Dictionary<int, ImageResource> bmp = new Dictionary<int, ImageResource>();
        private readonly Dictionary<int, AudioResource> wav = new Dictionary<int, AudioResource>();
        private bool bmpLoaded, wavLoaded;
        private UniTask bmpLoader, wavLoader;

        public IFileSystem FileSystem { get; private set; }

        public Chart Chart { get; private set; }

        public BMSLoader(string path, IFileSystem fileSystem = null) {
            if(defaultFS == null) defaultFS = new PhysicalFileSystem(Path.GetPathRoot(Application.dataPath));
            FileSystem = fileSystem ?? new SeamlessZipFileSystem(defaultFS);
            if(!isRootPathCreated) {
                rootDataPath = FileSystemPath.Root.Combine(HelperFunctions.FixPathRoot(Application.dataPath)).ParentPath;
                isRootPathCreated = true;
            }
            var parsedPath = rootDataPath.Combine(HelperFunctions.FixPathRoot(path));
            this.path = parsedPath.ParentPath;
            using(var stream = FileSystem.OpenRandomAccessFile(parsedPath, FileAccess.Read)) {
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
                    var ext = parsedPath.GetExtension();
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
                        res = new BMPImageSource(resData, FileSystem, path);
                        break;
                    default:
                        res = new VideoImageResource(resData, FileSystem, path);
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
                    var path = this.path.Combine(resData.dataPath);
                    if(!FileSystem.Exists(path)) {
                        bool hasPath = false;
                        var name = path.GetFileNameWithoutExtension();
                        foreach(var entry in FileSystem.GetEntities(path.ParentPath))
                            if(entry.IsFile && name.Equals(entry.GetFileNameWithoutExtension(), StringComparison.Ordinal)) {
                                hasPath = true;
                                path = entry;
                                break;
                            }
                        if(!hasPath)
                            continue;
                    }
                    wav[(int)resData.resourceId] = res = new AudioResource(resData, FileSystem, path);
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
