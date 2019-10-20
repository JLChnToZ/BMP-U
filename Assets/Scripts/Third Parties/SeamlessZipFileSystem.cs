using System;
using System.IO;
using SharpFileSystem.FileSystems;

namespace SharpFileSystem.SharpZipLib {
    public class SeamlessZipFileSystem: SeamlessArchiveFileSystem {
        private static readonly string[] archieveExtensions = new[] {
            ".zip",
        };

        public SeamlessZipFileSystem(IFileSystem fs): base(fs) { }

        protected override bool IsArchiveFile(IFileSystem fileSystem, FileSystemPath path) =>
            path.IsFile &&
            Array.IndexOf(archieveExtensions, path.GetExtension()) >= 0 &&
            !HasArchive(path);

        protected override IFileSystem CreateArchiveFileSystem(File archiveFile) =>
            SharpZipLibFileSystem.Open(archiveFile.FileSystem.OpenFile(archiveFile.Path, FileAccess.Read));
    }
}
