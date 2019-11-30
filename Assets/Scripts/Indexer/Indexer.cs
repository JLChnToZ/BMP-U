using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UniRx.Async;
using BMS;
using BananaBeats.PlayerData;
using BananaBeats.Utils;
using SharpFileSystem;
using SharpFileSystem.SharpZipLib;

using SystemFile = System.IO.File;

namespace BananaBeats {
    public class Indexer: IDisposable {
        public readonly IFileSystem fileSystem;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly List<BMSLoader> loadedLoaders = new List<BMSLoader>();
        private FileSystemPath path;

        public string CurrentDirectory => path.EntityName;

        public string FullPath => path.ToString();

        public Indexer() {
            fileSystem = new SeamlessZipFileSystem(FilsSystemHelper.DefaultFileSystem);
            path = FileSystemPath.Root;
            fileSystemWatcher = new FileSystemWatcher(FilsSystemHelper.AppPath) {
                IncludeSubdirectories = true,
            };
            fileSystemWatcher.Created += OnFileChanged;
            fileSystemWatcher.Changed += OnFileChanged;
            fileSystemWatcher.Deleted += OnFileDeleted;
        }

        private void OnFileChanged(object _, FileSystemEventArgs e) {
            var path = FileSystemPath.Root.AppendPath(FilsSystemHelper.GetRelativePathFromRoot(e.FullPath));
            if((SystemFile.GetAttributes(e.FullPath) & FileAttributes.Directory) == FileAttributes.Directory)
                Scan(path).Forget();
            else
                ScanSingleFile(path);
        }

        private void OnFileDeleted(object _, FileSystemEventArgs e) {
            var path = FileSystemPath.Root.AppendPath(FilsSystemHelper.GetRelativePathFromRoot(e.FullPath));
            PlayerDataManager.Instance.ClearSongInfo(path.ToString());
        }

        public UniTask Scan(IProgress<string> fileProgress = null) =>
            Scan(path, fileProgress);

        private async UniTask Scan(FileSystemPath path, IProgress<string> fileProgress = null) {
            await UniTask.SwitchToTaskPool();
            PlayerDataManager.Instance.ClearSongInfo(path.ToString());
            foreach(var filePath in fileSystem.GetEntitiesRecursive(path).Where(TypeFilter)) {
                if(filePath.IsDirectory) continue;
                fileProgress?.ReportInMainThread(filePath.ToString());
                ScanSingleFile(path);
            }
            await UniTask.SwitchToMainThread();
        }

        private void ScanSingleFile(FileSystemPath path) {
            try {
                using(var bmsFile = new BMSLoader(path, fileSystem)) {
                    var strPath = path.ToString();
                    bmsFile.Chart.Parse(ParseType.Header);
                    PlayerDataManager.Instance.UpdateSongInfo(strPath, bmsFile.Chart);
                }
            } catch { }
        }

        public IEnumerable<FileSystemPath> List() =>
            fileSystem.GetEntities(path).Where(TypeFilter);

        public void Navigate(string dir) =>
            path = path.AppendDirectory(dir);

        public void Upward() =>
            path = path.ParentPath;

        private static bool TypeFilter(FileSystemPath path) {
            if(path.IsDirectory)
                return true;
            switch(path.GetExtension()) {
                case ".bms":
                case ".bme":
                case ".bml":
                case ".pms":
                case ".bmson":
                    return true;
                default:
                    return false;
            }
        }

        public void Dispose() {
            ClearLoaders();
            fileSystemWatcher?.Dispose();
            fileSystem?.Dispose();
        }

        private void ClearLoaders() {
            if(loadedLoaders.Count <= 0)
                return;
            foreach(var loader in loadedLoaders)
                loader.Dispose();
            loadedLoaders.Clear();
        }
    }
}
