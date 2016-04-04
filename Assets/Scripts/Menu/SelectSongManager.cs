using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using BMS;

using System.Collections;
using System.Collections.Generic;
using System;

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

    public BMSManager bmsManager;

	void Start () {
        SongInfoLoader.CurrentCodePage = 932; // Hardcoded to Shift-JIS as most of BMS are encoded by this.
        LoadBMSInThread();
        autoModeToggle.isOn = Loader.autoMode;
        speedSlider.value = Loader.speed;
        sortMode.value = savedSortMode;
        itemsDisplay.OnChangeBackground += ChangeBackground;
        itemsDisplay.OnSongInfoRemoved += SongInfoLoader.RemoveSongInfo;
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
    
    Coroutine loadBMSFilesCoroutine;
    void LoadBMSInThread() {
        loadingDisplay.gameObject.SetActive(true);
        if(bmsManager == null)
            bmsManager = gameObject.GetComponent<BMSManager>() ?? gameObject.AddComponent<BMSManager>();
        
        SongInfoLoader.LoadBMSInThread(bmsManager, OnLoadCacheInfo, OnAddSong);
        dataPath = Application.dataPath;
        StartCoroutine(LoadBMSEnd());
    }

    void OnAddSong(SongInfo songInfo) {
        if(itemsDisplay != null)
            itemsDisplay.AddItem(songInfo);
        else
            SongInfoLoader.StopLoadBMS();
    }

    void OnLoadCacheInfo(IEnumerable<SongInfo> songInfos) {
        if(itemsDisplay != null)
            itemsDisplay.AddItem(songInfos);
        else
            SongInfoLoader.StopLoadBMS();
    }

    IEnumerator LoadBMSEnd() {
        Vector2 loadingAnchorMax = loadingPercentageDisplay.anchorMax;
        while(SongInfoLoader.HasLoadingThreadRunning) {
            loadingAnchorMax.x = SongInfoLoader.LoadedPercentage;
            loadingPercentageDisplay.anchorMax = loadingAnchorMax;
            yield return null;
        }
        loadingAnchorMax.x = 1;
        loadingPercentageDisplay.anchorMax = loadingAnchorMax;
        loadingDisplay.gameObject.SetActive(false);
        yield break;
    }

    void ChangeBackground(Texture texture) {
        background.texture = texture;
        Color color = texture || itemsDisplay.SelectedSongInfo.index < 0 ? Color.white : colorSet.GetColor(itemsDisplay.SelectedSongInfo.level);
        color.a /= 2;
        background.color = color;
    }

}
