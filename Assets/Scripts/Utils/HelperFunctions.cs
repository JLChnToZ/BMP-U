﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx.Async;
using SharpFileSystem;
using SharpFileSystem.FileSystems;
using File = System.IO.File;

namespace BananaBeats.Utils {
    public static class HelperFunctions {
        private static readonly Dictionary<FileSystemPath, string> tempFileMap = new Dictionary<FileSystemPath, string>();

        public static async UniTask<byte[]> ReadAllBytesAsync(this Stream source) {
            if(source == null)
                throw new ArgumentNullException(nameof(source));
            if(!source.CanRead)
                throw new InvalidOperationException("Cannot read this stream.");
            if(!(source is MemoryStream memoryStream)) {
                byte[] result;
                if(source.CanSeek) {
                    result = new byte[source.Length];
                    memoryStream = new MemoryStream(result);
                } else {
                    result = null;
                    memoryStream = new MemoryStream();
                }
                await source.CopyToAsync(memoryStream);
                if(result != null) return result;
            }
            return memoryStream.ToArray();
        }

        public static async UniTask<byte[]> ReadAllBytesAsync(this IFileSystem fileSystem, FileSystemPath path) {
            using(var stream = fileSystem.OpenFile(path, FileAccess.Read))
                return await ReadAllBytesAsync(stream);
        }

        public static IDictionary<FileSystemPath, FileSystemPath> GetCacheDictForMatchingNames(
            IFileSystem fileSystem,
            FileSystemPath basePath,
            IDictionary<FileSystemPath, FileSystemPath> dictionary = null) {
            if(dictionary == null) dictionary = new Dictionary<FileSystemPath, FileSystemPath>();
            foreach(var entry in fileSystem.GetEntities(basePath))
                if(entry.IsFile)
                    dictionary[basePath.AppendFile(entry.GetFileNameWithoutExtension())] = entry;
            return dictionary;
        }

        public static async UniTask<string> GetRealPathAsync(this IFileSystem fileSystem, FileSystemPath srcPath) {
            if(tempFileMap.TryGetValue(srcPath, out var destPath))
                return destPath;
            while(fileSystem is SeamlessArchiveFileSystem seamlessArchiveFS)
                fileSystem = seamlessArchiveFS.FileSystem;
            if(fileSystem is PhysicalFileSystem physicalFS && physicalFS.Exists(srcPath))
                return physicalFS.GetPhysicalPath(srcPath);
            var tempFile = Path.GetTempFileName();
            using(var source = fileSystem.OpenFile(srcPath, FileAccess.Read))
            using(var dest = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write))
                await source.CopyToAsync(dest);
            tempFileMap[srcPath] = destPath = tempFile;
            return destPath;
        }

        public static bool IsReal(this FileSystemPath srcPath, IFileSystem fileSystem = null) {
            while(fileSystem is SeamlessArchiveFileSystem seamlessArchiveFS)
                fileSystem = seamlessArchiveFS.FileSystem;
            if(fileSystem is PhysicalFileSystem physicalFS)
                return physicalFS.Exists(srcPath);
            return File.Exists(srcPath.ToString());
        }

        public static Stream OpenRandomAccessFile(this IFileSystem fileSystem, FileSystemPath path, FileAccess fileAccess) {
            var stream = fileSystem.OpenFile(path, fileAccess);
            if(stream.CanSeek) return stream;
            var copy = new MemoryStream();
            stream.CopyTo(copy);
            stream.Dispose();
            copy.Seek(0, SeekOrigin.Begin);
            return copy;
        }

        public static FileSystemPath Combine(this FileSystemPath path1, string path2) =>
            FileSystemPath.IsRooted(path2) ? FileSystemPath.Parse(path2) : path1.AppendPath(path2);

        public static FileSystemPath Combine(this FileSystemPath path1, FileSystemPath path2) =>
            FileSystemPath.IsRooted(path2.ToString()) ? path2 : path1.AppendPath(path2);

        public static string GetFileNameWithoutExtension(this FileSystemPath path) {
            var name = path.EntityName;
            return path.IsFile ? name.Substring(0, name.Length - path.GetExtension().Length) : name;
        }

        public static V GetOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue = default, bool autoAdd = false) =>
            GetOrCreate(dictionary, key, _ => defaultValue, autoAdd);

        public static V GetOrConstruct<K, V>(this IDictionary<K, V> dictionary, K key, bool autoAdd = true) where V : new() =>
            GetOrCreate(dictionary, key, Construct<K, V>, autoAdd);

        public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> crafter, bool autoAdd = true) {
            if(!dictionary.TryGetValue(key, out V value)) {
                value = crafter(key);
                if(autoAdd && !dictionary.IsReadOnly)
                    dictionary.Add(key, value);
            }
            return value;
        }

        private static V Construct<K, V>(K _) where V : new() => new V();

        public static string FixPathRoot(string path) {
            if(!Path.IsPathRooted(path))
                return path;
            var pathRoot = Path.GetPathRoot(path);
            return $"/{path.Substring(pathRoot.Length).Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator)}";
        }

        public static Vector2 SizeToParent(this RawImage image, float padding = 0) {
            float width = 0, height = 0;
            var transform = image.GetComponent<RectTransform>();
            var parent = transform.parent as RectTransform;
            var texture = image.texture;
            if(texture != null) {
                if(parent == null)
                    return transform.sizeDelta;
                padding = 1 - padding;
                float ratio = (float)texture.width / texture.height;
                var bounds = new Rect(Vector2.zero, parent.rect.size);
                if(Mathf.RoundToInt(transform.eulerAngles.z) % 180 == 90)
                    bounds.size = new Vector2(bounds.height, bounds.width);
                if(width > bounds.width * padding) {
                    width = bounds.width * padding;
                    height = width / ratio;
                } else {
                    height = bounds.height * padding;
                    width = height * ratio;
                }
            }
            transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return transform.sizeDelta;
        }

        public static void ReportInMainThread<T>(this IProgress<T> progress, T value) =>
            PlayerLoopHelper.AddContinuation(PlayerLoopTiming.Update, () => progress.Report(value));
    }
}
