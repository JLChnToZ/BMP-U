using System;
using System.IO;
using System.Collections.Generic;

namespace BananaBeats.Utils {
    public class VirtualFS: IDisposable {
        private readonly Dictionary<string, IVirtualFSEntry> vfs =
            new Dictionary<string, IVirtualFSEntry>();

        public IVirtualFSEntry this[string path] {
            get {
                if(vfs.TryGetValue(path, out IVirtualFSEntry entry))
                    return entry;
                return Prepare(path);
            }
        }

        public VirtualFS() {}

        private IVirtualFSEntry Prepare(string path) {
            var pathSplit = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            IVirtualFSEntry entry = new SystemFSEntry(pathSplit[0]);
            AddIfNotExists(entry);
            for(int i = 1; i < pathSplit.Length; i++) {
                if(entry == null) return null;
                AddIfNotExists(entry = entry[pathSplit[i]]);
            }
            return entry;
        }

        public IVirtualFSEntry Find(string path) {
            var pathSplit = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            IVirtualFSEntry entry = new SystemFSEntry(pathSplit[0]);
            AddIfNotExists(entry);
            for(int i = 1; i < pathSplit.Length; i++) {
                if(entry == null) return null;
                var subEntry = entry[pathSplit[i]];
                if(subEntry == null)
                    subEntry = entry.Find(pathSplit[i]);
                AddIfNotExists(entry = subEntry);
            }
            return entry;
        }

        private bool AddIfNotExists(IVirtualFSEntry entry) {
            if(entry == null)
                return false;
            var fullPath = entry.FullPath;
            if(vfs.ContainsKey(fullPath))
                return false;
            vfs.Add(fullPath, entry);
            return true;
        }

        internal static IVirtualFSEntry GetSpecialDirectoryEntry(string path, Stream stream) {
            if(Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return new ZipFSEntry(path, stream);
            return null;
        }

        public void Dispose() {
            if(vfs.Count <= 0) return;
            foreach(var entry in vfs.Values)
                entry.Dispose();
            vfs.Clear();
        }
    }
}
