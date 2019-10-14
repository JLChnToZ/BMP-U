using System;
using System.IO;
using System.Collections.Generic;
using UniRx.Async;

namespace BananaBeats.Utils {
    public static class HelperFunctions {
        private static readonly Dictionary<string, string> tempFileMap = new Dictionary<string, string>();

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

        public static async UniTask<byte[]> ReadAllBytesAsync(this IVirtualFSEntry fileEntry) {
            if(fileEntry.IsReal)
                return File.ReadAllBytes(fileEntry.FullPath);
            using(var stream = fileEntry.Stream)
                return await ReadAllBytesAsync(stream);
        }

        public static async UniTask<string> GetRealPathAsync(this IVirtualFSEntry fileEntry) {
            var path = fileEntry.FullPath;
            if(fileEntry.IsReal)
                return path;
            if(!tempFileMap.TryGetValue(path, out var tempFileName) || !File.Exists(tempFileName)) {
                using(var fileStream = fileEntry.Stream) {
                    if(fileStream == null)
                        throw new NullReferenceException("File entry returns a null stream");
                    if(!fileStream.CanRead)
                        throw new InvalidOperationException("Cannot read the stream");
                    if(string.IsNullOrEmpty(tempFileName))
                        tempFileName = Path.GetTempFileName();
                    var tempFileInfo = new FileInfo(tempFileName);
                    if(!tempFileInfo.Exists || !fileStream.CanSeek || fileStream.Length != tempFileInfo.Length)
                        using(var tempFileStream = new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.Write))
                            await fileStream.CopyToAsync(tempFileStream);
                }
                tempFileMap[path] = tempFileName;
            }
            return tempFileName;
        }
    }
}
