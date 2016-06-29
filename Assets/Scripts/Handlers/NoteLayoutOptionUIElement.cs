using UnityEngine;
using UnityEngine.UI;

class NoteLayoutOptionUIElement: MonoBehaviour {
    [SerializeField]
    Text display, keyCodeDisplay;
    public int index;
    public NoteLayoutOptionsHandler parent;

    void Start() {
        display.text = index.ToString();
        OnEnable();
    }

    void OnEnable() {
        KeyCode keyCode;
        if(NoteLayoutOptionsHandler.KeyMapping.TryGetValue(index, out keyCode))
            keyCodeDisplay.text = keyCode.ToString();
    }

    public void EditClick() {
        if(parent) parent.ShowKeyMapMenu(index);
    }

    public void OnKeyEdited(int channel, KeyCode keyCode) {
        if(channel != index) return;
        keyCodeDisplay.text = keyCode.ToString();
    }
}
