using System;
using System.Linq;
using BMS.Visualization;
using UnityEngine;

class NoteSpawnerSP: NoteSpawner {
    [SerializeField]
    int spawnerId;

    void Awake() {
        NoteLayoutOptionsHandler.Initialize();
        switch(spawnerId) {
            case 0:
                handledChannels = NoteLayoutOptionsHandler.LowerDeck.ToArray();
                break;
            case 1:
                handledChannels = NoteLayoutOptionsHandler.UpperDeck.ToArray();
                break;
        }
    }
}
