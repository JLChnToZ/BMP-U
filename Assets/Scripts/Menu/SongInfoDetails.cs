using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SongInfoDetails: MonoBehaviour {

    [SerializeField]
    RawImageFitter background;
    [SerializeField]
    RawImageFitter banner;
    [SerializeField]
    Text songDetails;
    [SerializeField]
    Text playerBest;
    [SerializeField]
    RankControl rankControl;
    [SerializeField]
    RectTransform detailsContainer;

    GameObject bannerParent;
    SongInfo songInfo;

    void Awake() {
        bannerParent = banner.transform.parent.gameObject;
        SongInfoLoader.OnSelectionChanged += OnUpdateInfo;
        LanguageLoader.OnLanguageChange += OnChangeLang;
    }

    void Start() {
        OnUpdateInfo(SongInfoLoader.SelectedSong);
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
        detailsContainer.gameObject.SetActive(hasInfo);
        songInfo = newInfo.GetValueOrDefault();
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
            songInfo.notes,
            songInfo.LayoutName,
            songInfo.comments
        ) : string.Empty;
        ReloadRecord();
    }

    public void ReloadRecord() {
        if(Loader.judgeMode == 2) {
            // Competition-Free mode will not bring out any result here
            playerBest.text = string.Empty;
            return;
        }
        RecordsManager.Record? record = GetCurrentrecord(songInfo.bmsHash);
        var recordContent = record.GetValueOrDefault();
        playerBest.text = record.HasValue ? string.Format(
            LanguageLoader.GetText(39),
            recordContent.score,
            recordContent.combos,
            recordContent.playCount,
            recordContent.timeStamp,
            GetFormattedRankString(rankControl, recordContent.score)
        ) : string.Empty;
    }

    public static RecordsManager.Record? GetCurrentrecord(string bmsHash) {
        if(string.IsNullOrEmpty(bmsHash))
            return null;
        return RecordsManager.Instance.GetRecord(bmsHash, NoteLayoutOptionsHandler.ChannelHash);
    }

    public static string GetFormattedRankString(RankControl rankControl, float score) {
        if(Loader.judgeMode == 2)
            return LanguageLoader.GetText(score > 500000 ? 33 : 34);
        string rankString;
        Color rankColor;
        rankControl.GetRank(score, out rankString, out rankColor);
        return string.Format(
            "<color=#{0}>{1}</color>",
            ColorUtility.ToHtmlStringRGBA(rankColor),
            rankString
        );
    }
}
