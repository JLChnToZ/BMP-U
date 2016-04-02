using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class SongFileSelect : MonoBehaviour {

    SongInfo songInfo = new SongInfo { index = -1 };
    [SerializeField]
    Text text;
    [SerializeField]
    RawImage rawImage;
    [SerializeField]
    Toggle button;
    [SerializeField]
    ColorRampLevel colorRampMapping;
    [NonSerialized]
    SelectSongScrollContent parent;

    public void SetParent(SelectSongScrollContent parent) {
        this.parent = parent;
        button.group = parent.toggleGroup;
    }

    public int Init(SongInfo songInfo) {
        if(this.songInfo.Equals(songInfo) && gameObject.activeInHierarchy && gameObject.activeSelf)
            return parent != null && parent.SelectedSongInternalIndex == songInfo.index ? -1 : 0;
        this.songInfo = songInfo;
        text.text = string.Format("{0} (Lv.{1})\n{2}\n{3}\n{4:#.#}BPM", songInfo.name, songInfo.level, songInfo.artist, songInfo.genre, songInfo.bpm);
        rawImage.enabled = songInfo.background;
        rawImage.texture = songInfo.background;

        var lvlColor = colorRampMapping.GetColor(songInfo.level);
        lvlColor.a = 0.5F;
        float h, s, v;
        Color.RGBToHSV(lvlColor, out h, out s, out v);
        var colorBlks = button.colors;
        colorBlks.normalColor = lvlColor;
        lvlColor.a = 1F;
        colorBlks.highlightedColor = lvlColor;
        colorBlks.pressedColor = Color.HSVToRGB(h, s, v / 2);
        button.colors = colorBlks;
        if(parent != null) {
            bool on = parent.SelectedSongInternalIndex == songInfo.index;
            if(on) button.isOn = true;
            return on ? 2 : 1;
        }
        return 1;
    }
	
    public void OnClick(bool isOn) {
        if(!isOn) return;
        Loader.songPath = songInfo.filePath;
        if(parent != null)
            parent.OnSelected(songInfo);
        else
            SceneManager.LoadScene("GameScene");
    }

    void OnDestroy() {
        rawImage.texture = null;
        if(songInfo.background) Destroy(songInfo.background);
    }
}
