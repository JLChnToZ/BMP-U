using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using SystemThreadPriority = System.Threading.ThreadPriority;

using BMS;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(ToggleGroup))]
public class SelectSongScrollContent : MonoBehaviour {
    static Vector2 savedPosition = new Vector2(0.5F, 0);

    public GameObject prefab;
    public ScrollRect scroller;
    [NonSerialized]
    public bool markLoaded;

    float widthPerRow;
    int idx = 0;
    int slotPerRow = 1;
    static int selectedIndex = -1;
    int selectedIdx = -1;
    float selectedIndexPos = -1;
    bool updateRequired;
    bool restorePosition;

    List<SelectSongScrollRow> songRows = new List<SelectSongScrollRow>();
    SongInfo selectedSongInfo = new SongInfo { index = -1 };
    List<SongInfo> songInfos = new List<SongInfo>();
    Vector2 currentPosition = Vector2.zero;
    HashSet<int> loadingImageIndeces = new HashSet<int>();
    List<int> deleteSongIdx = new List<int>(); 

    public event Action<Texture> OnChangeBackground;

    public event Action<int> OnSongInfoRemoved;
    
    RectTransform _rectTransform;
    public RectTransform rectTransform {
        get {
            if(_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    ToggleGroup _toggleGroup;
    public ToggleGroup toggleGroup {
        get {
            if(_toggleGroup == null)
                _toggleGroup = GetComponent<ToggleGroup>();
            return _toggleGroup;
        }
    }

    public int SelectedSongInternalIndex {
        get { return selectedIndex; }
    }

    public SongInfo SelectedSongInfo {
        get {
            return selectedSongInfo;
        }
    }
    
	IEnumerator Start () {
        scroller.onValueChanged.AddListener(ScrollUpdate);
        var prefabConf = prefab.GetComponent<SelectSongScrollRow>();
        var parentRect = prefab.GetComponent<RectTransform>();
        slotPerRow = prefabConf.items.Length;
        songRows.Add(prefabConf);
        yield return new WaitForSeconds(1);
        widthPerRow = parentRect.sizeDelta.x;
        for(float x = widthPerRow, w = scroller.viewport.rect.width + widthPerRow; x < w; x += widthPerRow) {
            var go = Instantiate(prefab);
            var t = go.GetComponent<RectTransform>();
            t.SetParent(rectTransform, false);
            t.sizeDelta = parentRect.sizeDelta;
            songRows.Add(go.GetComponent<SelectSongScrollRow>());
        }
        foreach(var row in songRows)
            foreach(var item in row.items)
                item.SetParent(this);
        updateRequired = true;
    }

    void Update() {
        if(updateRequired) {
            updateRequired = false;
            UpdateItems();
        }
    }

    public void SaveCurrentPosition() {
        savedPosition = currentPosition;
    }

    public void RestorePosition() {
        restorePosition = true;
        updateRequired = true;
    }
	
    void OnDestroy() {
        savedPosition = currentPosition;
        if(scroller != null)
            scroller.onValueChanged.RemoveListener(ScrollUpdate);
    }

    void ScrollUpdate(Vector2 position) {
        currentPosition = position;
        updateRequired = true;
    }

    public void Clear() {
        songInfos.Clear();
        updateRequired = true;
    }

    public void AddItem(IEnumerable<SongInfo> songInfo) {
        songInfos.AddRange(songInfo);
        FindPosition();
        updateRequired = true;
    }

    public void AddItem(SongInfo songInfo) {
        songInfos.Add(songInfo);
        updateRequired = true;
    }

    public void Sort() {
        Sort(currentSortMode);
    }

    public void OnSelected(SongInfo songInfo) {
        if(selectedIndex != songInfo.index || !selectedSongInfo.Equals(songInfo)) {
            selectedIndex = songInfo.index;
            selectedSongInfo = songInfo;
            InvokeChangeSongBackground();
        }
    }

    void InvokeChangeSongBackground() {
        if(OnChangeBackground != null)
            OnChangeBackground.Invoke(selectedSongInfo.background);
    }

    public void Sort(SongInfoComparer.SortMode sortMode) {
        if(sortThread != null && sortThread.IsAlive)
            sortThread.Abort();
        currentSortMode = sortMode;
        if(!markLoaded) return;
        sortThread = new Thread(SortInThread) {
            Priority = SystemThreadPriority.BelowNormal
        };
        sortThread.Start();
    }

    void UpdateItems() {
        idx = Mathf.Max(0, Mathf.FloorToInt(-rectTransform.localPosition.x / widthPerRow));
        int itemCount = songInfos.Count;
        rectTransform.sizeDelta = new Vector2(widthPerRow * Mathf.Ceil((float)itemCount / slotPerRow), rectTransform.sizeDelta.y);
        if(selectedIndexPos >= 0) {
            scroller.horizontalNormalizedPosition = selectedIndexPos;
            currentPosition.x = selectedIndexPos;
            InvokeChangeSongBackground();
            selectedIndexPos = -1;
        }
        SelectSongScrollRow row;
        GameObject go;
        SongInfo songInfo;
        bool hasToggle = false;
        for(int i = 0, l = songRows.Count, rowIndex, j, itemIdx, initResult; i < l; i++) {
            rowIndex = Mathf.FloorToInt(Mathf.Repeat(idx + i, l));
            row = songRows[rowIndex];
            row.rectTransform.localPosition = new Vector3((idx + i) * widthPerRow, 0, 0);
            for(j = 0; j < slotPerRow; j++) {
                itemIdx = (idx + i) * slotPerRow + j;
                go = row.items[j].gameObject;
                if(itemIdx < itemCount) {
                    songInfo = songInfos[itemIdx];
                    initResult = row.items[j].Init(songInfo);
                    if(initResult == 1 || initResult == -1) hasToggle = true;
                    if(initResult == 0) continue;
                    if(!songInfo.Exists) {
                        deleteSongIdx.Add(itemIdx);
                        if(OnSongInfoRemoved != null)
                            OnSongInfoRemoved.Invoke(songInfo.index);
                    }
                    if(!loadingImageIndeces.Contains(itemIdx) && songInfo.background == null && !string.IsNullOrEmpty(songInfo.backgroundPath))
                        StartCoroutine(LoadBackgroundImage(itemIdx, songInfo));
                    if(!go.activeSelf) go.SetActive(true);
                } else if(go.activeSelf)
                    go.SetActive(false);
            }
        }
        if(!hasToggle) toggleGroup.SetAllTogglesOff();
        if(deleteSongIdx.Count > 0) {
            deleteSongIdx.Sort();
            deleteSongIdx.Reverse();
            foreach(var removeIdx in deleteSongIdx)
                songInfos.RemoveAt(removeIdx);
            deleteSongIdx.Clear();
        }
    }

    IEnumerator LoadBackgroundImage(int index, SongInfo songInfo) {
        loadingImageIndeces.Add(index);
        var file = new FileInfo(SongInfoLoader.GetAbsolutePath(songInfo.filePath));
        var resLoader = new ResourceLoader(file.Directory.FullName);
        var resObj = new ResourceObject(-1, ResourceType.bmp, songInfo.backgroundPath);
        yield return StartCoroutine(resLoader.LoadResource(resObj));
        songInfo.background = resObj.texture;
        songInfos[index] = songInfo;
        updateRequired = true;
        loadingImageIndeces.Remove(index);
        if(selectedSongInfo.index == songInfo.index)
            OnSelected(songInfo);
        yield break;
    }

    Thread sortThread;
    SongInfoComparer.SortMode currentSortMode;
    void SortInThread() {
        try {
            songInfos.Sort();
            switch(currentSortMode) {
                case SongInfoComparer.SortMode.Name:
                    songInfos.Sort(SongInfoComparer.CompareByName);
                    break;
                case SongInfoComparer.SortMode.Artist:
                    songInfos.Sort(SongInfoComparer.CompareByArtist);
                    break;
                case SongInfoComparer.SortMode.Genre:
                    songInfos.Sort(SongInfoComparer.CompareByGenre);
                    break;
                case SongInfoComparer.SortMode.BPM:
                    songInfos.Sort(SongInfoComparer.CompareByBPM);
                    break;
                case SongInfoComparer.SortMode.Level:
                    songInfos.Sort(SongInfoComparer.CompareByLevel);
                    break;
                case SongInfoComparer.SortMode.NameInverse:
                    songInfos.Sort(SongInfoComparer.CompareByNameInverse);
                    break;
                case SongInfoComparer.SortMode.ArtistInverse:
                    songInfos.Sort(SongInfoComparer.CompareByArtistInverse);
                    break;
                case SongInfoComparer.SortMode.GenreInverse:
                    songInfos.Sort(SongInfoComparer.CompareByGenreInverse);
                    break;
                case SongInfoComparer.SortMode.BPMInverse:
                    songInfos.Sort(SongInfoComparer.CompareByBPMInverse);
                    break;
                case SongInfoComparer.SortMode.LevelInverse:
                    songInfos.Sort(SongInfoComparer.CompareByLevelInverse);
                    break;
            }
            FindPosition();
            updateRequired = true;
        } catch(ThreadAbortException) {
        } catch(Exception ex) {
            Debug.LogException(ex);
        }
    }

    void FindPosition() {
        selectedIdx = songInfos.FindIndex(songInfo => songInfo.index == selectedIndex);
        selectedSongInfo = selectedIdx >= 0 ? songInfos[selectedIdx] : new SongInfo { index = -1 };
        selectedIndexPos = Mathf.Floor(selectedIdx / slotPerRow) / Mathf.Floor(songInfos.Count / slotPerRow);
    }
}
