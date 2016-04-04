using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading;

using DisruptorUnity3d;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

public struct SongInfo:IEquatable<SongInfo>, IComparable<SongInfo> {
    public int index;
    public string filePath;
    public string name;
    public string artist;
    public string subArtist;
    public string genre;
    public string comments;
    public float level;
    public float bpm;
    public Texture background;
    public string backgroundPath;

    public override bool Equals(object obj) {
        if(obj == null || !(obj is SongInfo))
            return false;
        return Equals((SongInfo)obj);
    }

    public bool Equals(SongInfo other) {
        return filePath == other.filePath && background == other.background;
    }

    public override int GetHashCode() {
        return filePath.GetHashCode() * 29;
    }

    public int CompareTo(SongInfo other) {
        return index.CompareTo(other.index);
    }

    public bool Exists {
        get { return !string.IsNullOrEmpty(filePath) && File.Exists(filePath); }
    }
}

static class SongInfoLoader {
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

    static List<SongInfo> cachedSongInfo = new List<SongInfo>();
    static HashSet<string> cachedSongInfoPaths = new HashSet<string>();

    static Thread loadBMSFilesThread;
    static Action<SongInfo> onAddSongInfo;
    static Action<IEnumerable<SongInfo>> onAddSongInfoCache;
    static float loadedPercentage;
    static string dataPath;
    static BMS.BMSManager bmsManager;
    static bool endOfCache = false, cacheLoaded = false, isRemoving = false;
    static RingBuffer<int> pendingRemoveSongInfos = new RingBuffer<int>(20);

    public static float LoadedPercentage {
        get { return loadedPercentage; }
    }

    public static bool HasLoadingThreadRunning {
        get { return loadBMSFilesThread != null && loadBMSFilesThread.IsAlive; }
    }

    public static void LoadCacheInThread() {
        if(cacheLoaded) return;
        StopLoadBMS();
        var loadCachethread = new Thread(CacheLoad) {
            Priority = ThreadPriority.BelowNormal
        };
        loadCachethread.Start();
    }

    public static void LoadBMSInThread(BMS.BMSManager manager, Action<IEnumerable<SongInfo>> loadCacheSongInfoCallback, Action<SongInfo> addSongInfoCallback) {
        LoadCacheInThread();
        StopLoadBMS();
        dataPath = Application.dataPath;
        onAddSongInfo = addSongInfoCallback;
        onAddSongInfoCache = loadCacheSongInfoCallback;
        bmsManager = manager;
        loadBMSFilesThread = new Thread(LoadBMS) {
            Priority = ThreadPriority.BelowNormal
        };
        loadBMSFilesThread.Start();
        var saveCacheThread = new Thread(CacheSave) {
            Priority = ThreadPriority.BelowNormal
        };
        saveCacheThread.Start();
    }

    public static void StopLoadBMS() {
        if(HasLoadingThreadRunning)
            loadBMSFilesThread.Abort();
    }

    static HashSet<FileInfo> RecursiveSearchFiles(DirectoryInfo parent, params string[] filters) {
        return RecursiveSearchFiles(parent, new HashSet<FileInfo>(), filters);
    }

    static HashSet<FileInfo> RecursiveSearchFiles(DirectoryInfo parent, HashSet<FileInfo> list, string[] filters) {
        if(filters == null || filters.Length < 1)
            list.UnionWith(parent.GetFiles());
        foreach(var filter in filters)
            list.UnionWith(parent.GetFiles(filter, SearchOption.TopDirectoryOnly));
        foreach(var directory in parent.GetDirectories())
            RecursiveSearchFiles(directory, list, filters);
        return list;
    }

    static void CacheLoad() {
        var cacheFileInfo = new FileInfo(Path.Combine(dataPath, "../cache.dat"));
        SongInfo songInfo;
        if(cacheFileInfo.Exists) {
            try {
                using(var readStream = cacheFileInfo.OpenRead()) {
                    var reader = new BinaryReader(readStream);
                    index = reader.ReadInt32();
                    while(readStream.Position < readStream.Length) {
                        try {
                            songInfo = new SongInfo {
                                index = reader.ReadInt32(),
                                filePath = reader.ReadString(),
                                artist = reader.ReadString(),
                                subArtist = reader.ReadString(),
                                genre = reader.ReadString(),
                                bpm = reader.ReadSingle(),
                                level = reader.ReadSingle(),
                                comments = reader.ReadString(),
                                backgroundPath = reader.ReadString(),
                            };
                            if(File.Exists(songInfo.filePath)) {
                                cachedSongInfo.Add(songInfo);
                                cachedSongInfoPaths.Add(songInfo.filePath);
                            }
                        } catch { }
                    }
                }
            } catch { }
        }
        cacheLoaded = true;
    }

