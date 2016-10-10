using System;
using System.Collections.Generic;
using System.Linq;
using BMS.Visualization;
using UnityEngine;

public class NoteSpawnerSP: NoteSpawner {
    [SerializeField]
    int spawnerId;
    float originalClampRangeStart, originalClampRangeEnd;

    [Range(0, 360F)]
    public float clampRangeStart = 0F, clampRangeEnd = 360F;
    public float startDistance = 0F, targetDistance = 1F, offset;
    public float maxNoteDistance = 360F;
    public Vector3 centroid;
    public ScoreDisplayPack scoreDisplayPack;
    public bool showTrackIndicators;
    public GameObject trackIndicatorTemplate;
    
    private readonly List<TrackIndicator> spawnedIndicators = new List<TrackIndicator>();

    void Awake() {
        NoteLayoutOptionsHandler.Reset(false);
        switch(spawnerId) {
            case 0:
                handledChannels = NoteLayoutOptionsHandler.LowerDeck.ToArray();
                break;
            case 1:
                handledChannels = NoteLayoutOptionsHandler.UpperDeck.ToArray();
                break;
        }
        originalClampRangeStart = clampRangeStart;
        originalClampRangeEnd = clampRangeEnd;
    }

    protected override NoteHandler GetFreeNoteHandler() {
        var noteHandler = base.GetFreeNoteHandler() as CurvedNoteHandler;
        noteHandler.centroid = centroid;
        noteHandler.clampRangeStart = clampRangeStart;
        noteHandler.clampRangeEnd = clampRangeEnd;
        noteHandler.startDistance = startDistance;
        noteHandler.targetDistance = targetDistance;
        noteHandler.offset = offset;
        noteHandler.scoreDisplayPack = scoreDisplayPack;
        return noteHandler;
    }

    protected override void LateUpdate() {
        var bmsLoaded = bmsLoadedCalled;
        base.LateUpdate();
        if(bmsLoaded) UpdateDistance();
    }

    void UpdateDistance() {
        int count = channels != null ? channels.Length : 0;
        if(count < 1) return;
        float noteDistance = Mathf.Abs(clampRangeEnd - clampRangeStart) / count;
        if(noteDistance > maxNoteDistance) {
            float middle = Mathf.Repeat((clampRangeStart + clampRangeEnd) / 2, 360F);
            clampRangeStart = middle - maxNoteDistance * count / 2;
            clampRangeEnd = middle + maxNoteDistance * count / 2;
        }
        UpdateIndicators();
    }

    void UpdateIndicators() {
        if(!showTrackIndicators) return;
        int channelCount = channels == null ? 0 : (channels.Length + 1);
        for(int i = 0, l = Math.Max(channelCount, spawnedIndicators.Count); i < l; i++) {
            TrackIndicator trackInd;
            if(i >= channelCount || channelCount < 2) {
                if(i < spawnedIndicators.Count) {
                    spawnedIndicators[i].gameObject.SetActive(false);
                    continue;
                } else {
                    break;
                }
            } else if(i < channelCount && i >= spawnedIndicators.Count) {
                GameObject go = Instantiate(trackIndicatorTemplate);
                go.transform.SetParent(transform, false);
                spawnedIndicators.Add(trackInd = go.GetComponent<TrackIndicator>());
            } else {
                trackInd = spawnedIndicators[i];
            }
            trackInd.mode = 0;
            trackInd.centroid = centroid;
            trackInd.clampRangeStart = clampRangeStart;
            trackInd.clampRangeEnd = clampRangeEnd;
            trackInd.startDistance = startDistance;
            trackInd.targetDistance = targetDistance;
            trackInd.startOffset = offset;
            trackInd.offset = offset;
            trackInd.delta = channelCount <= 2 ? i : (i - 0.5F) / (channelCount - 2);
            trackInd.Init();
        }
    }

    protected override void OnGameStarted() {
        base.OnGameStarted();
    }
}
