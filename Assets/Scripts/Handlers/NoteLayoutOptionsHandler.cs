using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;

class NoteLayoutOptionsHandler: MonoBehaviour {
    static readonly int[] usableChannels = new[] { 11, 12, 13, 14, 15, 18, 19, 21, 22, 23, 24, 25, 28, 29, 26, 27, 17, 16};
    static readonly List<int> upperDeck = new List<int>(), lowerDeck = new List<int>();
    static bool initialized;
    readonly Dictionary<int, NoteLayoutOptionUIElement> currentMapping = new Dictionary<int, NoteLayoutOptionUIElement>();

    public static IList<int> UpperDeck {
        get { return upperDeck.AsReadOnly(); }
    }

    public static IList<int> LowerDeck {
        get { return lowerDeck.AsReadOnly(); }
    }

    [SerializeField]
    ReorderableList unusedList, upperDeckList, lowerDeckList;
    [SerializeField]
    GameObject itemPrefab;
    [SerializeField]
    PresetHandler presetsHandler;

    public static void Initialize() {
        if(initialized) return;
        // Default: 1/2/3/4/5/8/9 = Normal, 6 = Scratch, 7 = FreeZone
        foreach(var channel in usableChannels)
            switch(channel % 10) {
                case 6: case 7: upperDeck.Add(channel); break;
                default: lowerDeck.Add(channel); break;
            }
        initialized = true;
    }

    void Awake() {
        presetsHandler.OnPresetChange = Load;
        presetsHandler.OnPresetRequest = ApplyAndSave;
        foreach(var channel in usableChannels) {
            var go = Instantiate(itemPrefab);
            go.transform.SetParent(unusedList.Content, false);
            var elm = go.GetComponent<NoteLayoutOptionUIElement>();
            elm.index = channel;
            currentMapping.Add(channel, elm);
        }
        Initialize();
        AssignToDisplay();
    }

    public void Load(byte[] source) {
        if(source == null || source.Length == 0) {
            Initialize();
            return;
        }
        using(var stream = new MemoryStream(source))
            Load(new BinaryReader(stream));
    }

    public void Load(BinaryReader reader) {
        var unusedChannels = new HashSet<int>(usableChannels);
        int upperLength, lowerLength, i, channel;
        upperDeck.Clear();
        lowerDeck.Clear();
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
    }

    public void Apply() {
        presetsHandler.SetRequest(null, ApplyAndSave());
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
    }

}
