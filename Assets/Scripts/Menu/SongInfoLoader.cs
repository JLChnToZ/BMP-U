using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

using UnityEngine;

using BMS;

using Entry = SongInfoLoader.Entry;
using Ude;

public struct SongInfo: IEquatable<SongInfo>, IComparable<SongInfo> {
    public int index;
    public string filePath;
    public string name;
    public string artist;
    public string subArtist;
    public string genre;
    public string comments;
    public float level;
    public float bpm;
    public int notes;
    public Texture background;
    public string backgroundPath;
    public Texture banner;
    public string bannerPath;
    public string bmsHash;
    public BMSKeyLayout layout;

    public override bool Equals(object obj) {
        if(obj == null || !(obj is SongInfo))
            return false;
        return Equals((SongInfo)obj);
    }

    public bool Equals(SongInfo other) {
        return filePath == other.filePath && background == other.background && banner == other.banner;
    }

    public override int GetHashCode() {
        return filePath.GetHashCode() * 29;
    }

    public int CompareTo(SongInfo other) {
        return index.CompareTo(other.index);
    }

    public bool Exists {
        get { return !string.IsNullOrEmpty(filePath) && File.Exists(SongInfoLoader.GetAbsolutePath(filePath)); }
    }
    
    public string LayoutName {
        get {
            switch(layout) {
                case BMSKeyLayout.Single5Key: return "5-SP";
                case BMSKeyLayout.Single7Key: return "7-SP";
                case BMSKeyLayout.Single9Key:
                case BMSKeyLayout.Single9KeyAlt: return "9-SP";
                case BMSKeyLayout.Duel10Key: return "10-DP";
                case BMSKeyLayout.Duel14Key: return "14-DP";
                default: return "Custom";
            }
        }
    }
}

public static class SongInfoLoader {
    static string[] supportedFileTypes = new[] { "*.bms", "*.bme", "*.bml", "*.pms", "*.bmson" };
    public static ICollection<string> SupportedFileTypes {
        get {
            return new ReadOnlyCollection<string>(supportedFileTypes);
        }
    }

    static Encoding encoding = Encoding.Default;
    public static Encoding CurrentEncoding {
        get { return encoding; }
        set {
            encoding = value ?? Encoding.Default;
        }
    }

    public static int CurrentCodePage {
        get { return encoding.CodePage; }
        set {
            encoding = Encoding.GetEncoding(value) ?? Encoding.Default;
        }
    }

    static int index = 0;
    public static int GetNextIndex() {
        return index++;
    }

    static List<Entry> entries = new List<Entry>();
    static List<Entry> filteredEntries = entries;
    static Dictionary<string, Entry> cachedEntries = new Dictionary<string, Entry>();
    static Dictionary<string, Vector2> cachedScrollPosition = new Dictionary<string, Vector2>();
    static DirectoryInfo rootDiectory, currentDirectory;
    static string dirName;
    static string filterText = "", processedFilterText = "";
    static SongInfo? selectedEntry;

    static Coroutine loadResourceCoroutine;

    static Thread readDirectoryThread;
    static float loadedPercentage;
    static string dataPath;
    static BMSManager bmsManager;
    static bool ready;

    static SongInfoComparer.SortMode savedSortMode;

    public static event Action OnStartLoading;
    public static event Action OnListUpdated;
    public static event Action<SongInfo?> OnSelectionChanged;
    public static event Action OnRecursiveLoaded;

    public static bool IsReady {
        get { return ready; }
    }

    public static float LoadedPercentage {
        get { return loadedPercentage; }
    }

    public static bool HasLoadingThreadRunning {
        get { return readDirectoryThread != null && readDirectoryThread.IsAlive; }
    }

    public static SongInfoComparer.SortMode CurrentSortMode {
        get { return savedSortMode; }
        set {
            if(savedSortMode == value) return;
            savedSortMode = value;
            if(ready) Sort();
        }
    }

    public static DirectoryInfo CurrentDirectory {
        get { return currentDirectory; }
        set {
            if(value == null || string.Equals(currentDirectory.FullName, value.FullName, StringComparison.Ordinal))
                return;
            currentDirectory = value;
            dirName = value.FullName;
            ReloadDirectory();
        }
    }

    public static Vector2 ScrollPosition {
        get {
            if(dirName == null || !ready) return Vector2.up;
            Vector2 result;
            return cachedScrollPosition.TryGetValue(dirName, out result) ? result : Vector2.up;
        }
        set {
            if(dirName == null || !ready) return;
            cachedScrollPosition[dirName] = value;
        }
    }

    public static string FilterText {
        get { return filterText; }
        set {
            if(string.IsNullOrEmpty(value))
                value = "";
            else
                value = value.Trim();
            if(string.Equals(value, filterText, StringComparison.InvariantCultureIgnoreCase))
                return;
            filterText = value;
            if(ready) Sort();
        }
    }

