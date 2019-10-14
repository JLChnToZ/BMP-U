using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using GlobExpressions;

namespace BananaBeats.Utils {
    public class ZipFSEntry: SystemFSEntry {
        private readonly string zipPath;
        private readonly Stream undelyStream;
        public readonly ZipFile zipFile;
        public readonly ZipEntry zipEntry;

        public ZipFSEntry(string path, Stream undelyStream = null) {
            this.undelyStream = undelyStream ?? new FileStream(path, FileMode.Open, FileAccess.Read);
            zipPath = path;
            zipFile = new ZipFile(this.undelyStream, true);
        }

        protected ZipFSEntry(string zipPath, Stream undelyStream, ZipFile zipFile, ZipEntry zipEntry) {
            this.zipPath = zipPath;
            this.undelyStream = undelyStream;
            this.zipFile = zipFile;
            this.zipEntry = zipEntry;
        }

        public override IVirtualFSEntry this[string path] {
            get {
                if(entries.TryGetValue(path, out IVirtualFSEntry result))
                    return result;
                if(zipEntry != null)
                    path = Path.Combine(zipEntry.Name, path);
                int index = zipFile.FindEntry(path, true);
                if(index < 0)
                    return null;
                return AddEntry(path, zipFile[index]);
            }
        }

        private IVirtualFSEntry AddEntry(string path, ZipEntry zipEntry) {
            if(zipEntry.IsFile) {
                var stream = zipFile.GetInputStream(zipEntry);
                var entry = VirtualFS.GetSpecialDirectoryEntry(path, stream);
                if(entry != null)
                    return entry;
                stream.Dispose();
            }
            var result = new ZipFSEntry(zipPath, undelyStream, zipFile, zipEntry);
            entries.Add(path, result);
            return result;
        }

        public override IVirtualFSEntry Find(string query) {
            if(zipEntry != null)
                query = Path.Combine(zipEntry.Name, query);
            var glob = new Glob(query, GlobOptions.MatchFullPath);
            foreach(ZipEntry testEntry in zipFile)
                if(glob.IsMatch(testEntry.Name))
                    return AddEntry(Path.GetFileName(testEntry.Name), testEntry);
            return null;
        }

        public override Stream Stream =>
            zipEntry != null ? zipFile.GetInputStream(zipEntry) : undelyStream;

        public override string FullPath => zipEntry != null ?
            Path.Combine(zipPath, zipEntry.Name) : zipPath;

        public override string Name =>
            Path.GetFileName(zipEntry != null ? zipEntry.Name : zipPath);

        public override bool IsReal => false;

        public override void Dispose() {
            zipFile.Close();
            undelyStream.Dispose();
            base.Dispose();
        }
    }
}
