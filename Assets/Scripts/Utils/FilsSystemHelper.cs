using System.IO;
using UnityEngine;
using SharpFileSystem;
using SharpFileSystem.FileSystems;

namespace BananaBeats.Utils {
    public static class FilsSystemHelper {
        private static bool inited;

        private static string appPath;
        public static string AppPath {
            get {
                Init();
                return appPath;
            }
        }

        private static FileSystemPath rootDataPath;
        public static FileSystemPath RootDataPath {
            get {
                Init();
                return rootDataPath;
            }
        }

        private static IFileSystem defaultFileSystem;
        public static IFileSystem DefaultFileSystem {
            get {
                Init();
                return defaultFileSystem;
            }
        }

        private static void Init() {
            if(inited) return;
            inited = true;
            var dataPath = Application.dataPath;
            rootDataPath = FileSystemPath.Root.Combine(HelperFunctions.FixPathRoot(dataPath)).ParentPath;
            var fileSystem = new PhysicalFileSystem(Path.GetPathRoot(dataPath));
            defaultFileSystem = fileSystem;
            appPath = fileSystem.GetPhysicalPath(rootDataPath);
        }
    }
}
