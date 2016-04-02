using System;
using UnityEngine;

public class InputHandler: MonoBehaviour {
    [Serializable]
    struct InputMapping {
        public string inputButtonName;
        public int channelId;
    }

    struct MappingStatus {
        public string inputButtonName;
        public int channelId;
        public bool previousState;
    }

    public BMS.NoteDetector noteDetector;

    [SerializeField]
    InputMapping[] inputMappings;

    MappingStatus[] mappingStatus;
    int length;

    void Awake() {
        length = inputMappings.Length;
        mappingStatus = new MappingStatus[length];
        for(int i = 0, l = inputMappings.Length; i < l; i++)
            mappingStatus[i] = new MappingStatus {
                inputButtonName = inputMappings[i].inputButtonName,
                channelId = inputMappings[i].channelId
            };
        inputMappings = new InputMapping[0];
    }

    void Update() {
        MappingStatus mapState;
        bool currentState;
        for(int i = 0; i < length; i++) {
            mapState = mappingStatus[i];
            currentState = Input.GetButton(mapState.inputButtonName);
            if(currentState != mapState.previousState)
                noteDetector.OnClick(mapState.channelId, currentState);
            mapState.previousState = currentState;
            mappingStatus[i] = mapState;
        }
    }
}