    public static IList<Entry> Entries {
        get { return entries.AsReadOnly(); }
    }

    public static SongInfo? SelectedSong {
        get { return selectedEntry; }
        set {
            selectedEntry = value;

            if(value.HasValue)
                Loader.songPath = GetAbsolutePath(value.Value.filePath);
            else
                Loader.songPath = string.Empty;

            if(OnSelectionChanged != null)
                OnSelectionChanged.Invoke(value);
        }
    }

    public static bool IsRootDirectory {
        get {
            return currentDirectory == null ||
                string.Equals(currentDirectory.FullName, rootDiectory.FullName, StringComparison.Ordinal); 
        }
    }
    
    static SongInfoLoader() {
        GetDataPath();
    }

    static void GetDataPath() {
        if(string.IsNullOrEmpty(dataPath))
            dataPath = Application.dataPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        if(rootDiectory == null)
            rootDiectory = new DirectoryInfo(GetAbsolutePath("../BMS"));
        if(!rootDiectory.Exists)
            rootDiectory.Create();
        if(currentDirectory == null)
            currentDirectory = rootDiectory;
    }

    public static string GetAbsolutePath(string path) {
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(dataPath, path));
    }

    public static void SetBMSManager(BMSManager bmsManager) {
        SongInfoLoader.bmsManager = bmsManager;
    }

    public static string LoadFile(FileInfo fileInfo) {
        string result;
        Encoding encoding = CurrentEncoding;
        using(Stream stream = fileInfo.OpenRead()) {
            CharsetDetector detector = new CharsetDetector();
            detector.Feed(stream);
            detector.DataEnd();
            stream.Position = 0;
            if(detector.Charset != null) {
                Debug.LogFormat("Detected charset of file: {0}", detector.Charset);
                try {
                    encoding = Encoding.GetEncoding(detector.Charset);
                } catch {
                    Debug.LogWarning("Failed to load encoding, will use default encoding.");
                    encoding = CurrentEncoding;
                }
            } else {
                Debug.LogFormat("Failed to detect charset, will use default encoding.");
            }
            using(StreamReader reader = new StreamReader(stream, encoding))
                result = reader.ReadToEnd();
        }
        return result;
    }

    public static SongInfo LoadBMS(FileInfo file) {
        string bmsContent = LoadFile(file);
        bmsManager.LoadBMS(bmsContent, file.Directory.FullName, file.Extension, true);
        return new SongInfo {
            index = GetNextIndex(),
            filePath = HelperFunctions.MakeRelative(dataPath, file.FullName),
            name = bmsManager.Title,
            artist = bmsManager.Artist,
            subArtist = bmsManager.SubArtist,
            genre = bmsManager.Genre,
            bpm = bmsManager.BPM,
            notes = bmsManager.OriginalNotesCount,
            level = bmsManager.PlayLevel,
            comments = bmsManager.Comments,
            backgroundPath = bmsManager.StageFilePath,
            bannerPath = bmsManager.BannerFilePath,
            bmsHash = bmsManager.GetHash(CurrentEncoding, RecordsManager.Instance.HashAlgorithm),
            layout = bmsManager.OriginalLayout
        };
    }

    public static void ReloadDirectory() {
        AboartReadDirectory();
        ready = false;
        if(OnStartLoading != null)
            OnStartLoading.Invoke();
        ThreadHelper.InitThreadHandler();
        readDirectoryThread = new Thread(ReadDirectoryThread) {
            IsBackground = true
        };
        readDirectoryThread.Start();
    }

    public static void RecursiveLoadDirectory() {
        AboartReadDirectory();
        ready = false;
        if(OnStartLoading != null)
            OnStartLoading.Invoke();
        ThreadHelper.InitThreadHandler();
        readDirectoryThread = new Thread(RecursiveReadDirectoryThread) {
            IsBackground = true
        };
        readDirectoryThread.Start();
    }

    public static void AboartReadDirectory() {
        if(readDirectoryThread != null && readDirectoryThread.IsAlive)
            readDirectoryThread.Abort();
        readDirectoryThread = null;

        if(loadResourceCoroutine != null) {
            bmsManager.StopCoroutine(loadResourceCoroutine);
            loadResourceCoroutine = null;
        }
    }

    public static void RecursiveReadDirectoryThread() {
        try {
            List<Entry> directories = new List<Entry> {
                new Entry {
                    dirInfo = currentDirectory
                }
            }, childDirectories = new List<Entry>();
            List<string> bmsEntries = new List<string>();
            while(directories.Count > 0) {
                foreach(var dir in directories)
                    try {
                        foreach(var child in ReadDirectoryThreadInner(dir.dirInfo)) {
                            if(child.isParentDirectory) continue;
                            if(child.isDirectory) {
                                childDirectories.Add(child);
                                continue;
                            }
                            bmsEntries.Add(GetAbsolutePath(child.songInfo.filePath));
                        }
                    } catch (Exception ex) {
                        Debug.LogException(ex);
                    }
                directories.Clear();
                directories.AddRange(childDirectories);
                childDirectories.Clear();
            }
            Loader.songPaths = bmsEntries.ToArray();
            ready = true;
            ThreadHelper.RunInUnityThread(RecursiveLoaded);
        } catch(ThreadAbortException) {
        } catch(Exception ex) {
            Debug.LogException(ex);
        }
    }

    static IEnumerable<Entry> ReadDirectoryThreadInner(DirectoryInfo currentDirectory) {
        var supportedFileTypes = SupportedFileTypes;
        if(!string.Equals(currentDirectory.FullName, rootDiectory.FullName, StringComparison.Ordinal))
            yield return new Entry {
                isDirectory = true,
                isParentDirectory = true,
                dirInfo = currentDirectory,
                summary = ""
            };
        Entry current = new Entry();
        bool hasEntry = false;
        foreach(var dirInfo in currentDirectory.GetDirectories()) {
            hasEntry = false;
            try {
                if(supportedFileTypes.Any(filter => dirInfo.GetFiles(filter).Any()) ||
                    dirInfo.GetDirectories().Any()) {
                    current = new Entry {
                        isDirectory = true,
                        dirInfo = dirInfo,
                        summary = ""
                    };
                    hasEntry = true;
                }
            } catch(Exception ex) {
                Debug.LogException(ex);
            }
            if(hasEntry) yield return current;
        }
        foreach(var fileInfo in supportedFileTypes.SelectMany(filter => currentDirectory.GetFiles(filter))) {
            hasEntry = false;
            try {
                if(!(hasEntry = cachedEntries.TryGetValue(fileInfo.FullName, out current))) {
                    current = new Entry {
                        isDirectory = false,
                        songInfo = LoadBMS(fileInfo)
                    };
                    current.summary = string.Format("{0}\n{1}\n{2}\n{3}\n{4}",
                        current.songInfo.name,
                        current.songInfo.artist,
                        current.songInfo.subArtist,
                        current.songInfo.genre,
                        current.songInfo.comments
                    );
                    if(string.IsNullOrEmpty(current.songInfo.name))
                        current.songInfo.name = fileInfo.Name;
                    cachedEntries.Add(fileInfo.FullName, current);
                    hasEntry = true;
                }
            } catch(Exception ex) {
                Debug.LogException(ex);
            }
            if(hasEntry) yield return current;
        }
    }

    static void ReadDirectoryThread() {
        try {
            entries.Clear();
            entries.AddRange(ReadDirectoryThreadInner(currentDirectory));
            SortInThread();
            ready = true;
            ThreadHelper.RunInUnityThread(UpdateList);
        } catch(ThreadAbortException) {
        } catch(Exception ex) {
            Debug.LogException(ex);
        }
    }

    public static void Sort() {
        AboartReadDirectory();
        readDirectoryThread = new Thread(SortInThread) {
            IsBackground = true
        };
        readDirectoryThread.Start();
    }

    static void SortInThread() {
        bool _ready = ready;
        ready = false;
        if(!string.IsNullOrEmpty(filterText)) {
            if(string.Equals(processedFilterText, filterText, StringComparison.InvariantCultureIgnoreCase))
                filteredEntries = entries.FindAll(Filter);
        } else
            filteredEntries = entries;
        processedFilterText = filterText;
        filteredEntries.Sort(SongInfoComparer.GetComparer(savedSortMode));
        if(_ready) {
            ready = true;
            ThreadHelper.RunInUnityThread(InvokeListUpdated);
        }
    }

    static bool Filter(Entry entry) {
        return entry.isParentDirectory || entry.summary.IndexOf(filterText, StringComparison.InvariantCultureIgnoreCase) >= 0;
    }

    static void UpdateList() {
        loadResourceCoroutine = SmartCoroutineLoadBalancer.StartCoroutine(bmsManager, LoadResource());
        InvokeListUpdated();
        SelectedSong = null;
    }

    static void RecursiveLoaded() {
        if(OnRecursiveLoaded != null) OnRecursiveLoaded();
    }

    static void InvokeListUpdated() {
        if(OnListUpdated != null)
            OnListUpdated.Invoke();
    }

    static IEnumerator LoadResource() {
        for(int i = 0; i < entries.Count; i++) {
            Entry entry = entries[i];

            if(entry.isDirectory) {
                yield return null;
                continue;
            }

            if((entry.songInfo.background || string.IsNullOrEmpty(entry.songInfo.backgroundPath)) &&
                (entry.songInfo.banner || string.IsNullOrEmpty(entry.songInfo.bannerPath))) {
                yield return null;
                continue;
            }

            FileInfo fileInfo = new FileInfo(GetAbsolutePath(entry.songInfo.filePath));
            var resourceLoader = new ResourceLoader(fileInfo.Directory.FullName);

            if(!entry.songInfo.background && !string.IsNullOrEmpty(entry.songInfo.backgroundPath)) {
                var backgroundObj = new ResourceObject(-1, ResourceType.bmp, entry.songInfo.backgroundPath);
                yield return SmartCoroutineLoadBalancer.StartCoroutine(bmsManager, resourceLoader.LoadResource(backgroundObj));
                entry.songInfo.background = backgroundObj.texture;
            }

            if(!entry.songInfo.banner && !string.IsNullOrEmpty(entry.songInfo.bannerPath)) {
                var bannerObj = new ResourceObject(-2, ResourceType.bmp, entry.songInfo.bannerPath);
                yield return SmartCoroutineLoadBalancer.StartCoroutine(bmsManager, resourceLoader.LoadResource(bannerObj));
                entry.songInfo.banner = bannerObj.texture;
            }

            entries[i] = entry;
            cachedEntries[fileInfo.FullName] = entry;

            if(selectedEntry.HasValue &&
                string.Equals(entry.songInfo.filePath, selectedEntry.Value.filePath, StringComparison.Ordinal)) {
                selectedEntry = entry.songInfo;
                if(OnSelectionChanged != null)
                    OnSelectionChanged.Invoke(selectedEntry);
            }

            InvokeListUpdated();
            yield return null;
        }
        loadResourceCoroutine = null;
        yield break;
    }

    public struct Entry {
        public bool isDirectory, isParentDirectory;
        public string summary;
        public SongInfo songInfo;
        public DirectoryInfo dirInfo;
    }
}

