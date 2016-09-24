using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

[RequireComponent(typeof(RectTransform))]
public class SelectSongEntry: MonoBehaviour {

    [SerializeField]
    Text songName;
    [SerializeField]
    Text artist;
    [SerializeField]
    Text otherInfo;
    [SerializeField]
    Button selectButton;
    [SerializeField]
    RawImageFitter banner;

    SelectSongScrollView parent;

    bool isDirectory, isParentDirectory;
    SongInfo songInfo;
    DirectoryInfo dirInfo;

    [HideInInspector]
    public new RectTransform transform;

    void Awake() {
        transform = GetComponent<RectTransform>();
        selectButton.onClick.AddListener(OnSelect);
    }

    public void Load(SongInfo songInfo, SelectSongScrollView parent) {
        if(!isDirectory && songInfo.Equals(this.songInfo)) return;
        this.parent = parent;
        this.songInfo = songInfo;
        isDirectory = false;
        UpdateDisplay();
    }
    
    public void Load(DirectoryInfo dirInfo, bool isParent, SelectSongScrollView parent) {
        if(isDirectory && dirInfo == this.dirInfo) return;
        this.parent = parent;
        this.dirInfo = dirInfo;
        isParentDirectory = isParent;
        isDirectory = true;
        UpdateDisplay();
    }

    void UpdateDisplay() {
        if(isDirectory) {
            songName.text = (isParentDirectory ? "<< " : "") + dirInfo.Name;
            artist.text = string.Empty;
            otherInfo.text = string.Empty;
            banner.gameObject.SetActive(false);
        } else {
            songName.text = songInfo.name;
            artist.text = string.IsNullOrEmpty(songInfo.subArtist) ?
                songInfo.artist :
                string.Concat(songInfo.artist, " / " , songInfo.subArtist);
            otherInfo.text = string.Format("Lv{0} {1}BPM", songInfo.level, songInfo.bpm);
            banner.SetTexture(songInfo.banner);
            banner.gameObject.SetActive(songInfo.banner);
        }
    }

    void OnSelect() {
        if(isDirectory)
            SongInfoLoader.CurrentDirectory = dirInfo;
        else
            SongInfoLoader.SelectedSong = songInfo;
    }
}
