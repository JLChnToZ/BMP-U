using UnityEngine;
using UnityEngine.UI;

class NoteLayoutOptionUIElement: MonoBehaviour {
    [SerializeField]
    Text display;
    public int index;

    void Start() {
        display.text = index.ToString();
    }
}