public static class SongInfoComparer {
    public static int CompareByName(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, string.Compare(lhs.songInfo.name, rhs.songInfo.name, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int CompareByNameInverse(Entry lhs, Entry rhs) {
        return FinalCompare(ref rhs, ref lhs, string.Compare(rhs.songInfo.name, lhs.songInfo.name, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int CompareByArtist(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, string.Compare(lhs.songInfo.artist, rhs.songInfo.artist, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int CompareByArtistInverse(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, string.Compare(rhs.songInfo.artist, lhs.songInfo.artist, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int CompareByGenre(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, string.Compare(lhs.songInfo.genre, rhs.songInfo.genre, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int CompareByGenreInverse(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, string.Compare(rhs.songInfo.genre, lhs.songInfo.genre, StringComparison.InvariantCultureIgnoreCase));
    }

    public static int CompareByBPM(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, lhs.songInfo.bpm.CompareTo(rhs.songInfo.bpm));
    }

    public static int CompareByBPMInverse(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, rhs.songInfo.bpm.CompareTo(lhs.songInfo.bpm));
    }

    public static int CompareByLevel(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, lhs.songInfo.level.CompareTo(rhs.songInfo.level));
    }

    public static int CompareByLevelInverse(Entry lhs, Entry rhs) {
        return FinalCompare(ref lhs, ref rhs, rhs.songInfo.level.CompareTo(lhs.songInfo.level));
    }

    static int FinalCompare(ref Entry lhs, ref Entry rhs, int val) {
        if(val != 0) return val;
        if(lhs.isParentDirectory || (lhs.isDirectory && !rhs.isDirectory)) return -1;
        if(rhs.isParentDirectory || (!lhs.isDirectory && rhs.isDirectory)) return 1;
        string lhsDisplay = lhs.isDirectory ? lhs.dirInfo.Name : lhs.songInfo.name;
        string rhsDisplay = rhs.isDirectory ? rhs.dirInfo.Name : rhs.songInfo.name;
        return string.Compare(lhsDisplay, rhsDisplay, StringComparison.InvariantCultureIgnoreCase);
    }

    public static Comparison<Entry> GetComparer(SortMode sortMode) {
        switch(sortMode) {
            case SortMode.Name: return CompareByName;
            case SortMode.NameInverse: return CompareByNameInverse;
            case SortMode.Artist: return CompareByArtist;
            case SortMode.ArtistInverse: return CompareByArtistInverse;
            case SortMode.Genre: return CompareByGenre;
            case SortMode.GenreInverse: return CompareByGenreInverse;
            case SortMode.BPM: return CompareByBPM;
            case SortMode.BPMInverse: return CompareByBPMInverse;
            case SortMode.Level: return CompareByLevel;
            case SortMode.LevelInverse: return CompareByLevelInverse;
            default: return null;
        }
    }

    public enum SortMode {
        Name, NameInverse,
        Artist, ArtistInverse,
        Genre, GenreInverse,
        BPM, BPMInverse,
        Level, LevelInverse
    }
}