    static void CacheSave() {
        while(!cacheLoaded) Thread.Sleep(25);
        int currentIdx = 0;
        bool isMax;
        using(var writeStream = File.Open(Path.Combine(dataPath, "../cache.dat"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
            var writer = new BinaryWriter(writeStream);
            writer.Write(index);
            while(true) {
                while(isMax = (!endOfCache && currentIdx >= cachedSongInfo.Count))
                    Thread.Sleep(25);
                if(isMax) {
                    if(!endOfCache)
                        continue;
                    break;
                }
                WriteSongInfo(writer, cachedSongInfo[currentIdx]);
                currentIdx++;
            }
        }
    }

    static void WriteSongInfo(BinaryWriter writer, SongInfo songInfo) {
        writer.Write(songInfo.index);
        writer.Write(songInfo.filePath);
        writer.Write(songInfo.artist);
        writer.Write(songInfo.subArtist);
        writer.Write(songInfo.genre);
        writer.Write(songInfo.bpm);
        writer.Write(songInfo.level);
        writer.Write(songInfo.comments);
        writer.Write(songInfo.backgroundPath);
    }

    public static void RemoveSongInfo(int index) {
        pendingRemoveSongInfos.Enqueue(index);
        if(!isRemoving) {
            isRemoving = true;
            var thread = new Thread(RemoveSongInfo) {
                Priority = ThreadPriority.BelowNormal
            };
            thread.Start();
        }
    }

    static void RemoveSongInfo() {
        int index;
        while(pendingRemoveSongInfos.TryDequeue(out index))
            cachedSongInfo.RemoveAll(songInfo => songInfo.index == index);
        isRemoving = false;
    }

    static void LoadBMS() {
        try {
            while(!cacheLoaded) Thread.Sleep(25);
            if(onAddSongInfoCache != null)
                onAddSongInfoCache.Invoke(cachedSongInfo);
            var dirInfo = new DirectoryInfo(Path.Combine(dataPath, "../BMS"));
            var fileList = RecursiveSearchFiles(dirInfo, "*.bms", "*.bme", "*.bml", "*.pms");
            SongInfo songInfo;
            string bmsContent;
            int i = 0, l = fileList.Count;
            foreach(var file in fileList) {
                if(cachedSongInfoPaths.Contains(file.FullName)) continue;
                if(!file.Exists) continue;
                bmsContent = string.Empty;
                using(var fs = file.OpenRead())
                using(var fsRead = new StreamReader(fs, CurrentEncoding))
                    bmsContent = fsRead.ReadToEnd();
                bmsManager.LoadBMS(bmsContent, file.Directory.FullName, true);
                songInfo = new SongInfo {
                    index = GetNextIndex(),
                    filePath = file.FullName,
                    name = bmsManager.Title,
                    artist = bmsManager.Artist,
                    subArtist = bmsManager.SubArtist,
                    genre = bmsManager.Genre,
                    bpm = bmsManager.BPM,
                    level = bmsManager.PlayLevel,
                    comments = bmsManager.Comments,
                    background = bmsManager.StageFile,
                    backgroundPath = bmsManager.StageFilePath
                };
                cachedSongInfo.Add(songInfo);
                cachedSongInfoPaths.Add(file.FullName);
                if(onAddSongInfo != null)
                    onAddSongInfo.Invoke(songInfo);
                loadedPercentage = ++i / (float)l;
            }
            endOfCache = true;
        } catch(ThreadAbortException) {
        } catch(Exception ex) {
            Debug.LogException(ex);
        }
    }

}

public static class SongInfoComparer {
    public static int CompareByName(SongInfo lhs, SongInfo rhs) {
        return string.Compare(lhs.name, rhs.name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByNameInverse(SongInfo lhs, SongInfo rhs) {
        return string.Compare(rhs.name, lhs.name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByArtist(SongInfo lhs, SongInfo rhs) {
        return string.Compare(lhs.artist, rhs.artist, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByArtistInverse(SongInfo lhs, SongInfo rhs) {
        return string.Compare(rhs.artist, lhs.artist, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByGenre(SongInfo lhs, SongInfo rhs) {
        return string.Compare(lhs.genre, rhs.genre, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByGenreInverse(SongInfo lhs, SongInfo rhs) {
        return string.Compare(rhs.genre, lhs.genre, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int CompareByBPM(SongInfo lhs, SongInfo rhs) {
        return lhs.bpm.CompareTo(rhs.bpm);
    }

    public static int CompareByBPMInverse(SongInfo lhs, SongInfo rhs) {
        return rhs.bpm.CompareTo(lhs.bpm);
    }

    public static int CompareByLevel(SongInfo lhs, SongInfo rhs) {
        return lhs.level.CompareTo(rhs.level);
    }

    public static int CompareByLevelInverse(SongInfo lhs, SongInfo rhs) {
        return rhs.level.CompareTo(lhs.level);
    }

    public enum SortMode {
        Name, NameInverse,
        Artist, ArtistInverse,
        Genre, GenreInverse,
        BPM, BPMInverse,
        Level, LevelInverse
    }
}
