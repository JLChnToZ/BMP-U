using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using BMS;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using ThreadPriority = System.Threading.ThreadPriority;

public class SelectSongManager : MonoBehaviour {
    static int savedSortMode;

    public SelectSongScrollContent itemsDisplay;
    public RectTransform loadingDisplay;
    public RectTransform loadingPercentageDisplay;
    public Toggle autoModeToggle;
    public Slider speedSlider;
    public Dropdown sortMode;
    public RawImage background;
    public ColorRampLevel colorSet;
    string dataPath;
    float loadedPercentage = 0;

    static List<SongInfo> cachedSongInfo = new List<SongInfo>();
    static HashSet<string> cachedSongInfoPaths = new HashSet<string>();

    public BMSManager bmsManager;

	void Start () {
        SongInfoLoader.CurrentCodePage = 932; // Hardcoded to Shift-JIS as most of BMS are encoded by this.
        LoadBMSInThread();
        autoModeToggle.isOn = Loader.autoMode;
        speedSlider.value = Loader.speed;
        sortMode.value = savedSortMode;
        itemsDisplay.OnChangeBackground += ChangeBackground;
    }

    void OnDestroy() {
        if(itemsDisplay != null)
            itemsDisplay.OnChangeBackground -= ChangeBackground;
    }

    public void ToggleAuto(bool state) {
        Loader.autoMode = autoModeToggle.isOn;
    }

    public void ChangeSpeed(float value) {
        Loader.speed = speedSlider.value;
    }

    public void ChangeSortMode(int mode) {
        savedSortMode = mode;
        switch(savedSortMode) {
            case 0: itemsDisplay.Sort(SongInfoComparer.SortMode.Name); break;
            case 1: itemsDisplay.Sort(SongInfoComparer.SortMode.Artist); break;
            case 2: itemsDisplay.Sort(SongInfoComparer.SortMode.Genre); break;
            case 3: itemsDisplay.Sort(SongInfoComparer.SortMode.Level); break;
            case 4: itemsDisplay.Sort(SongInfoComparer.SortMode.BPM); break;
        }
    }

    public void StartGame() {
        if(itemsDisplay.SelectedSongInternalIndex >= 0)
            SceneManager.LoadScene("GameScene");
    }

    Thread loadBMSFilesThread;
    Coroutine loadBMSFilesCoroutine;
    void LoadBMSInThread() {
        loadingDisplay.gameObject.SetActive(true);
        if(bmsManager == null)
            bmsManager = gameObject.GetComponent<BMSManager>() ?? gameObject.AddComponent<BMSManager>();

        if(loadBMSFilesThread != null && loadBMSFilesThread.IsAlive)
            loadBMSFilesThread.Abort();
        dataPath = Application.dataPath;
        loadBMSFilesThread = new Thread(LoadBMS) {
            Priority = ThreadPriority.BelowNormal
        };
        loadBMSFilesThread.Start();
        StartCoroutine(LoadBMSEnd());
    }

    IEnumerator LoadBMSEnd() {
        Vector2 loadingAnchorMax = loadingPercentageDisplay.anchorMax;
        while(loadBMSFilesThread != null && loadBMSFilesThread.IsAlive) {
            loadingAnchorMax.x = loadedPercentage;
            loadingPercentageDisplay.anchorMax = loadingAnchorMax;
            yield return null;
        }
        loadingAnchorMax.x = 1;
        loadingPercentageDisplay.anchorMax = loadingAnchorMax;
        loadingDisplay.gameObject.SetActive(false);
        yield break;
    }
    
    void LoadBMS() {
        try {
            itemsDisplay.markLoaded = false;
            itemsDisplay.AddItem(cachedSongInfo);
            itemsDisplay.RestorePosition();
            var dirInfo = new DirectoryInfo(Path.Combine(dataPath, "../BMS"));
            var fileList = RecursiveSearchFiles(dirInfo, null, "*.bms", "*.bme", "*.bml", "*.pms");
            SongInfo songInfo;
            string bmsContent;
            int i = 0, l = fileList.Count;
            foreach(var file in fileList) {
                if(itemsDisplay == null) return;
                if(cachedSongInfoPaths.Contains(file.FullName)) continue;
                if(!file.Exists) continue;
                bmsContent = string.Empty;
                using(var fs = file.OpenRead())
                using(var fsRead = new StreamReader(fs, SongInfoLoader.CurrentEncoding))
                    bmsContent = fsRead.ReadToEnd();
                bmsManager.LoadBMS(bmsContent, file.Directory.FullName, true);
                songInfo = new SongInfo {
                    index = SongInfoLoader.GetNextIndex(),
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
                if(itemsDisplay == null) return;
                itemsDisplay.AddItem(songInfo);
                loadedPercentage = ++i / (float)l;
            }
            if(itemsDisplay == null) return;
            itemsDisplay.markLoaded = true;
            itemsDisplay.Sort();
        } catch(ThreadAbortException) {
        } catch(Exception ex) {
            Debug.LogException(ex);
        }
    }

    static HashSet<FileInfo> RecursiveSearchFiles(DirectoryInfo parent, HashSet<FileInfo> list, params string[] filters) {
        if(list == null) list = new HashSet<FileInfo>();
        if(filters == null || filters.Length < 1)
            list.UnionWith(parent.GetFiles());
        foreach(var filter in filters)
            list.UnionWith(parent.GetFiles(filter, SearchOption.TopDirectoryOnly));
        foreach(var directory in parent.GetDirectories())
            RecursiveSearchFiles(directory, list, filters);
        return list;
    }

    void ChangeBackground(Texture texture) {
        background.texture = texture;
        Color color = texture || itemsDisplay.SelectedSongInfo.index < 0 ? Color.white : colorSet.GetColor(itemsDisplay.SelectedSongInfo.level);
        color.a /= 2;
        background.color = color;
    }

}
