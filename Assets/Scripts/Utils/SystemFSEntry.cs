using System.IO;
using System.Collections.Generic;

namespace BananaBeats.Utils {
    public class SystemFSEntry: IVirtualFSEntry {
        protected readonly Dictionary<string, IVirtualFSEntry> entries = new Dictionary<string, IVirtualFSEntry>();

        protected SystemFSEntry() { }

        public SystemFSEntry(string path) {
            if(path.EndsWith(Path.VolumeSeparatorChar.ToString()))
                path += Path.DirectorySeparatorChar;
            FullPath = Path.GetFullPath(path);
        }

        public virtual IVirtualFSEntry this[string path] {
            get {
                if(entries.TryGetValue(path, out IVirtualFSEntry result))
                    return result;
                var fullPath = Path.Combine(FullPath, path);
                if(!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    return null;
                var attr = File.GetAttributes(fullPath);
                if((attr & FileAttributes.Directory) != FileAttributes.Directory) {
                    var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    var entry = VirtualFS.GetSpecialDirectoryEntry(path, stream);
                    if(entry != null)
                        return entry;
                    stream.Dispose();
                }
                result = new SystemFSEntry(fullPath);
                entries.Add(path, result);
                return result;
            }
        }

        public virtual IVirtualFSEntry Find(string query) {
            foreach(var file in Directory.EnumerateFiles(FullPath, query)) {
                var result = this[Path.GetFileName(file)];
                if(result != null)
                    return result;
            }
            return null;
        }

        public virtual Stream Stream =>
            new FileStream(FullPath, FileMode.Open, FileAccess.Read);

        public virtual string FullPath { get; }

        public virtual string Name =>
            Path.GetFileName(FullPath);

        public virtual bool IsReal => true;

        public virtual void Dispose() {
            if(entries.Count <= 0) return;
            foreach(var value in entries.Values)
                value.Dispose();
            entries.Clear();
        }

    }
}
