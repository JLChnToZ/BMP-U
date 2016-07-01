using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

class NoteLayoutOptionsHandler: MonoBehaviour {
    static readonly int[] usableChannels = new[] { 11, 12, 13, 14, 15, 18, 19, 21, 22, 23, 24, 25, 28, 29, 26, 27, 17, 16};
    static readonly KeyCode[] defaultKeyMapping = new[] {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J,
        KeyCode.O, KeyCode.P, KeyCode.L, KeyCode.K
    };

    static readonly List<string> keyCodeNames = new List<string>(Enum.GetNames(typeof(KeyCode)));
    static readonly KeyCode[] keyCodeValues = Enum.GetValues(typeof(KeyCode)) as KeyCode[];
    static readonly List<int> upperDeck = new List<int>(), lowerDeck = new List<int>();
    static bool initialized;
    readonly Dictionary<int, NoteLayoutOptionUIElement> currentMapping = new Dictionary<int, NoteLayoutOptionUIElement>();
    static readonly Dictionary<int, KeyCode> keyMapping = new Dictionary<int, KeyCode>();

    int selectedChannel = -1;

    public static IList<int> UpperDeck {
        get { return upperDeck.AsReadOnly(); }
    }

    public static IList<int> LowerDeck {
        get { return lowerDeck.AsReadOnly(); }
    }

    public static Dictionary<int, KeyCode> KeyMapping {
        get { return keyMapping; }
    }

    [SerializeField]
    ReorderableList unusedList, upperDeckList, lowerDeckList;
    [SerializeField]
    GameObject itemPrefab;
    [SerializeField]
    PresetHandler presetsHandler;

    [SerializeField]
    RectTransform keyMapMenu;
    [SerializeField]
    Text keyMappingDescDisplay;
    [SerializeField]
    Dropdown keyMappingDropdown;
    [SerializeField, Multiline]
    string keyMappingDescFormat;

    public static void Reset(bool forced) {
        if(initialized && !forced) return;
        upperDeck.Clear();
        lowerDeck.Clear();
        // Default: 1/2/3/4/5/8/9 = Normal, 6 = Scratch, 7 = FreeZone
        for(int i = 0, l = usableChannels.Length, channel; i < l; i++) {
            channel = usableChannels[i];
            switch(channel % 10) {
                case 6: case 7: upperDeck.Add(channel); break;
                default: lowerDeck.Add(channel); break;
            }
            keyMapping.Add(channel, defaultKeyMapping[i]);
        }
        initialized = true;
    }

    public void ShowKeyMapMenu(int channel) {
        if(Array.IndexOf(usableChannels, channel) < 0) return;
        selectedChannel = -1;
        KeyCode keyCode;
        if(!keyMapping.TryGetValue(channel, out keyCode))
            keyCode = KeyCode.None;
        keyMappingDropdown.value = Array.IndexOf(keyCodeValues, keyCode);
        selectedChannel = channel;
        keyMappingDescDisplay.text = string.Format(keyMappingDescFormat, channel);
        keyMapMenu.gameObject.SetActive(true);
    }

    void Awake() {
        presetsHandler.OnPresetChange = Load;
        presetsHandler.OnPresetRequest = ApplyAndSave;
        presetsHandler.OnReset = ResetAndApply;
        foreach(var channel in usableChannels) {
            var go = Instantiate(itemPrefab);
            go.transform.SetParent(unusedList.Content, false);
            var elm = go.GetComponent<NoteLayoutOptionUIElement>();
            elm.index = channel;
            elm.parent = this;
            currentMapping.Add(channel, elm);
        }
        AssignToDisplay();
        var keyCodeList = new List<string>(Enum.GetNames(typeof(KeyCode)));
        keyMappingDropdown.AddOptions(keyCodeList);
        keyMappingDropdown.onValueChanged.AddListener(OnMapKey);
    }

    public void Load(byte[] source) {
        if(source == null || source.Length == 0)
            return;
        using(var stream = new MemoryStream(source))
            Load(new BinaryReader(stream));
    }

    public void Load(BinaryReader reader) {
        var unusedChannels = new HashSet<int>(usableChannels);
        int upperLength, lowerLength, keyMapLength, i, channel;
        upperDeck.Clear();
        lowerDeck.Clear();
        keyMapping.Clear();
        upperLength = reader.ReadByte();
        for(i = 0; i < upperLength; i++) {
            channel = reader.ReadByte();
            upperDeck.Add(channel);
            unusedChannels.Remove(channel);
        }

        lowerLength = reader.ReadByte();
        for(i = 0; i < lowerLength; i++) {
            channel = reader.ReadByte();
            lowerDeck.Add(channel);
            unusedChannels.Remove(channel);
        }
        keyMapLength = reader.ReadByte();
        for(i = 0; i < keyMapLength; i++) {
            channel = reader.ReadByte();
            keyMapping[channel] = (KeyCode)reader.ReadInt16();
        }
        AssignToDisplay();
        foreach(var chn in unusedChannels)
            currentMapping[chn].transform.SetParent(unusedList.Content, false);
        initialized = true;
    }

    void AssignToDisplay() {
        Transform t;
        foreach(var chn in upperDeck) {
            t = currentMapping[chn].transform;
            t.SetParent(upperDeckList.Content, false);
            t.SetAsFirstSibling();
        }
        foreach(var chn in lowerDeck) {
            t = currentMapping[chn].transform;
            t.SetParent(lowerDeckList.Content, false);
            t.SetAsLastSibling();
        }
        foreach(var kv in currentMapping)
            kv.Value.OnKeyEdited(kv.Key, keyMapping[kv.Key]);
    }

    void OnMapKey(int index) {
        if(Array.IndexOf(usableChannels, selectedChannel) < 0) return;
        keyMapping[selectedChannel] = keyCodeValues[index];
        currentMapping[selectedChannel].OnKeyEdited(selectedChannel, keyCodeValues[index]);
    }

    public void Apply() {
        presetsHandler.SetRequest(null, ApplyAndSave());
    }

    public void ResetAndApply() {
        Reset(true);
        AssignToDisplay();
        Apply();
    }

    public byte[] ApplyAndSave() {
        upperDeck.Clear();
        foreach(var child in upperDeckList.Content.Cast<Transform>())
            upperDeck.Add(child.GetComponent<NoteLayoutOptionUIElement>().index);
        upperDeck.Reverse();
        lowerDeck.Clear();
        foreach(var child in lowerDeckList.Content.Cast<Transform>())
            lowerDeck.Add(child.GetComponent<NoteLayoutOptionUIElement>().index);
        return Save();
    }

    public byte[] Save() {
        using(var stream = new MemoryStream()) {
            Save(new BinaryWriter(stream));
            return stream.ToArray();
        }
    }

    public void Save(BinaryWriter writer) {
        writer.Write((byte)upperDeck.Count);
        foreach(var chn in upperDeck)
            writer.Write((byte)chn);
        writer.Write((byte)lowerDeck.Count);
        foreach(var chn in lowerDeck)
            writer.Write((byte)chn);
        writer.Write((byte)keyMapping.Count);
        foreach(var kv in keyMapping) {
            writer.Write((byte)kv.Key);
            writer.Write((short)kv.Value);
        }
    }

}
