using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SongInfoDetails: MonoBehaviour {

    [SerializeField]
    RawImageFitter background;
    [SerializeField]
    RawImageFitter banner;
    [SerializeField]
    Text songDetails;

    GameObject bannerParent;

    void Awake() {
        bannerParent = banner.transform.parent.gameObject;
        OnUpdateInfo(SongInfoLoader.SelectedSong);
        SongInfoLoader.OnSelectionChanged += OnUpdateInfo;
        LanguageLoader.OnLanguageChange += OnChangeLang;
    }

    void OnDestroy() {
        SongInfoLoader.OnSelectionChanged -= OnUpdateInfo;
        LanguageLoader.OnLanguageChange -= OnChangeLang;
    }

    void OnChangeLang() {
        OnUpdateInfo(SongInfoLoader.SelectedSong);
    }

    void OnUpdateInfo(SongInfo? newInfo) {
        bool hasInfo = newInfo.HasValue;
        SongInfo songInfo = newInfo.GetValueOrDefault();
        if(!songInfo.banner && songInfo.background)
            songInfo.banner = songInfo.background;
        background.SetTexture(songInfo.background);
        background.gameObject.SetActive(songInfo.background);
        banner.SetTexture(songInfo.banner);
        bannerParent.gameObject.SetActive(songInfo.banner);
        songDetails.text = hasInfo ? string.Format(
            LanguageLoader.GetText(1),
            songInfo.name,
            songInfo.level,
            songInfo.artist,
            songInfo.subArtist,
            songInfo.genre,
            songInfo.bpm,
            songInfo.comments
        ) : string.Empty;
    }
}
