﻿using System;
using System.Linq;
using BMS.Visualization;
using UnityEngine;

public class NoteSpawnerClassic: NoteSpawner {
    [SerializeField]
    int spawnerId;
    float originalClampRangeStart, originalClampRangeEnd;
    
    public float clampRangeStart = -1, clampRangeEnd = 1;
    public float startDistance = 0F, targetDistance = 1F, offset;
    public float maxNoteDistance = 360F;
    public Vector3 centroid;
    public ScoreDisplayPack scoreDisplayPack;

    void Awake() {
        NoteLayoutOptionsHandler.Reset(false);
        switch(spawnerId) {
            case 0:
                handledChannels = NoteLayoutOptionsHandler.LowerDeck.ToArray();
                break;
            case 1:
                handledChannels = NoteLayoutOptionsHandler.UpperDeck.ToArray();
                Array.Reverse(handledChannels); // It's left-side right
                break;
        }
        originalClampRangeStart = clampRangeStart;
        originalClampRangeEnd = clampRangeEnd;
    }

    protected override NoteHandler GetFreeNoteHandler() {
        var noteHandler = base.GetFreeNoteHandler() as ClassicNoteHandler;
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
    }

    protected override void OnGameStarted() {
        base.OnGameStarted();
    }
}