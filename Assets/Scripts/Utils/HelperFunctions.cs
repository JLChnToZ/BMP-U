using System;
using System.IO;
using System.Collections.Generic;
using UniRx.Async;
using SharpFileSystem;
using SharpFileSystem.FileSystems;
using SystemFile = System.IO.File;

namespace BananaBeats.Utils {
    public static class HelperFunctions {
        private static readonly Dictionary<FileSystemPath, FileSystemPath> tempFileMap = new Dictionary<FileSystemPath, FileSystemPath>();

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

        public static async UniTask<FileSystemPath> GetRealPathAsync(this IFileSystem fileSystem, FileSystemPath srcPath) {
            if(tempFileMap.TryGetValue(srcPath, out var destPath))
                return destPath;
            if(SystemFile.Exists(srcPath.ToString()))
                return srcPath;
            var tempFile = Path.GetTempFileName();
            using(var source = fileSystem.OpenFile(srcPath, FileAccess.Read))
            using(var dest = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write))
                await source.CopyToAsync(dest);
            tempFileMap[srcPath] = destPath = FileSystemPath.Parse(tempFile);
            return destPath;
        }

        public static bool IsReal(this FileSystemPath srcPath, IFileSystem fileSystem = null) {
            while(fileSystem is SeamlessArchiveFileSystem seamlessArchiveFS)
                fileSystem = seamlessArchiveFS.FileSystem;
            if(fileSystem is PhysicalFileSystem physicalFS)
                return physicalFS.Exists(srcPath);
            return SystemFile.Exists(srcPath.ToString());
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

        public static V GetOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue = default, bool autoAdd = false) {
            if(!dictionary.TryGetValue(key, out V value)) {
                value = defaultValue;
                if(autoAdd && !dictionary.IsReadOnly)
                    dictionary.Add(key, value);
            }
            return value;
        }

        public static V GetOrConstruct<K, V>(this IDictionary<K, V> dictionary, K key, bool autoAdd = false) where V : new() {
            if(!dictionary.TryGetValue(key, out V value)) {
                value = new V();
                if(autoAdd && !dictionary.IsReadOnly)
                    dictionary.Add(key, value);
            }
            return value;
        }

        public static string FixPathRoot(string path) {
            if(!Path.IsPathRooted(path))
                return path;
            var pathRoot = Path.GetPathRoot(path);
            return $"/{path.Substring(pathRoot.Length).Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator)}";
        }
    }
}
