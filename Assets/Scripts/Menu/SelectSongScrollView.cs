using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BMS;

using Entry = SongInfoLoader.Entry;

public class SelectSongScrollView: MonoBehaviour {
    public BMSManager bmsManager;

    public GameObject prefab;
    public ScrollRect scroller;
    public Vector2 sizePerEntry;

    public float slopeStart = 0;
    public float slopeEnd = 0;

    public float slopeAnchorStart = 0;
    public float slopeAnchorEnd = 0;
    
    IList<Entry> entries;
    readonly List<SelectSongEntry> entryDisplay = new List<SelectSongEntry>();
    
    void Awake() {
        scroller.onValueChanged.AddListener(OnScroll);

        entries = SongInfoLoader.Entries;
        SongInfoLoader.OnListUpdated += UpdateList;
    }

    IEnumerator Start() {
        yield return null;
        for(float y = 0, h = scroller.viewport.rect.height + sizePerEntry.y * 2; y < h; y += sizePerEntry.y) {
            var go = Instantiate(prefab);
            var t = go.GetComponent<RectTransform>();
            t.SetParent(scroller.content, false);
            entryDisplay.Add(go.GetComponent<SelectSongEntry>());
        }
        OnScroll();
    }

    void OnDestroy() {
        SongInfoLoader.OnListUpdated -= UpdateList;
    }

    void OnScroll() {
        OnScroll(scroller.normalizedPosition);
    }

    void OnScroll(Vector2 position) {
        if(!SongInfoLoader.IsReady) return;
        Vector2 actualSize = scroller.content.rect.size;
        Vector2 contentPosition = scroller.content.anchoredPosition;
        Vector2 viewportPosition = scroller.viewport.anchoredPosition;

        int startIndex = Mathf.Max(0, Mathf.FloorToInt(contentPosition.y / sizePerEntry.y));
        Rect viewportRect = scroller.viewport.rect;
        Vector2 offsetMin = viewportRect.min - contentPosition;
        Vector2 offsetMax = viewportRect.max - contentPosition;
        for(int i = 0, l = entryDisplay.Count, c = entries.Count; i < l; i++) {
            int actualIndex = startIndex + i - 1;
            SelectSongEntry entryDisp = entryDisplay[(actualIndex + l) % l];
            if(actualIndex < c && actualIndex >= 0) {
                Vector2 pos = entryDisp.transform.anchoredPosition;
                pos.y = -actualIndex * sizePerEntry.y;
                float lerp = (pos.y - offsetMin.y) / (offsetMax.y - offsetMin.y);
                pos.x = Mathf.LerpUnclamped(slopeStart, slopeEnd, lerp);
                entryDisp.transform.anchoredPosition = pos;

                Entry entry = entries[actualIndex];
                if(entry.isDirectory)
                    entryDisp.Load(entry.dirInfo, entry.isParentDirectory, this);
                else
                    entryDisp.Load(entry.songInfo, this);
                entryDisp.gameObject.SetActive(true);
                entryDisp.UpdateChildTransform(Mathf.LerpUnclamped(slopeAnchorStart, slopeAnchorEnd, lerp));
            } else {
                entryDisp.gameObject.SetActive(false);
            }
        }
        SongInfoLoader.ScrollPosition = position;
    }

    void UpdateList() {
        RectTransform content = scroller.content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, sizePerEntry.y * entries.Count);
        scroller.normalizedPosition = SongInfoLoader.ScrollPosition;
        OnScroll();
    }

    public void RefreshDisplay() {
        foreach(var entry in entryDisplay)
            if(entry.gameObject.activeSelf)
                entry.UpdateDisplay();
    }
}
