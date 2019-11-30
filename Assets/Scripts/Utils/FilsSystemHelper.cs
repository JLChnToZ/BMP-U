using System;
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
        private static Uri appPathUri;

        private static FileSystemPath rootDataPath;
        public static FileSystemPath RootDataPath {
            get {
                Init();
                return rootDataPath;
            }
        }


        private static PhysicalFileSystem defaultFileSystem;
        public static PhysicalFileSystem DefaultFileSystem {
            get {
                Init();
                return defaultFileSystem;
            }
        }

        private static void Init() {
            if(inited) return;
            inited = true;
            var dataPath = Path.Combine(Application.dataPath, "..");
            rootDataPath = FileSystemPath.Root.Combine(HelperFunctions.FixPathRoot(dataPath));
            appPathUri = new Uri(dataPath);
            defaultFileSystem = new PhysicalFileSystem(Path.GetPathRoot(dataPath));
            appPath = defaultFileSystem.GetPhysicalPath(rootDataPath);
        }

        public static string GetRelativePathFromRoot(string path) =>
            new Uri(path).MakeRelativeUri(appPathUri).ToString();
    }
}
