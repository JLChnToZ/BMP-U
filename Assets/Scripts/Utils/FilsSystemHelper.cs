using System.IO;
using UnityEngine;
using SharpFileSystem;
using SharpFileSystem.FileSystems;

namespace BananaBeats.Utils {
    public static class FilsSystemHelper {
        public static FileSystemPath RootDataPath { get; private set; }
        public static IFileSystem DefaultFileSystem { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() {
            var dataPath = Application.dataPath;
            RootDataPath = FileSystemPath.Root.Combine(HelperFunctions.FixPathRoot(dataPath)).ParentPath;
            DefaultFileSystem = new PhysicalFileSystem(Path.GetPathRoot(dataPath));
        }
    }
}
