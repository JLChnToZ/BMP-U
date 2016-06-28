using System;
using UnityEngine;

public class InputHandler: MonoBehaviour {
    struct MappingStatus {
        public KeyCode keyCode;
        public int channelId;
        public bool previousState;
    }

    public BMS.NoteDetector noteDetector;
    
    MappingStatus[] mappingStatus;
    int length;

    void Awake() {
        var keyMapping = NoteLayoutOptionsHandler.KeyMapping;
        length = keyMapping.Count;
        mappingStatus = new MappingStatus[length];
        int i = 0;
        foreach(var kv in keyMapping) {
            mappingStatus[i++] = new MappingStatus {
                keyCode = kv.Value,
                channelId = kv.Key
            };
        }
    }

    void Update() {
        MappingStatus mapState;
        bool currentState;
        for(int i = 0; i < length; i++) {
            mapState = mappingStatus[i];
            currentState = Input.GetKey(mapState.keyCode);
            if(currentState != mapState.previousState)
                noteDetector.OnClick(mapState.channelId, currentState);
            mapState.previousState = currentState;
            mappingStatus[i] = mapState;
        }
    }
}
