using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

[RequireComponent(typeof(RectTransform))]
public class SelectSongEntry: MonoBehaviour {
    [SerializeField]
    Text directory;
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
    [SerializeField]
    RankControl rankControl;

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

    public void UpdateDisplay() {
        directory.enabled = isDirectory;
        songName.enabled = !isDirectory;
        artist.enabled = !isDirectory;
        otherInfo.enabled = !isDirectory;
        banner.gameObject.SetActive(!isDirectory);
        if(isDirectory) {
            directory.text = isParentDirectory ? string.Concat(dirInfo.Parent.Name, " << ", dirInfo.Name) : dirInfo.Name;
        } else {
            songName.text = songInfo.name;
            artist.text = string.IsNullOrEmpty(songInfo.subArtist) ?
                songInfo.artist :
                string.Concat(songInfo.artist, " / ", songInfo.subArtist);
            otherInfo.text = string.Format("Lv{0} {1}BPM", songInfo.level, songInfo.bpm);
            banner.SetTexture(songInfo.banner);
            banner.gameObject.SetActive(songInfo.banner);

            var record = SongInfoDetails.GetCurrentrecord(songInfo.bmsHash);
            if(record.HasValue)
                otherInfo.text += string.Format(" <size=28>{0}</size>",
                    SongInfoDetails.GetFormattedRankString(rankControl, record.Value.score));
        }
    }

    void OnSelect() {
        if(isDirectory)
            SongInfoLoader.CurrentDirectory = isParentDirectory ? dirInfo.Parent : dirInfo;
        else
            SongInfoLoader.SelectedSong = songInfo;
    }
}
