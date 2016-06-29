using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

class PresetHandler: MonoBehaviour {
    readonly Dictionary<string, byte[]> presets = new Dictionary<string, byte[]>();
    bool initialized = false;
    public string path = "../presets.dat";
    public Action OnReset;
    public Action<byte[]> OnPresetChange;
    public Func<byte[]> OnPresetRequest;
    [SerializeField]
    Dropdown presetsDisplay;
    [SerializeField]
    InputField presetsName;
    [SerializeField]
    Button addButton, removeButton, resetButton;
    int oldIndex = -1;

    void Awake() {
        presetsDisplay.onValueChanged.AddListener(SelectionChanged);
        addButton.onClick.AddListener(AddClick);
        removeButton.onClick.AddListener(RemoveClick);
        resetButton.onClick.AddListener(ResetClick);
    }

    void SelectionChanged(int index) {
        ForceRequestPreset();
        oldIndex = index;
        SelectionChanged();
    }

    void SelectionChanged() {
        presetsName.text = presets.Count == 0 ? "" : presetsDisplay.options[oldIndex].text;
        if(oldIndex < 0) return;
        byte[] preset;
        if(!presets.TryGetValue(presetsDisplay.options[oldIndex].text, out preset))
            return;
        if(OnPresetChange != null)
            OnPresetChange.Invoke(preset);
    }

    void AddClick() {
        if(string.IsNullOrEmpty(presetsName.text)) return;
        if(OnPresetRequest == null) return;
        if(presets.ContainsKey(presetsName.text)) return;
        presetsDisplay.options.Add(new Dropdown.OptionData(presetsName.text));
        var preset = OnPresetRequest();
        presets.Add(presetsName.text, preset);
        oldIndex = presets.Count - 1;
        Save();
        presetsDisplay.value = oldIndex;
    }

    void RemoveClick() {
        if(oldIndex < 0) return;
        int index = oldIndex;
        SelectionChanged();
        presets.Remove(presetsDisplay.options[index].text);
        presetsDisplay.options.RemoveAt(index);
        if(oldIndex >= presets.Count)
            oldIndex--;
        presetsName.text = presets.Count == 0 ? "Default" : presetsDisplay.options[oldIndex].text;
        Save();
    }

    void ResetClick() {
        if(OnReset != null)
            OnReset.Invoke();
    }

    void Start() {
        var file = new FileInfo(SongInfoLoader.GetAbsolutePath(path));
        if(file.Exists)
            using(var stream = file.OpenRead()) {
                var reader = new BinaryReader(stream);
                int presetsCount = reader.ReadInt32();
                for(int i = 0; i < presetsCount; i++) {
                    var presetName = reader.ReadString();
                    int dataLength = reader.ReadInt32();
                    presets[presetName] = dataLength == -1 ? null : reader.ReadBytes(dataLength);
                    presetsDisplay.options.Add(new Dropdown.OptionData(presetName));
                }
                oldIndex = reader.ReadInt32();
            }
        if(presets.Count == 0) {
            oldIndex = 0;
            presets.Add("Default", null);
            presetsDisplay.options.Add(new Dropdown.OptionData("Default"));
            if(OnReset != null)
                OnReset.Invoke();
        }
        if(oldIndex >= 0) {
            presetsDisplay.value = oldIndex;
            SelectionChanged();
        }
        initialized = true;
    }

    public void SetRequest(string presetName, byte[] preset) {
        if(preset == null)
            return;
        if(string.IsNullOrEmpty(presetName))
            presetName = oldIndex < 0 ? "Default" : presetsDisplay.options[oldIndex].text;
        if(!presets.ContainsKey(presetName)) {
            presetsDisplay.options.Add(new Dropdown.OptionData(presetName));
            oldIndex = presetsDisplay.options.Count - 1;
        }
        presets[presetName] = preset;
        Save();
        presetsDisplay.value = oldIndex;
    }

    void ForceRequestPreset() {
        if(OnPresetRequest != null && initialized) {
            var preset = OnPresetRequest();
            presets[presetsDisplay.options[oldIndex].text] = preset;
            Save();
        }
    }

    public void Save() {
        using(var stream = File.Open(SongInfoLoader.GetAbsolutePath(path), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
            var writer = new BinaryWriter(stream);
            writer.Write(presets.Count);
            foreach(var kv in presets) {
                writer.Write(kv.Key);
                if(kv.Value == null) {
                    writer.Write(-1);
                    continue;
                }
                writer.Write(kv.Value.Length);
                writer.Write(kv.Value);
            }
            writer.Write(oldIndex);
        }
    }
}
